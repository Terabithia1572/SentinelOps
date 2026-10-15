import { test, expect } from '@playwright/test';
import { waitForAppReady } from './test-helpers';

test.describe('Dashboards & Cross-Device Responsiveness', () => {

  test('Custom Observability Dashboards view loads and displays dashboard controls', async ({ page }) => {
    await page.goto('/dashboards');
    await waitForAppReady(page);

    await expect(page.locator('h1')).toContainText('Observability Dashboards');
    await expect(page.locator('main')).toBeVisible();
  });

  test('Responsive viewport layout does not cause horizontal overflow', async ({ page }) => {
    await page.goto('/overview');
    await waitForAppReady(page);

    const hasHorizontalOverflow = await page.evaluate(() => {
      return document.documentElement.scrollWidth > window.innerWidth;
    });

    expect(hasHorizontalOverflow).toBe(false);
  });
});
