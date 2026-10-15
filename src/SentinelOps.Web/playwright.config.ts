import { defineConfig } from '@playwright/test';

const authFile = 'e2e/.auth/admin.json';

export default defineConfig({
  testDir: './e2e',
  timeout: 30000,
  expect: {
    timeout: 10000,
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
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: 'Desktop-1920x1080',
      dependencies: ['setup'],
      use: {
        viewport: { width: 1920, height: 1080 },
        browserName: 'chromium',
        storageState: authFile,
      },
      testIgnore: /auth\.setup\.ts/,
    },
    {
      name: 'Laptop-1440x900',
      dependencies: ['setup'],
      use: {
        viewport: { width: 1440, height: 900 },
        browserName: 'chromium',
        storageState: authFile,
      },
      testIgnore: /auth\.setup\.ts/,
    },
    {
      name: 'Tablet-768x1024',
      dependencies: ['setup'],
      use: {
        viewport: { width: 768, height: 1024 },
        browserName: 'chromium',
        storageState: authFile,
      },
      testIgnore: /auth\.setup\.ts/,
    },
    {
      name: 'Mobile-375x812',
      dependencies: ['setup'],
      use: {
        viewport: { width: 375, height: 812 },
        browserName: 'chromium',
        isMobile: true,
        storageState: authFile,
      },
      testIgnore: /auth\.setup\.ts/,
    },
  ],
});
