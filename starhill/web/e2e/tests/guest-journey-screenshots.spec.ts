import { test, type Page } from '@playwright/test';

// Bộ CHỤP ẢNH trọn hành trình guest THẬT (home→rules→faq→chat→housekeeping) cho user XEM ("mở web bằng browser").
// KHÔNG assert — ảnh ở starhill/web/screenshots/. Mock đủ 8 endpoint để mỗi màn có nội dung.
const TOKEN = 'A'.repeat(43);

const RESOLVED = {
  room: { id: '00000000-0000-0000-0000-000000000101', number: '101', building: 'A', floor: 1 },
  resort: { id: '00000000-0000-0000-0000-000000000002', name: 'Star Hill Resort', logoUrl: null },
  languages: ['vi', 'en', 'ko', 'zh'],
  defaultLanguage: 'vi',
  visit: { id: '00000000-0000-0000-0000-000000000003', portalWindowExpiresAt: '2099-01-01T00:00:00Z' },
  features: {
    faqEnabled: true, chatEnabled: true, housekeepingEnabled: true,
    ruleAckRequiredForFaq: false, ruleAckRequiredForChat: false, ruleAckRequiredForHousekeeping: false,
  },
};

const RULES = {
  publicationId: '10000000-0000-0000-0000-000000000003', version: 3, language: 'vi',
  sections: [
    { key: 'welcome', sortOrder: 10, isRequired: true, requireScrollEnd: false, minReadSeconds: 0, title: 'Chào mừng đến Star Hill', bodyHtmlSanitized: '<p>Vui lòng giữ chìa khoá phòng và thẻ QR trong suốt kỳ nghỉ.</p>', resolvedLanguage: 'vi', isFallback: false, isMissing: false },
    { key: 'quiet', sortOrder: 20, isRequired: true, requireScrollEnd: false, minReadSeconds: 0, title: 'Giờ yên tĩnh & An toàn', bodyHtmlSanitized: '<p>Giữ yên tĩnh từ <strong>22:00 đến 07:00</strong>. Khoá cửa cẩn thận khi rời phòng.</p>', resolvedLanguage: 'vi', isFallback: false, isMissing: false },
  ],
};

const FAQ = {
  language: 'vi',
  categories: [
    { id: '21000000-0000-0000-0000-000000000001', key: 'arrival', sortOrder: 10, name: 'Nhận phòng & Tiện ích', resolvedLanguage: 'vi', isFallback: false, isMissing: false,
      items: [
        { id: '23000000-0000-0000-0000-000000000001', sortOrder: 10, question: 'Mật khẩu Wi-Fi là gì?', answerHtmlSanitized: '<p>Wi-Fi: <strong>StarHill-Guest</strong>, mật khẩu welcome2026.</p>', resolvedLanguage: 'vi', isFallback: false, isMissing: false, children: [] },
        { id: '23000000-0000-0000-0000-000000000002', sortOrder: 20, question: 'Giờ ăn sáng?', answerHtmlSanitized: '<p>Buffet sáng 06:30–10:00 tại nhà hàng tầng 1.</p>', resolvedLanguage: 'vi', isFallback: false, isMissing: false, children: [] },
      ] },
  ],
};

const CONVO = {
  conversation: {
    conversationId: '30000000-0000-0000-0000-000000000001', status: 'Open', lastMessageAt: '2026-07-18T03:00:00Z',
    messages: [
      { messageId: '40000000-0000-0000-0000-000000000001', senderType: 'Guest', body: 'Cho hỏi mấy giờ trả phòng ạ?', createdAt: '2026-07-18T02:58:00Z', readByStaffAt: '2026-07-18T02:59:00Z' },
      { messageId: '40000000-0000-0000-0000-000000000002', senderType: 'Staff', body: 'Dạ trả phòng trước 12:00 trưa. Cần hỗ trợ gì thêm không ạ?', createdAt: '2026-07-18T03:00:00Z', readByStaffAt: null },
    ],
  },
};

const HK = {
  ticket: { ticketId: '50000000-0000-0000-0000-000000000001', roomId: '00000000-0000-0000-0000-000000000101', status: 'InProgress', createdAt: '2026-07-18T02:00:00Z', startedAt: '2026-07-18T02:30:00Z', completedAt: null },
};

async function mockAll(page: Page): Promise<void> {
  await page.route('**/v1/guest/resolve', (r) => r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(RESOLVED) }));
  await page.route(/\/v1\/guest\/rules/, (r) => r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(RULES) }));
  await page.route(/\/v1\/guest\/faq/, (r) => r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(FAQ) }));
  await page.route('**/v1/guest/conversation**', (r) => r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(CONVO) }));
  await page.route('**/v1/guest/housekeeping**', (r) => r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(HK) }));
}

const SHOTS: { name: string; path: string }[] = [
  { name: 'home', path: '/' },
  { name: 'rules', path: '/rules' },
  { name: 'faq', path: '/faq' },
  { name: 'chat', path: '/chat' },
  { name: 'housekeeping', path: '/housekeeping' },
];

test('capture full guest journey (phone 390)', async ({ page }) => {
  await mockAll(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/r/${TOKEN}`);
  await page.waitForURL(/\/$/);
  for (const s of SHOTS) {
    if (s.path !== '/') {
      await page.goto(s.path);
    }
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(300);
    await page.screenshot({ path: `../screenshots/guest-journey-${s.name}-390.png`, fullPage: true });
  }
});

test('capture guest home + rules (desktop 1280)', async ({ page }) => {
  await mockAll(page);
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto(`/r/${TOKEN}`);
  await page.waitForURL(/\/$/);
  await page.waitForTimeout(300);
  await page.screenshot({ path: '../screenshots/guest-journey-home-1280.png', fullPage: true });
  await page.goto('/faq');
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(300);
  await page.screenshot({ path: '../screenshots/guest-journey-faq-1280.png', fullPage: true });
});
