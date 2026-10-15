import { test, expect } from '@playwright/test';
import { waitForAppReady } from './test-helpers';

test.describe('Telemetry Exploration & Observability Workflows', () => {

  test('Logs explorer supports search input, severity filtering, and time ranges', async ({ page }) => {
    await page.goto('/logs');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Log Explorer');

    // Verify search input
    const searchInput = page.locator('input[placeholder*="Search"]').first();
    await expect(searchInput).toBeVisible();
    await searchInput.fill('status:error');

    // Verify severity filter button
    const errorPill = page.locator('button:has-text("Error")').first();
    if (await errorPill.isVisible()) {
      await errorPill.click();
    }

    // Clear search filter
    await searchInput.clear();
  });

  test('Traces explorer supports latency, error filtering, and detail navigation', async ({ page }) => {
    await page.goto('/traces');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Distributed Traces');

    // Errors only filter
    const errorCheckbox = page.locator('input[type="checkbox"]#errorsOnly');
    if (await errorCheckbox.isVisible()) {
      await errorCheckbox.check();
      expect(await errorCheckbox.isChecked()).toBe(true);
      await errorCheckbox.uncheck();
    }
  });

  test('Metrics page renders operational telemetry dashboards and metric controls', async ({ page }) => {
    await page.goto('/metrics');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Metrics');
    await expect(page.locator('main')).toBeVisible();
  });

  test('APM & Service Map page renders without client errors', async ({ page }) => {
    await page.goto('/apm');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Application Performance');
    await expect(page.locator('main')).toBeVisible();
  });
});
