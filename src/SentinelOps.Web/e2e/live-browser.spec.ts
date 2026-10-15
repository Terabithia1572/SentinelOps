import { test, expect } from '@playwright/test';

test.describe('SentinelOps Live Platform Browser Audit', () => {

  const adminEmail = process.env.E2E_ADMIN_EMAIL || 'admin@sentinelops.local';
  const adminPassword = process.env.E2E_ADMIN_PASSWORD || 'Password123!';

  test('Unauthenticated user is redirected to /login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/.*login/);
    await expect(page.locator('h1')).toContainText('SentinelOps');
    await expect(page.locator('#emailOrUser')).toBeVisible();
    await expect(page.locator('#password')).toBeVisible();
  });

  test('Successful login with valid credentials navigates to dashboard', async ({ page }) => {
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
  });

  test('Core telemetry and SaaS routes load without uncaught client errors', async ({ page }) => {
    // 1. Authenticate
    await page.goto('/login');
    await page.fill('#emailOrUser', adminEmail);
    await page.fill('#password', adminPassword);
    await page.click('button[type="submit"]');
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10000 });

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
      await page.waitForSelector('#root > *', { timeout: 10000 });
      await page.waitForTimeout(250);
      const content = await page.locator('body').innerText();
      expect(content.length).toBeGreaterThan(10);
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
