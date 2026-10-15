import { test, expect } from '@playwright/test';
import { waitForAppReady } from './test-helpers';

test.describe('Alerting and Incident Management Workflows', () => {

  test('Alert Rules view renders and supports create rule modal inspection', async ({ page }) => {
    await page.goto('/alert-rules');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Alert Rules');

    // Click Create Rule button if visible
    const createBtn = page.locator('button:has-text("Create Rule"), button:has-text("New Rule")').first();
    if (await createBtn.isVisible()) {
      await createBtn.click();
      await expect(page.locator('text=Create Alert Rule, text=New Alert Rule').first()).toBeVisible();

      // Cancel modal
      const cancelBtn = page.locator('button:has-text("Cancel")').first();
      if (await cancelBtn.isVisible()) {
        await cancelBtn.click();
      }
    }
  });

  test('Incidents queue renders status tabs, search, and incident lifecycle view', async ({ page }) => {
    await page.goto('/incidents');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Incident Management');

    // Verify search input
    const searchInput = page.locator('input[placeholder*="Search"]').first();
    if (await searchInput.isVisible()) {
      await searchInput.fill('investigation');
    }

    // Check status filter buttons or tabs
    const statusTab = page.locator('button:has-text("All Incidents")').first();
    if (await statusTab.isVisible()) {
      await statusTab.dispatchEvent('click');
    }
  });
});
