const path = require('path');
const { chromium } = require(path.resolve(__dirname, '../src/SentinelOps.Web/node_modules/@playwright/test'));
const fs = require('fs');

const outputDir = path.resolve(__dirname, '../docs/phase18/screenshots');
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

async function run() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 }
  });

  const concepts = [
    { id: "concept-a", name: "Concept A (Precision Dark)" },
    { id: "concept-b", name: "Concept B (Editorial Enterprise)" },
    { id: "concept-c", name: "Concept C (Industrial Command Center)" }
  ];

  const screens = ["login", "overview", "logs", "incident"];

  for (const concept of concepts) {
    for (const screen of screens) {
      const page = await context.newPage();
      const targetUrl = `http://localhost:3000/design-prototypes/${concept.id}/${screen}`;
      const filename = `${concept.id}-${screen}.png`;
      const filePath = path.join(outputDir, filename);

      console.log(`Capturing ${concept.name} - ${screen} -> ${filename}`);
      try {
        await page.goto(targetUrl, { waitUntil: 'networkidle', timeout: 15000 });
        await page.waitForTimeout(600);
        await page.screenshot({ path: filePath, fullPage: false });
        console.log(`Saved screenshot: ${filename}`);
      } catch (err) {
        console.error(`Error capturing ${filename}: ${err.message}`);
      } finally {
        await page.close();
      }
    }
  }

  // Also capture the index comparison screen
  const indexPage = await context.newPage();
  try {
    await indexPage.goto('http://localhost:3000/design-prototypes', { waitUntil: 'networkidle' });
    await indexPage.waitForTimeout(500);
    await indexPage.screenshot({ path: path.join(outputDir, 'concepts-comparison-index.png'), fullPage: false });
    console.log("Saved screenshot: concepts-comparison-index.png");
  } finally {
    await indexPage.close();
  }

  await context.close();
  await browser.close();
  console.log("All 12 design prototype screenshots captured successfully!");
}

run().catch(err => {
  console.error("Capture failed:", err);
  process.exit(1);
});
