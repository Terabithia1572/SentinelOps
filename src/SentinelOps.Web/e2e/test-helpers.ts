import { Page, expect } from '@playwright/test';

export const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL || 'admin@sentinelops.local';
export const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD || 'Password123!';

/**
 * Ensures the page main content container is mounted and populated.
 */
export async function waitForAppReady(page: Page) {
  await page.waitForSelector('main', { timeout: 15000 });
  await expect(page.locator('main')).toBeVisible({ timeout: 15000 });
}
