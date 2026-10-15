import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  timeout: 30000,
  expect: {
    timeout: 10000
  },
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'Desktop-1920x1080',
      use: {
        viewport: { width: 1920, height: 1080 },
        browserName: 'chromium',
      },
    },
    {
      name: 'Laptop-1440x900',
      use: {
        viewport: { width: 1440, height: 900 },
        browserName: 'chromium',
      },
    },
    {
      name: 'Tablet-768x1024',
      use: {
        viewport: { width: 768, height: 1024 },
        browserName: 'chromium',
      },
    },
    {
      name: 'Mobile-375x812',
      use: {
        viewport: { width: 375, height: 812 },
        browserName: 'chromium',
        isMobile: true,
      },
    },
  ],
});
