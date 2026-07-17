import { test } from '@playwright/test';

// KHÔNG phải test assert — đây là bộ CHỤP ẢNH giao diện để người dùng XEM (yêu cầu "cho tôi xem giao diện").
// Ảnh lưu ở starhill/web/screenshots/ (gitignore). Chạy: pnpm --filter @starhill/e2e exec playwright test screenshots.
const SHOTS = [
  { name: 'guest-home-phone-390', width: 390, height: 844 },
  { name: 'guest-home-largephone-430', width: 430, height: 932 },
  { name: 'guest-home-tablet-820', width: 820, height: 1180 },
  { name: 'guest-home-desktop-1280', width: 1280, height: 800 },
];

for (const s of SHOTS) {
  test(`screenshot ${s.name}`, async ({ page }) => {
    await page.setViewportSize({ width: s.width, height: s.height });
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: `../screenshots/${s.name}.png`, fullPage: true });
  });
}

// Bản tiếng Việt (auto-detect qua navigator.language) — chứng minh đa ngôn ngữ.
test.describe('vietnamese', () => {
  test.use({ locale: 'vi-VN' });
  test('screenshot guest-home-vi-phone-390', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: '../screenshots/guest-home-vi-phone-390.png', fullPage: true });
  });
});
