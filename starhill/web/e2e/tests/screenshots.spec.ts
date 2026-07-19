import { test, type Page } from '@playwright/test';

// Bộ CHỤP ẢNH giao diện guest THẬT (không phải assert) — qua luồng resolve→home để phản ánh app thật
// (route `/` redirect /rescan khi chưa scan, nên phải vào qua /r/{token}). Ảnh ở starhill/web/screenshots/ (gitignore).
const TOKEN = 'A'.repeat(43);
const RESOLVED = {
  room: { id: '00000000-0000-0000-0000-000000000001', number: '101', building: 'A', floor: 1 },
  resort: { id: '00000000-0000-0000-0000-000000000002', name: 'Star Hill Resort', logoUrl: null },
  languages: ['vi', 'en', 'ko', 'zh'],
  defaultLanguage: 'en',
  visit: { id: '00000000-0000-0000-0000-000000000003', portalWindowExpiresAt: '2099-01-01T00:00:00Z' },
  features: {
    faqEnabled: true, chatEnabled: true, housekeepingEnabled: true,
    ruleAckRequiredForFaq: true, ruleAckRequiredForChat: true, ruleAckRequiredForHousekeeping: false,
  },
};

async function mockResolve(page: Page, defaultLanguage: string): Promise<void> {
  await page.route('**/v1/guest/resolve', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ...RESOLVED, defaultLanguage }) }),
  );
}

const SHOTS = [
  { name: 'guest-home-phone-390', width: 390, height: 844 },
  { name: 'guest-home-largephone-430', width: 430, height: 932 },
  { name: 'guest-home-tablet-820', width: 820, height: 1180 },
  { name: 'guest-home-desktop-1280', width: 1280, height: 800 },
];

for (const s of SHOTS) {
  test(`screenshot ${s.name}`, async ({ page }) => {
    await mockResolve(page, 'en');
    await page.setViewportSize({ width: s.width, height: s.height });
    await page.goto(`/r/${TOKEN}`);
    await page.waitForURL(/\/$/);
    await page.waitForLoadState('networkidle');
    await page.screenshot({ path: `../screenshots/${s.name}.png`, fullPage: true });
  });
}

// Bản tiếng Việt (resolve trả defaultLanguage=vi).
test('screenshot guest-home-vi-phone-390', async ({ page }) => {
  await mockResolve(page, 'vi');
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/r/${TOKEN}`);
  await page.waitForURL(/\/$/);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: '../screenshots/guest-home-vi-phone-390.png', fullPage: true });
});
