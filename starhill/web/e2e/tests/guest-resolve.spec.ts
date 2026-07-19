import { test, expect } from '@playwright/test';

const TOKEN = 'A'.repeat(43);

const RESOLVED = {
  room: { id: '00000000-0000-0000-0000-000000000001', number: '01', building: 'A', floor: 1 },
  resort: { id: '00000000-0000-0000-0000-000000000002', name: 'Star Hill Resort', logoUrl: null },
  languages: ['vi', 'en'],
  defaultLanguage: 'vi',
  visit: { id: '00000000-0000-0000-0000-000000000003', portalWindowExpiresAt: '2026-07-19T00:00:00Z' },
  features: {
    faqEnabled: true,
    chatEnabled: true,
    housekeepingEnabled: true,
    ruleAckRequiredForFaq: false,
    ruleAckRequiredForChat: false,
    ruleAckRequiredForHousekeeping: false,
  },
};

test('QR entry resolves the room and never renders a blank page', async ({ page }) => {
  let postedToken: string | undefined;
  await page.route('**/v1/guest/resolve', async (route) => {
    postedToken = (route.request().postDataJSON() as { token: string }).token;
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(RESOLVED) });
  });

  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByText(/Room 01|Phòng 01/)).toBeVisible();
  await expect(page.getByText(/Welcome to Star Hill Resort|Chào mừng đến Star Hill Resort/)).toBeVisible();
  expect(postedToken).toBe(TOKEN);
});

test('invalid QR shows a recovery message instead of a blank page', async ({ page }) => {
  await page.route('**/v1/guest/resolve', (route) =>
    route.fulfill({
      status: 404,
      contentType: 'application/problem+json',
      body: JSON.stringify({ code: 'qr_invalid', detail: 'QR invalid' }),
    }),
  );

  await page.goto(`/r/${TOKEN}`);
  await expect(page.getByText(/This QR code is no longer valid|Mã QR này không còn hợp lệ/)).toBeVisible();
  await expect(page.getByRole('button', { name: /Try again|Thử lại/ })).toBeVisible();
});
