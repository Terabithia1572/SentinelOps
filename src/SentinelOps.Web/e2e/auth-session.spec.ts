import { test, expect } from '@playwright/test';
import { ADMIN_EMAIL, ADMIN_PASSWORD } from './test-helpers';

test.describe('Authentication & Session Lifecycle', () => {

  test('Empty credentials triggers HTML5 validation constraint', async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();

    await page.goto('/login');
    const emailInput = page.locator('#emailOrUser');
    const passwordInput = page.locator('#password');

    // Both inputs have HTML5 required constraint
    const emailRequired = await emailInput.getAttribute('required');
    const passwordRequired = await passwordInput.getAttribute('required');
    expect(emailRequired).not.toBeNull();
    expect(passwordRequired).not.toBeNull();

    // Verify invalidity before entering values
    const isInvalid = await emailInput.evaluate((el: HTMLInputElement) => !el.checkValidity());
    expect(isInvalid).toBe(true);

    await context.close();
  });

  test('Invalid credentials displays authentication error alert', async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();

    await page.goto('/login');
    await page.fill('#emailOrUser', 'invalid-user@sentinelops.local');
    await page.fill('#password', 'WrongPassword123!');
    await page.click('button[type="submit"]');

    // Should stay on login and display error message
    await expect(page).toHaveURL(/.*login/);
    await expect(page.locator('.border-rose-500\\/20, [role="alert"]')).toBeVisible({ timeout: 5000 });

    await context.close();
  });

  test('Valid login and subsequent logout flows correctly in isolated context', async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();

    await page.goto('/login');
    await page.fill('#emailOrUser', ADMIN_EMAIL);
    await page.fill('#password', ADMIN_PASSWORD);
    await page.click('button[type="submit"]');

    // Wait for dashboard redirect
    await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 10000 });
    await expect(page.locator('main')).toBeVisible();

    // Verify user profile badge is rendered
    await expect(page.locator('aside')).toContainText(ADMIN_EMAIL);

    // Click Sign Out / Exit button
    const exitButton = page.locator('button[title="Sign Out"]');
    if (await exitButton.isVisible()) {
      await exitButton.click();
      await page.waitForURL(/.*login/, { timeout: 10000 });
      await expect(page).toHaveURL(/.*login/);
    }

    await context.close();
  });

  test('Invalid session token redirects unauthenticated user to /login in isolated context', async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();

    await page.addInitScript(() => {
      localStorage.setItem('sentinelops_token', 'invalid-jwt-token-value');
    });

    await page.goto('/overview');
    await page.waitForURL(/.*login/, { timeout: 10000 });
    await expect(page).toHaveURL(/.*login/);

    await context.close();
  });
});
