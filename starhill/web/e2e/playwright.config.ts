import { defineConfig, devices } from '@playwright/test';

const guestBaseUrl = process.env.STARHILL_E2E_GUEST_BASE_URL ?? 'http://localhost:5173';
const guestUsesHttps = guestBaseUrl.startsWith('https://');

// Cổng anti-drift FE (browser thật): tự khởi Vite dev guest-web rồi chạy assertion responsive đa-viewport.
// Song song INV-1..6 của BE — "mở web bằng browser phát hiện lỗi". CI thêm job FE gọi lệnh này.
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: process.env.CI ? 'github' : 'list',
  use: {
    baseURL: guestBaseUrl,
    ignoreHTTPSErrors: guestUsesHttps,
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  // Hai SPA: guest-web (5173) + admin-web (5174). Test admin dùng URL tuyệt đối :5174.
  webServer: [
    {
      command: 'pnpm --filter @starhill/guest-web dev',
      url: guestBaseUrl,
      cwd: '..',
      reuseExistingServer: !process.env.CI,
      ignoreHTTPSErrors: guestUsesHttps,
      timeout: 120_000,
    },
    {
      command: 'pnpm --filter @starhill/admin-web dev',
      url: 'http://localhost:5174',
      cwd: '..',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
  ],
});
