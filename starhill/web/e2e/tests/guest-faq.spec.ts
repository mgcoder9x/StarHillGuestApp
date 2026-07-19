import { test, expect, type Page } from '@playwright/test';

// FE.5b Guest FAQ (browser thật, backend mock). Phủ: home→FAQ tree render + expand cha-con + CTA→chat +
// gating (ackRequiredForFaq chưa ack → /rules) + no-overflow + no-console-error.
const TOKEN = 'A'.repeat(43);

function resolved(ackReqFaq: boolean) {
  return {
    room: { id: '00000000-0000-0000-0000-000000000001', number: '01', building: 'A', floor: 1 },
    resort: { id: '00000000-0000-0000-0000-000000000002', name: 'Star Hill Resort', logoUrl: null },
    languages: ['en', 'vi'],
    defaultLanguage: 'en',
    visit: { id: '00000000-0000-0000-0000-000000000003', portalWindowExpiresAt: '2099-01-01T00:00:00Z' },
    features: {
      faqEnabled: true, chatEnabled: true, housekeepingEnabled: true,
      ruleAckRequiredForFaq: ackReqFaq, ruleAckRequiredForChat: false, ruleAckRequiredForHousekeeping: false,
    },
  };
}

const FAQ_TREE = {
  language: 'en',
  categories: [
    {
      id: '21000000-0000-0000-0000-000000000001', key: 'arrival', sortOrder: 10, name: 'Arrival & Amenities',
      resolvedLanguage: 'en', isFallback: false, isMissing: false,
      items: [
        {
          id: '23000000-0000-0000-0000-000000000001', sortOrder: 10, question: 'What is the Wi-Fi password?',
          answerHtmlSanitized: '<p>Wi-Fi: <strong>StarHill-Guest</strong>, password welcome2026.</p>',
          resolvedLanguage: 'en', isFallback: false, isMissing: false,
          children: [
            {
              id: '23000000-0000-0000-0000-000000000002', sortOrder: 10, question: 'Is there vegetarian breakfast?',
              answerHtmlSanitized: '<p>Yes, please tell reception the night before.</p>',
              resolvedLanguage: 'en', isFallback: false, isMissing: false, children: [],
            },
          ],
        },
      ],
    },
  ],
};

async function mockFaq(page: Page, ackReqFaq: boolean): Promise<void> {
  await page.route('**/v1/guest/resolve', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(resolved(ackReqFaq)) }),
  );
  await page.route(/\/v1\/guest\/faq/, (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(FAQ_TREE) }),
  );
}

test('FAQ tree renders, expands parent/child and offers a chat CTA', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(`console.error: ${m.text()}`); });

  await mockFaq(page, false);
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);

  await page.getByTestId('tile-faq').click();
  await expect(page).toHaveURL(/\/faq$/);
  await expect(page.getByTestId('faq-list')).toBeVisible();
  await expect(page.getByText('Arrival & Amenities')).toBeVisible();

  // Mở câu hỏi cha → đáp án + câu hỏi con hiện.
  await page.getByRole('button', { name: /What is the Wi-Fi password/ }).click();
  await expect(page.getByText(/StarHill-Guest/)).toBeVisible();
  await expect(page.getByText('Is there vegetarian breakfast?')).toBeVisible();

  // CTA → chat (canChat true).
  await page.getByRole('button', { name: /Message us about this/ }).first().click();
  await expect(page).toHaveURL(/\/chat/);

  expect(errors, `errors:\n${errors.join('\n')}`).toEqual([]);
});

test('FAQ is gated to rules when acknowledgement is required and missing', async ({ page }) => {
  await mockFaq(page, true);
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);
  // Vào /faq trực tiếp → guard chặn vì canFaq=false (chưa ack) → /rules.
  await page.goto('/faq');
  await expect(page).toHaveURL(/\/rules$/);
});

const VIEWPORTS = [
  { name: 'phone-390', width: 390, height: 844 },
  { name: 'desktop-1280', width: 1280, height: 800 },
];
for (const vp of VIEWPORTS) {
  test(`FAQ no-overflow @ ${vp.name}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (m) => { if (m.type() === 'error') errors.push(`console.error: ${m.text()}`); });

    await mockFaq(page, false);
    await page.setViewportSize({ width: vp.width, height: vp.height });
    await page.goto(`/r/${TOKEN}`);
    await expect(page).toHaveURL(/\/$/);
    await page.goto('/faq');
    await expect(page.getByTestId('faq-list')).toBeVisible();

    const m = await page.evaluate(() => {
      const el = document.scrollingElement ?? document.documentElement;
      return { sw: el.scrollWidth, cw: el.clientWidth };
    });
    expect(m.sw, `overflow @ ${vp.name}: ${m.sw}>${m.cw}`).toBeLessThanOrEqual(m.cw + 1);
    expect(errors, `errors @ ${vp.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}
