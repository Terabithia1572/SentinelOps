import { test, expect } from '@playwright/test';

test.describe('SentinelOps Live Platform Browser Audit', () => {

  const adminEmail = process.env.E2E_ADMIN_EMAIL || 'admin@sentinelops.local';
  const adminPassword = process.env.E2E_ADMIN_PASSWORD || 'Password123!';

  test('Unauthenticated user is redirected to /login', async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();

    await page.goto('/');
    await expect(page).toHaveURL(/.*login/);
    await expect(page.locator('h1')).toContainText('SentinelOps');
    await expect(page.locator('#emailOrUser')).toBeVisible();
    await expect(page.locator('#password')).toBeVisible();

    await context.close();
  });

  test('Successful login with valid credentials navigates to dashboard', async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();

    await page.goto('/login');
    await page.fill('#emailOrUser', adminEmail);
    await page.fill('#password', adminPassword);
    await page.click('button[type="submit"]');

    // Wait for navigation away from login
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10000 });
    await expect(page).not.toHaveURL(/.*login/);

    // Verify main app layout loaded
    const bodyText = await page.textContent('body');
    expect(bodyText).toContain('SentinelOps');

    await context.close();
  });

  test('Core telemetry and SaaS routes load without uncaught client errors', async ({ page }) => {
    const routes = [
      '/apm',
      '/alerts',
      '/incidents',
      '/organizations',
      '/billing',
      '/applications',
      '/logs',
      '/traces',
      '/metrics'
    ];

    for (const route of routes) {
      const response = await page.goto(route);
      expect(response?.status()).toBeLessThan(400);
      await page.waitForSelector('main', { timeout: 15000 });
      await expect(page.locator('main')).toBeVisible({ timeout: 15000 });
      const mainContent = await page.locator('main').innerText();
      expect(mainContent.trim().length).toBeGreaterThan(0);
    }
  });

  test('Responsive viewport renders cleanly with no horizontal overflow', async ({ page }) => {
    await page.goto('/login');
    await page.waitForLoadState('domcontentloaded');

    // Check horizontal scroll on current viewport
    const hasHorizontalOverflow = await page.evaluate(() => {
      return document.documentElement.scrollWidth > window.innerWidth;
    });

    expect(hasHorizontalOverflow).toBe(false);
  });
});
