import { test, expect, type Page } from '@playwright/test';

// FE.5c Guest Chat POLLING (browser thật, backend mock). Phủ: home→chat, gửi tin→hiện (poll nguồn sự thật),
// prefill từ FAQ CTA, gating (ackRequiredForChat chưa ack→/rules), no-overflow + no-console-error.
const TOKEN = 'A'.repeat(43);

function resolved(ackReqChat: boolean) {
  return {
    room: { id: '00000000-0000-0000-0000-000000000001', number: '01', building: 'A', floor: 1 },
    resort: { id: '00000000-0000-0000-0000-000000000002', name: 'Star Hill Resort', logoUrl: null },
    languages: ['en', 'vi'],
    defaultLanguage: 'en',
    visit: { id: '00000000-0000-0000-0000-000000000003', portalWindowExpiresAt: '2099-01-01T00:00:00Z' },
    features: {
      faqEnabled: true, chatEnabled: true, housekeepingEnabled: true,
      ruleAckRequiredForFaq: false, ruleAckRequiredForChat: ackReqChat, ruleAckRequiredForHousekeeping: false,
    },
  };
}

// Mock conversation STATEFUL trong closure (GET là nguồn sự thật; POST append).
async function mockChat(page: Page, ackReqChat: boolean): Promise<void> {
  interface Msg { messageId: string; senderType: string; body: string; createdAt: string; readByStaffAt: string | null }
  let convo: { conversationId: string; status: string; lastMessageAt: string; messages: Msg[] } | null = null;
  let seq = 1;

  await page.route('**/v1/guest/resolve', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(resolved(ackReqChat)) }),
  );
  await page.route('**/v1/guest/conversation**', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ conversation: convo }) }),
  );
  await page.route('**/v1/guest/messages', (route) => {
    const body = (route.request().postDataJSON() as { body: string }).body;
    const now = new Date().toISOString();
    if (!convo) convo = { conversationId: '30000000-0000-0000-0000-000000000001', status: 'Open', lastMessageAt: now, messages: [] };
    const messageId = `40000000-0000-0000-0000-${String(seq++).padStart(12, '0')}`;
    convo.messages.push({ messageId, senderType: 'Guest', body, createdAt: now, readByStaffAt: null });
    convo.status = 'Open';
    convo.lastMessageAt = now;
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ conversationId: convo.conversationId, messageId, status: 'Open', reopened: false }) });
  });
}

test('guest can send a message and it appears via polling', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(`console.error: ${m.text()}`); });

  await mockChat(page, false);
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);

  await page.getByTestId('tile-chat').click();
  await expect(page).toHaveURL(/\/chat$/);
  await expect(page.getByTestId('chat-list')).toBeVisible();

  await page.getByTestId('chat-input').fill('Xin chao le tan, cho hoi gio an sang?');
  await page.getByTestId('chat-send').click();
  await expect(page.getByText('Xin chao le tan, cho hoi gio an sang?')).toBeVisible();

  expect(errors, `errors:\n${errors.join('\n')}`).toEqual([]);
});

test('chat consumes prefill from FAQ CTA', async ({ page }) => {
  await mockChat(page, false);
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);
  await page.goto('/chat?prefill=Cau%20hoi%20ve%20Wi-Fi');
  await expect(page.getByTestId('chat-input')).toHaveValue('Cau hoi ve Wi-Fi');
});

test('chat is gated to rules when acknowledgement is required and missing', async ({ page }) => {
  await mockChat(page, true);
  await page.goto(`/r/${TOKEN}`);
  await expect(page).toHaveURL(/\/$/);
  await page.goto('/chat');
  await expect(page).toHaveURL(/\/rules$/);
});

const VIEWPORTS = [
  { name: 'phone-390', width: 390, height: 844 },
  { name: 'desktop-1280', width: 1280, height: 800 },
];
for (const vp of VIEWPORTS) {
  test(`chat no-overflow @ ${vp.name}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (m) => { if (m.type() === 'error') errors.push(`console.error: ${m.text()}`); });

    await mockChat(page, false);
    await page.setViewportSize({ width: vp.width, height: vp.height });
    await page.goto(`/r/${TOKEN}`);
    await expect(page).toHaveURL(/\/$/);
    await page.goto('/chat');
    await expect(page.getByTestId('chat-list')).toBeVisible();

    const m = await page.evaluate(() => {
      const el = document.scrollingElement ?? document.documentElement;
      return { sw: el.scrollWidth, cw: el.clientWidth };
    });
    expect(m.sw, `overflow @ ${vp.name}: ${m.sw}>${m.cw}`).toBeLessThanOrEqual(m.cw + 1);
    expect(errors, `errors @ ${vp.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}
