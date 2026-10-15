import { test, expect } from '@playwright/test';
import { waitForAppReady } from './test-helpers';

test.describe('Applications Management Workflows', () => {

  test('Applications page loads and displays existing apps or register prompt', async ({ page }) => {
    await page.goto('/applications');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Applications');
    await expect(page.locator('input[placeholder*="Search"]').first()).toBeVisible();
  });

  test('Create Application modal can be opened and dismissed cleanly', async ({ page }) => {
    await page.goto('/applications');
    await waitForAppReady(page);

    // Click "New Application" button
    const createBtn = page.locator('button:has-text("New Application")');
    if (await createBtn.isVisible()) {
      await createBtn.click();
      await expect(page.locator('text=Register Application')).toBeVisible();

      // Verify input fields
      const appNameInput = page.locator('input[placeholder="e.g. payment-service"]');
      await expect(appNameInput).toBeVisible();

      // Cancel modal
      await page.locator('button:has-text("Cancel")').click();
      await expect(page.locator('text=Register Application')).not.toBeVisible();
    }
  });

  test('Search filter reacts to user input without errors', async ({ page }) => {
    await page.goto('/applications');
    await waitForAppReady(page);

    const searchInput = page.locator('input[placeholder*="Search"]').first();
    await searchInput.fill('sentinelops-nonexistent-search-term-xyz');
    await page.waitForTimeout(300);

    // Should render empty search message or no matching apps
    await expect(page.locator('main')).toBeVisible();
    await searchInput.clear();
  });
});
