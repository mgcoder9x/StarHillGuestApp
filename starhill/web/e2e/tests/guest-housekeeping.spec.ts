import { test, expect, type Page } from '@playwright/test';

// FE.5d Guest Housekeeping (browser thật, backend mock). Phủ: home→housekeeping, yêu cầu→trạng thái (poll),
// idempotent open-hint, gating (ackRequiredForHousekeeping→/rules), no-overflow + no-console-error.
const TOKEN = 'A'.repeat(43);

function resolved(ackReq: boolean) {
  return {
    room: { id: '00000000-0000-0000-0000-000000000001', number: '01', building: 'A', floor: 1 },
    resort: { id: '00000000-0000-0000-0000-000000000002', name: 'Star Hill Resort', logoUrl: null },
    languages: ['en', 'vi'],
    defaultLanguage: 'en',
    visit: { id: '00000000-0000-0000-0000-000000000003', portalWindowExpiresAt: '2099-01-01T00:00:00Z' },
    features: {
      faqEnabled: true, chatEnabled: true, housekeepingEnabled: true,
      ruleAckRequiredForFaq: false, ruleAckRequiredForChat: false, ruleAckRequiredForHousekeeping: ackReq,
    },
  };
}

async function mockHk(page: Page, ackReq: boolean): Promise<void> {
  let ticket: { ticketId: string; roomId: string; status: string; createdAt: string; startedAt: string | null; completedAt: string | null } | null = null;

  await page.route('**/v1/guest/resolve', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(resolved(ackReq)) }),
  );
  await page.route('**/v1/guest/housekeeping**', (route) => {
    const method = route.request().method();
    if (method === 'POST') {
      const now = new Date().toISOString();
      const open = ticket && (ticket.status === 'Requested' || ticket.status === 'InProgress');
      if (!open) {
        ticket = { ticketId: '50000000-0000-0000-0000-000000000001', roomId: '00000000-0000-0000-0000-000000000001', status: 'Requested', createdAt: now, startedAt: null, completedAt: null };
        return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ticketId: ticket.ticketId, status: 'Requested', alreadyOpen: false }) });
      }
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ticketId: ticket!.ticketId, status: ticket!.status, alreadyOpen: true }) });
    }
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ticket }) });
  });
}

test('guest requests housekeeping and sees the status', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(`console.error: ${m.text()}`); });

  await mockHk(page, false);
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);

  await page.getByTestId('tile-housekeeping').click();
  await expect(page).toHaveURL(/\/housekeeping$/);
  await expect(page.getByTestId('hk-empty')).toBeVisible();
  await expect(page.getByTestId('hk-request')).toBeVisible();

  await page.getByTestId('hk-request').click();
  await expect(page.getByTestId('hk-status')).toBeVisible();
  await expect(page.getByTestId('hk-open-hint')).toBeVisible();
  await expect(page.getByTestId('hk-request')).toHaveCount(0);

  expect(errors, `errors:\n${errors.join('\n')}`).toEqual([]);
});

test('housekeeping is gated to rules when acknowledgement is required and missing', async ({ page }) => {
  await mockHk(page, true);
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);
  await page.goto('/housekeeping');
  await expect(page).toHaveURL(/\/rules$/);
});

const VIEWPORTS = [
  { name: 'phone-390', width: 390, height: 844 },
  { name: 'desktop-1280', width: 1280, height: 800 },
];
for (const vp of VIEWPORTS) {
  test(`housekeeping no-overflow @ ${vp.name}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (m) => { if (m.type() === 'error') errors.push(`console.error: ${m.text()}`); });

    await mockHk(page, false);
    await page.setViewportSize({ width: vp.width, height: vp.height });
    await page.goto(`/r/${TOKEN}`);
    await expect(page).toHaveURL(/\/$/);
    await page.goto('/housekeeping');
    await expect(page.getByTestId('hk-request')).toBeVisible();

    const m = await page.evaluate(() => {
      const el = document.scrollingElement ?? document.documentElement;
      return { sw: el.scrollWidth, cw: el.clientWidth };
    });
    expect(m.sw, `overflow @ ${vp.name}: ${m.sw}>${m.cw}`).toBeLessThanOrEqual(m.cw + 1);
    expect(errors, `errors @ ${vp.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}
