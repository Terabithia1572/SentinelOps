import puppeteer from "puppeteer-core";
import path from "path";
import fs from "fs";

const CHROME_PATH = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const BASE_URL = "http://localhost:3000";
const SCREENSHOTS_DIR = "C:\\Users\\Yunus\\Desktop\\SentinelOps\\docs\\screenshots";

if (!fs.existsSync(SCREENSHOTS_DIR)) {
  fs.mkdirSync(SCREENSHOTS_DIR, { recursive: true });
}

async function runVisualQA() {
  console.log("=================================================");
  console.log("   SENTINELOPS — BROWSER VISUAL QA & VERIFICATION");
  console.log("=================================================");

  const browser = await puppeteer.launch({
    executablePath: CHROME_PATH,
    headless: "new",
    args: ["--no-sandbox", "--disable-setuid-sandbox", "--disable-gpu"],
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1440, height: 900 });

  const consoleErrors = [];
  page.on("console", (msg) => {
    if (msg.type() === "error") {
      consoleErrors.push(msg.text());
    }
  });

  page.on("pageerror", (err) => {
    consoleErrors.push(err.message);
  });

  page.on("response", (res) => {
    if (res.status() >= 400) {
      console.log(`HTTP ${res.status()}: ${res.url()}`);
    }
  });

  try {
    // 1. Visit Login Page
    console.log("\n[1/13] Verifying Login Page...");
    await page.goto(`${BASE_URL}/login`, { waitUntil: "networkidle0" });
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "01-login.png") });
    console.log("Saved: 01-login.png");

    // 2. Visit Register Page (unauthenticated)
    console.log("\n[2/13] Verifying Register Page...");
    await page.goto(`${BASE_URL}/register`, { waitUntil: "networkidle0" });
    await page.waitForSelector("#fullName", { timeout: 5000 });
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "13-register.png") });
    console.log("Saved: 13-register.png");

    // 3. Perform Login
    console.log("\n[3/13] Submitting Credentials on Login Page...");
    await page.goto(`${BASE_URL}/login`, { waitUntil: "networkidle0" });
    await page.waitForSelector("#emailOrUser");
    await page.type("#emailOrUser", "qa.admin@sentinelops.local");
    await page.type("#password", "P@ssword2026!Secure");
    await page.click('button[type="submit"]');

    // Wait for redirect to Dashboard
    await page.waitForNavigation({ waitUntil: "networkidle0" });
    console.log("Login successful! Current URL:", page.url());

    // Seed Phase 5 demo data if not present
    console.log("\nEnsuring Phase 5 Alerting, Incident, and SLO baseline data...");
    await page.evaluate(async () => {
      const token = localStorage.getItem("sentinelops_token");
      const headers = {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      };

      // 1. Get or create Application
      let appRes = await fetch("/api/applications", { headers });
      let appJson = await appRes.json();
      let apps = appJson.data || [];
      let appId = apps[0]?.id;

      if (!appId) {
        const createRes = await fetch("/api/applications", {
          method: "POST",
          headers,
          body: JSON.stringify({
            name: "payments-api",
            displayName: "Payments & Checkout Service",
            description: "High-throughput core transaction processing pipeline",
          }),
        });
        const createdJson = await createRes.json();
        appId = createdJson.data?.id;
      }

      if (!appId) return;

      // 2. Notification Channels
      const chRes = await fetch("/api/notification-channels", { headers });
      const chJson = await chRes.json();
      let channels = chJson.data || [];
      if (channels.length === 0) {
        await fetch("/api/notification-channels", {
          method: "POST",
          headers,
          body: JSON.stringify({
            name: "SRE Slack Alerts",
            description: "Production channel for on-call SRE alerts",
            type: "Webhook",
            destination: "https://hooks.slack.com/services/T00/B00/X00",
          }),
        });
        await fetch("/api/notification-channels", {
          method: "POST",
          headers,
          body: JSON.stringify({
            name: "On-Call PagerDuty",
            description: "High-urgency paging endpoint",
            type: "PagerDuty",
            destination: "https://events.pagerduty.com/v2/enqueue",
          }),
        });
      }

      // 3. Alert Rules
      const rulesRes = await fetch(`/api/alert-rules?applicationId=${appId}`, { headers });
      const rulesJson = await rulesRes.json();
      let rules = rulesJson.data?.items || [];
      if (rules.length === 0) {
        await fetch("/api/alert-rules", {
          method: "POST",
          headers,
          body: JSON.stringify({
            name: "High Checkout Error Rate",
            description: "Triggers when HTTP 5xx error rate exceeds 5.0% over 5m window",
            applicationId: appId,
            serviceName: "checkout-api",
            environmentName: "Production",
            conditionType: "ErrorRateAboveThreshold",
            operator: "GreaterThan",
            metricName: "error_rate",
            threshold: 5.0,
            evaluationWindowMinutes: 5,
            consecutiveCountRequired: 1,
            cooldownMinutes: 15,
            severity: "Critical",
          }),
        });

        await fetch("/api/alert-rules", {
          method: "POST",
          headers,
          body: JSON.stringify({
            name: "Elevated P95 API Latency",
            description: "Triggers when P95 response time exceeds 400ms across service endpoints",
            applicationId: appId,
            serviceName: "checkout-api",
            environmentName: "Production",
            conditionType: "P95LatencyAboveThreshold",
            operator: "GreaterThan",
            metricName: "latency_p95",
            threshold: 400.0,
            evaluationWindowMinutes: 5,
            consecutiveCountRequired: 2,
            cooldownMinutes: 15,
            severity: "Warning",
          }),
        });
      }

      // 4. Incidents
      const incRes = await fetch(`/api/incidents?applicationId=${appId}`, { headers });
      const incJson = await incRes.json();
      let incidents = incJson.data?.items || [];
      if (incidents.length === 0) {
        const createIncRes = await fetch("/api/incidents", {
          method: "POST",
          headers,
          body: JSON.stringify({
            title: "Elevated Payment Checkout Latency across US-East Region",
            description: "P95 latency elevated to 840ms due to database connection pool saturation. Multiple payment retries observed.",
            severity: "Critical",
            applicationId: appId,
            serviceName: "checkout-api",
            environmentName: "Production",
          }),
        });
        const createdInc = await createIncRes.json();
        const incId = createdInc.data?.id;
        if (incId) {
          // Add comment
          await fetch(`/api/incidents/${incId}/comments`, {
            method: "POST",
            headers,
            body: JSON.stringify({
              commentText: "Initial diagnosis: Connection pool leak identified in background payment worker. Initiating hotfix patch deployment.",
            }),
          });
          // Transition to Investigating
          await fetch(`/api/incidents/${incId}/transition`, {
            method: "POST",
            headers,
            body: JSON.stringify({
              status: "Investigating",
              note: "Lead engineer assigned, incident triage underway",
            }),
          });
        }
      }

      // 5. SLOs
      const sloRes = await fetch(`/api/slos?applicationId=${appId}`, { headers });
      const sloJson = await sloRes.json();
      let slos = sloJson.data || [];
      if (slos.length === 0) {
        const createSloRes1 = await fetch("/api/slos", {
          method: "POST",
          headers,
          body: JSON.stringify({
            name: "Checkout API 99.9% Availability",
            description: "Core payment checkout transaction availability over rolling 24 hours",
            applicationId: appId,
            serviceName: "checkout-api",
            environmentName: "Production",
            indicatorType: "Availability",
            targetPercentage: 99.9,
            evaluationWindow: "Rolling24Hours",
          }),
        });
        const slo1 = await createSloRes1.json();
        if (slo1.data?.id) {
          await fetch(`/api/slos/${slo1.data.id}/evaluate`, { method: "POST", headers });
        }

        const createSloRes2 = await fetch("/api/slos", {
          method: "POST",
          headers,
          body: JSON.stringify({
            name: "Checkout Latency < 250ms (P99 Objective)",
            description: "Percentage of checkout requests completing in under 250ms",
            applicationId: appId,
            serviceName: "checkout-api",
            environmentName: "Production",
            indicatorType: "Latency",
            targetPercentage: 99.0,
            evaluationWindow: "Rolling7Days",
            latencyThresholdMs: 250,
          }),
        });
        const slo2 = await createSloRes2.json();
        if (slo2.data?.id) {
          await fetch(`/api/slos/${slo2.data.id}/evaluate`, { method: "POST", headers });
        }
      }
    });

    // 3. Overview Page
    console.log("\n[3/17] Verifying Overview Dashboard...");
    await page.waitForSelector("h1", { timeout: 10000 });
    // Let charts settle
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "02-overview.png") });
    console.log("Saved: 02-overview.png");

    // 4. Applications Page
    console.log("\n[4/12] Verifying Applications Page...");
    await page.goto(`${BASE_URL}/applications`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "03-applications.png") });
    console.log("Saved: 03-applications.png");

    // 5. API Keys Page
    console.log("\n[5/12] Verifying API Keys Page...");
    await page.goto(`${BASE_URL}/api-keys`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "04-api-keys.png") });
    console.log("Saved: 04-api-keys.png");

    // 6. Log Explorer Page
    console.log("\n[6/12] Verifying Log Explorer...");
    await page.goto(`${BASE_URL}/logs`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    
    // Click on the first log row to expand structured properties
    const firstLogRow = await page.$(".divide-y > div");
    if (firstLogRow) {
      await firstLogRow.click();
      await new Promise((r) => setTimeout(r, 600));
    }
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "05-logs-explorer.png") });
    console.log("Saved: 05-logs-explorer.png (with expanded structured properties)");

    // 7. Distributed Traces Page (List)
    console.log("\n[7/12] Verifying Distributed Traces List...");
    await page.goto(`${BASE_URL}/traces`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "06-traces-list.png") });
    console.log("Saved: 06-traces-list.png");

    // 8. Trace Hierarchical Waterfall Detail
    console.log("\n[8/12] Verifying Trace Waterfall Visualization...");
    const firstTraceRow = await page.$(".divide-y > div");
    if (firstTraceRow) {
      await firstTraceRow.click();
      await new Promise((r) => setTimeout(r, 1500));
      await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "07-traces-waterfall.png") });
      console.log("Saved: 07-traces-waterfall.png");
    }

    // 9. Metrics Explorer Page
    console.log("\n[9/12] Verifying Metrics Explorer...");
    await page.goto(`${BASE_URL}/metrics`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 2000));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "08-metrics-explorer.png") });
    console.log("Saved: 08-metrics-explorer.png");

    // 10. Service Health Monitoring Page
    console.log("\n[10/12] Verifying Service Health Monitoring...");
    await page.goto(`${BASE_URL}/health`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "09-service-health.png") });
    console.log("Saved: 09-service-health.png");

    // 11. Exceptions Explorer Page
    console.log("\n[11/17] Verifying Exceptions Explorer...");
    await page.goto(`${BASE_URL}/exceptions`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "10-exceptions-explorer.png") });
    console.log("Saved: 10-exceptions-explorer.png");

    // 12. Alerts Page
    console.log("\n[12/17] Verifying Alerts Overview...");
    await page.goto(`${BASE_URL}/alerts`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "14-alerts-overview.png") });
    console.log("Saved: 14-alerts-overview.png");

    // 13. Alert Rules Page
    console.log("\n[13/17] Verifying Alert Rules & Notification Channels...");
    await page.goto(`${BASE_URL}/alert-rules`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "15-alert-rules.png") });
    console.log("Saved: 15-alert-rules.png");

    // 14. Incidents Page
    console.log("\n[14/17] Verifying Incidents List & Triage...");
    await page.goto(`${BASE_URL}/incidents`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "16-incidents-list.png") });
    console.log("Saved: 16-incidents-list.png");

    // 15. Incident Detail Page (if incident exists)
    const firstIncidentCard = await page.$('a[href^="/incidents/"]');
    if (firstIncidentCard) {
      console.log("\n[15/17] Verifying Incident Detail & Audit Timeline...");
      await firstIncidentCard.click();
      await new Promise((r) => setTimeout(r, 1500));
      await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "17-incident-detail.png") });
      console.log("Saved: 17-incident-detail.png");
    }

    // 16. SLOs Dashboard
    console.log("\n[16/17] Verifying SLO & Error Budget Dashboard...");
    await page.goto(`${BASE_URL}/slos`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "18-slos-dashboard.png") });
    console.log("Saved: 18-slos-dashboard.png");

    // 17. Responsive Viewport Tests (Tablet & Mobile)
    console.log("\n[17/17] Verifying Responsive Viewports...");
    
    // Tablet Viewport (1024x768)
    await page.setViewport({ width: 1024, height: 768 });
    await page.goto(`${BASE_URL}/metrics`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1200));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "11-responsive-tablet.png") });
    console.log("Saved: 11-responsive-tablet.png (Tablet 1024x768)");

    // Mobile Viewport (390x844 iPhone 14/15)
    await page.setViewport({ width: 390, height: 844 });
    await page.goto(`${BASE_URL}/`, { waitUntil: "networkidle0" });
    await page.waitForSelector("h1", { timeout: 10000 });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "12-responsive-mobile.png") });
    console.log("Saved: 12-responsive-mobile.png (Mobile 390x844)");

    console.log("\n=================================================");
    console.log("   VISUAL QA BROWSER VERIFICATION SUMMARY");
    console.log("=================================================");
    console.log(`Total Screenshots Captured: 18`);
    console.log(`Console Errors Encountered: ${consoleErrors.length}`);
    if (consoleErrors.length > 0) {
      console.log("Errors:", consoleErrors);
    } else {
      console.log("Zero runtime JavaScript console errors encountered!");
    }
  } finally {
    await browser.close();
  }
}

runVisualQA().catch((err) => {
  console.error("Visual QA failed:", err);
  process.exit(1);
});
