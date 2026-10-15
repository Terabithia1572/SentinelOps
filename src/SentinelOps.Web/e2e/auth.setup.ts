import { test as setup, expect } from '@playwright/test';
import fs from 'fs';
import path from 'path';

const authFile = 'e2e/.auth/admin.json';

setup('authenticate as admin', async ({ page }) => {
  const adminEmail = process.env.E2E_ADMIN_EMAIL || 'admin@sentinelops.local';
  const adminPassword = process.env.E2E_ADMIN_PASSWORD || 'Password123!';

  const authDir = path.dirname(path.resolve(authFile));
  if (!fs.existsSync(authDir)) {
    fs.mkdirSync(authDir, { recursive: true });
  }

  await page.goto('/login');
  await page.fill('#emailOrUser', adminEmail);
  await page.fill('#password', adminPassword);
  await page.click('button[type="submit"]');

  await page.waitForURL((url) => !url.pathname.includes('/login'), { timeout: 15000 });
  await page.waitForSelector('main', { timeout: 15000 });

  await page.context().storageState({ path: authFile });
});
