import { defineConfig, devices } from '@playwright/test';

// Cổng anti-drift FE (browser thật): tự khởi Vite dev guest-web rồi chạy assertion responsive đa-viewport.
// Song song INV-1..6 của BE — "mở web bằng browser phát hiện lỗi". CI thêm job FE gọi lệnh này.
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: process.env.CI ? 'github' : 'list',
  use: {
    baseURL: 'http://localhost:5173',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'pnpm --filter @starhill/guest-web dev',
    url: 'http://localhost:5173',
    cwd: '..',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
