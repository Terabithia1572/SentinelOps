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

    // 3. Overview Page
    console.log("\n[3/12] Verifying Overview Dashboard...");
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
    console.log("\n[11/12] Verifying Exceptions Explorer...");
    await page.goto(`${BASE_URL}/exceptions`, { waitUntil: "networkidle0" });
    await new Promise((r) => setTimeout(r, 1500));
    await page.screenshot({ path: path.join(SCREENSHOTS_DIR, "10-exceptions-explorer.png") });
    console.log("Saved: 10-exceptions-explorer.png");

    // 12. Responsive Viewport Tests (Tablet & Mobile)
    // 13. Responsive Viewport Tests (Tablet & Mobile)
    console.log("\n[13/13] Verifying Responsive Viewports...");
    
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
    console.log(`Total Screenshots Captured: 13`);
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
