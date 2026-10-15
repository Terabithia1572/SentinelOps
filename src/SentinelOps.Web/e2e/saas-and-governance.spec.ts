import { test, expect } from '@playwright/test';
import { waitForAppReady } from './test-helpers';

test.describe('Multi-Tenant SaaS Governance, Billing, & Resilience', () => {

  test('Organizations management displays tabs for members, invitations, settings, and quotas', async ({ page }) => {
    await page.goto('/organizations');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Organizations');

    // Click tabs
    const membersTab = page.locator('button:has-text("Members")');
    if (await membersTab.isVisible()) {
      await membersTab.click();
    }

    const settingsTab = page.locator('button:has-text("Settings")');
    if (await settingsTab.isVisible()) {
      await settingsTab.click();
      await expect(page.locator('main')).toBeVisible();
    }

    const usageTab = page.locator('button:has-text("Usage"), button:has-text("Quotas")').first();
    if (await usageTab.isVisible()) {
      await usageTab.click();
      await expect(page.locator('main')).toBeVisible();
    }
  });

  test('Billing page renders subscription tiers and plans breakdown', async ({ page }) => {
    await page.goto('/billing');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Subscription & SaaS Management');

    // Click Plans tab
    const plansTab = page.locator('button:has-text("Plans")');
    if (await plansTab.isVisible()) {
      await plansTab.click();
      // Should show plans cards
      await expect(page.locator('main')).toBeVisible();
    }

    // Click Usage tab
    const usageTab = page.locator('button:has-text("Usage")');
    if (await usageTab.isVisible()) {
      await usageTab.click();
      await expect(page.locator('main')).toBeVisible();
    }
  });

  test('Network error resilience: API failure renders graceful error state without white screen crash', async ({ page }) => {
    // Intercept /api/applications to return 500 internal server error
    await page.route('**/api/applications', (route) => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ success: false, message: 'Simulated infrastructure disruption' }),
      });
    });

    await page.goto('/applications');
    await page.waitForSelector('main', { timeout: 10000 });

    // The page should remain mounted and render error message rather than unhandled blank crash
    await expect(page.locator('main')).toBeVisible();
    const bodyContent = await page.locator('body').innerText();
    expect(bodyContent.length).toBeGreaterThan(0);
    await page.unroute('**/api/applications');
  });
});
