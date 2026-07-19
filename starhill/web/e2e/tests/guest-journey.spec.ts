import { test, expect, type Page } from '@playwright/test';

// FE.5a Guest Journey (browser thật, backend mock qua page.route). Phủ: resolve→home gated → force-read
// (Next disabled tới hết MinReadSeconds + scroll-end) → acknowledge → home unlock. + no-overflow + no-console-error.
const TOKEN = 'A'.repeat(43);

const RESOLVED = {
  room: { id: '00000000-0000-0000-0000-000000000001', number: '01', building: 'A', floor: 1 },
  resort: { id: '00000000-0000-0000-0000-000000000002', name: 'Star Hill Resort', logoUrl: null },
  languages: ['vi', 'en'],
  defaultLanguage: 'en',
  visit: { id: '00000000-0000-0000-0000-000000000003', portalWindowExpiresAt: '2099-01-01T00:00:00Z' },
  features: {
    faqEnabled: true, chatEnabled: true, housekeepingEnabled: true,
    ruleAckRequiredForFaq: true, ruleAckRequiredForChat: true, ruleAckRequiredForHousekeeping: false,
  },
};

const RULES = {
  publicationId: '10000000-0000-0000-0000-000000000003',
  version: 3,
  language: 'en',
  sections: [
    { key: 'welcome', sortOrder: 10, isRequired: true, requireScrollEnd: false, minReadSeconds: 1, title: 'Welcome', bodyHtmlSanitized: '<p>Please keep your key.</p>', resolvedLanguage: 'en', isFallback: false, isMissing: false },
    { key: 'quiet', sortOrder: 20, isRequired: true, requireScrollEnd: true, minReadSeconds: 0, title: 'Quiet hours', bodyHtmlSanitized: '<p>Quiet from 22:00 to 07:00.</p>', resolvedLanguage: 'en', isFallback: false, isMissing: false },
  ],
};

async function mockJourney(page: Page): Promise<void> {
  await page.route('**/v1/guest/resolve', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(RESOLVED) }),
  );
  // Khớp CẢ GET /v1/guest/rules?... (list) VÀ POST /v1/guest/rules/acknowledge → dispatch theo method.
  await page.route(/\/v1\/guest\/rules/, (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ rulePublicationId: RULES.publicationId, version: RULES.version, alreadyAcknowledged: false }) });
    }
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(RULES) });
  });
}

test('force-read gates until read + scroll-end, then acknowledge unlocks features', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(`console.error: ${m.text()}`); });

  await mockJourney(page);
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);

  // Home gated: must-read notice + faq tile locked (ackRequiredForFaq=true, chưa ack).
  await expect(page.getByTestId('must-read-rules')).toBeVisible();
  await expect(page.getByTestId('tile-faq')).toBeVisible();

  // Bấm faq (khoá) → điều hướng /rules.
  await page.getByTestId('tile-faq').click();
  await expect(page).toHaveURL(/\/rules$/);

  // Section 1: minReadSeconds=1 → Next disabled tới hết đếm ngược.
  await expect(page.getByTestId('rules-progress')).toHaveText('1/2');
  await expect(page.getByTestId('rules-next')).toBeDisabled();
  await page.waitForTimeout(1300);
  await expect(page.getByTestId('rules-next')).toBeEnabled();
  await page.getByTestId('rules-next').click();

  // Section 2 (cuối): requireScrollEnd. Cuộn sentinel vào viewport để thoả scroll-end (nếu nội dung đã hiện hết
  // thì IntersectionObserver thoả ngay — hành vi đúng: không bắt cuộn khi đã thấy toàn bộ).
  await expect(page.getByTestId('rules-progress')).toHaveText('2/2');
  await page.locator('.rules__sentinel').scrollIntoViewIfNeeded();

  // Đủ điều kiện → checkbox + confirm.
  await expect(page.getByTestId('rules-agree')).toBeVisible();
  await page.getByTestId('rules-agree').check();
  await page.getByTestId('rules-confirm').click();

  // Ack xong → về home, mở khoá (không còn must-read).
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByTestId('must-read-rules')).toHaveCount(0);

  expect(errors, `console/page errors:\n${errors.join('\n')}`).toEqual([]);
});

const VIEWPORTS = [
  { name: 'phone-390', width: 390, height: 844 },
  { name: 'tablet-820', width: 820, height: 1180 },
  { name: 'desktop-1280', width: 1280, height: 800 },
];

for (const vp of VIEWPORTS) {
  test(`guest journey no-overflow @ ${vp.name}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (m) => { if (m.type() === 'error') errors.push(`console.error: ${m.text()}`); });

    await mockJourney(page);
    await page.setViewportSize({ width: vp.width, height: vp.height });
    await page.goto(`/r/${TOKEN}`);
    await expect(page).toHaveURL(/\/$/);
    await page.goto('/rules');
    await expect(page.getByTestId('rules-progress')).toBeVisible();

    const m = await page.evaluate(() => {
      const el = document.scrollingElement ?? document.documentElement;
      return { sw: el.scrollWidth, cw: el.clientWidth };
    });
    expect(m.sw, `overflow @ ${vp.name}: ${m.sw}>${m.cw}`).toBeLessThanOrEqual(m.cw + 1);
    expect(errors, `errors @ ${vp.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}

// Ảnh giao diện luồng thật (home shell + rules force-read) cho user XEM.
test('screenshot guest home + rules', async ({ page }) => {
  await mockJourney(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);
  await page.screenshot({ path: '../screenshots/guest-home-shell-390.png', fullPage: true });
  await page.goto('/rules');
  await expect(page.getByTestId('rules-progress')).toBeVisible();
  await page.screenshot({ path: '../screenshots/guest-rules-force-read-390.png', fullPage: true });
});
