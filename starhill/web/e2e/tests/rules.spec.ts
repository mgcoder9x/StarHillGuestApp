import { test, expect, type Page, type Route } from '@playwright/test';

const ADMIN = 'http://localhost:5174';
const STAFF_TOKEN = 'eyJhbGciOiJub25lIn0.eyJyb2xlIjoic3RhZmYifQ.test';

interface MockState {
  previewLanguages: Array<string | null>;
  sectionUpdates: Array<Record<string, unknown>>;
  translationUpdates: Array<Record<string, unknown>>;
  publicationBodies: Array<Record<string, unknown>>;
  conflictNextSectionUpdate: boolean;
}

interface RuleTranslation {
  translationId: string;
  rowVersion: number;
  languageCode: string;
  title: string | null;
  bodyHtmlSanitized: string | null;
}

interface RuleSection {
  sectionId: string;
  rowVersion: number;
  key: string;
  sortOrder: number;
  isRequired: boolean;
  requireScrollEnd: boolean;
  minReadSeconds: number;
  missingLanguages: string[];
  translations: RuleTranslation[];
}

function json(route: Route, body: unknown, status = 200): Promise<void> {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });
}

async function mockApi(page: Page): Promise<MockState> {
  const state: MockState = {
    previewLanguages: [],
    sectionUpdates: [],
    translationUpdates: [],
    publicationBodies: [],
    conflictNextSectionUpdate: false,
  };
  const draft = {
    ruleSetId: '11000000-0000-0000-0000-000000000001',
    rowVersion: 91,
    enabledLanguageCodes: ['vi', 'en', 'ko'],
    defaultLanguageCode: 'vi',
    sections: [
      {
        sectionId: '12000000-0000-0000-0000-000000000001',
        rowVersion: 92,
        key: 'welcome',
        sortOrder: 10,
        isRequired: true,
        requireScrollEnd: false,
        minReadSeconds: 5,
        missingLanguages: ['ko'],
        translations: [
          { translationId: '13000000-0000-0000-0000-000000000001', rowVersion: 93, languageCode: 'vi', title: 'Chào mừng đến Star Hill', bodyHtmlSanitized: '<p>Vui lòng giữ chìa khoá phòng.</p>' },
          { translationId: '13000000-0000-0000-0000-000000000002', rowVersion: 94, languageCode: 'en', title: 'Welcome to Star Hill', bodyHtmlSanitized: '<p>Please keep your room key with you.</p>' },
        ],
      },
      {
        sectionId: '12000000-0000-0000-0000-000000000002',
        rowVersion: 95,
        key: 'quiet-hours',
        sortOrder: 20,
        isRequired: true,
        requireScrollEnd: true,
        minReadSeconds: 12,
        missingLanguages: ['en', 'ko'],
        translations: [
          { translationId: '13000000-0000-0000-0000-000000000003', rowVersion: 96, languageCode: 'vi', title: 'Giờ yên tĩnh', bodyHtmlSanitized: '<p>Giữ yên tĩnh từ <strong>22:00 đến 07:00</strong>.</p>' },
        ],
      },
    ] as RuleSection[],
  };
  const publications = [
    { publicationId: '10000000-0000-0000-0000-000000000003', version: 3, publishedAt: '2026-07-17T03:15:00Z', publishedByUserId: '20000000-0000-0000-0000-000000000001', changeNote: 'Cập nhật giờ yên tĩnh.', isCurrent: true },
    { publicationId: '10000000-0000-0000-0000-000000000002', version: 2, publishedAt: '2026-06-10T08:30:00Z', publishedByUserId: null, changeNote: null, isCurrent: false },
  ];

  await page.route('**/v1/token/login', (route) => json(route, { accessToken: STAFF_TOKEN, refreshToken: 'fake.refresh', refreshTokenExpiresAt: '2026-07-19T00:00:00Z' }));
  await page.route('**/v1/dashboard/stats', (route) => json(route, { unreadConversations: 5, openConversations: 3, openHousekeepingTickets: 7, activeRooms: 42, rulesAcksToday: 11 }));
  await page.route('**/v1/rules/admin', (route) => json(route, draft));
  await page.route(/\/v1\/rules\/sections\/[^/]+\/translations\/[^/]+$/, async (route) => {
    const sectionId = route.request().url().split('/').at(-3);
    const languageCode = decodeURIComponent(route.request().url().split('/').at(-1) ?? '');
    const body = route.request().postDataJSON() as Record<string, unknown>;
    state.translationUpdates.push(body);
    const section = draft.sections.find((item) => item.sectionId === sectionId);
    if (!section) return json(route, { code: 'not_found' }, 404);
    const existing = section.translations.find((item) => item.languageCode === languageCode);
    if ((existing?.rowVersion ?? null) !== body.expectedRowVersion) return json(route, { code: 'concurrency_conflict' }, 409);
    if (existing) {
      existing.rowVersion += 1;
      existing.title = body.title as string | null;
      existing.bodyHtmlSanitized = body.bodyHtml as string | null;
    } else {
      section.translations.push({ translationId: '13000000-0000-0000-0000-000000000009', rowVersion: 97, languageCode, title: body.title as string | null, bodyHtmlSanitized: body.bodyHtml as string | null });
    }
    section.missingLanguages = draft.enabledLanguageCodes.filter((lang) => !section.translations.some((item) => item.languageCode === lang && Boolean(item.title?.trim() || item.bodyHtmlSanitized?.trim())));
    return json(route, { translationId: existing?.translationId ?? '13000000-0000-0000-0000-000000000009' });
  });
  await page.route(/\/v1\/rules\/sections\/[^/]+$/, async (route) => {
    if (route.request().method() !== 'PUT') return route.fallback();
    const sectionId = route.request().url().split('/').at(-1);
    const body = route.request().postDataJSON() as Record<string, unknown>;
    state.sectionUpdates.push(body);
    if (state.conflictNextSectionUpdate) {
      state.conflictNextSectionUpdate = false;
      return json(route, { code: 'concurrency_conflict' }, 409);
    }
    const section = draft.sections.find((item) => item.sectionId === sectionId);
    if (!section || section.rowVersion !== body.expectedRowVersion) return json(route, { code: 'concurrency_conflict' }, 409);
    section.sortOrder = body.sortOrder as number;
    section.isRequired = body.isRequired as boolean;
    section.requireScrollEnd = body.requireScrollEnd as boolean;
    section.minReadSeconds = body.minReadSeconds as number;
    section.rowVersion += 1;
    return route.fulfill({ status: 204 });
  });
  await page.route('**/v1/rules/publish', async (route) => {
    const body = route.request().postDataJSON() as Record<string, unknown>;
    state.publicationBodies.push(body);
    publications.forEach((item) => { item.isCurrent = false; });
    publications.unshift({ publicationId: '10000000-0000-0000-0000-000000000004', version: 4, publishedAt: '2026-07-18T03:15:00Z', publishedByUserId: null, changeNote: body.changeNote as string | null, isCurrent: true });
    return json(route, { publicationId: '10000000-0000-0000-0000-000000000004', version: 4 });
  });
  await page.route(/\/v1\/rules\/preview(\?|$)/, (route) => {
    const requestedLanguage = new URL(route.request().url()).searchParams.get('lang');
    state.previewLanguages.push(requestedLanguage);
    const english = requestedLanguage === 'en';
    return json(route, { language: requestedLanguage ?? 'vi', sections: [
      { key: 'welcome', sortOrder: 10, isRequired: true, requireScrollEnd: false, minReadSeconds: 5, title: english ? 'Welcome to Star Hill' : 'Chào mừng đến Star Hill', bodyHtmlSanitized: english ? '<p>Please keep your room key with you.</p>' : '<p>Vui lòng giữ <strong>chìa khoá phòng</strong> trong suốt kỳ nghỉ.</p>', resolvedLanguage: requestedLanguage ?? 'vi', isFallback: false, isMissing: false },
      { key: 'quiet-hours', sortOrder: 20, isRequired: true, requireScrollEnd: true, minReadSeconds: 12, title: 'Giờ yên tĩnh', bodyHtmlSanitized: '<p>Giữ yên tĩnh từ <strong>22:00 đến 07:00</strong>.</p>', resolvedLanguage: 'vi', isFallback: english, isMissing: false },
    ] });
  });
  await page.route('**/v1/rules/publications', (route) => json(route, { publications }));
  return state;
}

async function loginToRules(page: Page): Promise<void> {
  await page.goto(`${ADMIN}/login`);
  await page.locator('#username').fill('staff');
  await page.locator('#password').fill('secret-123');
  await page.getByRole('button', { name: 'Đăng nhập' }).click();
  await expect(page).toHaveURL(`${ADMIN}/`);
  await expect(page.getByRole('heading', { name: 'Tổng quan vận hành' })).toBeVisible();
  await page.locator('.admin__sidebar').getByRole('link', { name: 'Nội quy' }).click();
  await expect(page).toHaveURL(`${ADMIN}/rules`);
  await expect(page.getByRole('heading', { name: 'Xem trước nội dung' })).toBeVisible();
  await expect(page.getByTestId('rules-preview-section')).toHaveCount(2);
}

test('rules preview renders sanitized HTML, fallback state and publication history', async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1280, height: 800 });
  await loginToRules(page);
  await expect(page.locator('.rules__body strong').first()).toHaveText('chìa khoá phòng');
  await expect(page.getByTestId('rules-history-row')).toHaveCount(2);
  await expect(page.getByText('v3', { exact: true })).toBeVisible();
  await expect(page.getByText('Hiện hành', { exact: true })).toBeVisible();
});

test('changing preview language refetches and exposes server fallback', async ({ page }) => {
  const state = await mockApi(page);
  await loginToRules(page);
  await page.getByLabel('Ngôn ngữ xem trước').click();
  await page.getByRole('option', { name: 'English (en)' }).click();
  await expect(page.getByText('Welcome to Star Hill', { exact: true })).toBeVisible();
  await expect(page.getByText('Đang dùng bản mặc định', { exact: true })).toBeVisible();
  expect(state.previewLanguages).toContain('en');
});

test('editor sends row versions, creates a missing translation and refreshes publication history', async ({ page }) => {
  const state = await mockApi(page);
  await loginToRules(page);
  await page.locator('#rule-section-sort').fill('15');
  await page.getByTestId('rule-save-settings').click();
  await expect.poll(() => state.sectionUpdates.length).toBe(1);
  expect(state.sectionUpdates[0].expectedRowVersion).toBe(92);

  await page.getByTestId('rule-language-ko').click();
  await page.locator('#rule-translation-title').fill('Chào mừng');
  await page.locator('#rule-translation-body').fill('<p>Nội dung tiếng Hàn sẽ được dịch sau.</p>');
  await page.getByTestId('rule-save-translation').click();
  await expect.poll(() => state.translationUpdates.length).toBe(1);
  expect(state.translationUpdates[0].expectedRowVersion).toBeNull();

  await page.locator('.rule-editor__actions').getByRole('button', { name: 'Phát hành' }).click();
  await page.getByLabel('Ghi chú thay đổi (không bắt buộc)').fill('Cập nhật bản dịch.');
  await page.getByRole('dialog').getByRole('button', { name: 'Phát hành' }).click();
  await expect.poll(() => state.publicationBodies.length).toBe(1);
  await expect(page.getByText('v4', { exact: true })).toBeVisible();
  expect(state.publicationBodies[0].changeNote).toBe('Cập nhật bản dịch.');
});

test('stale section edit surfaces a reloadable conflict message', async ({ page }) => {
  const state = await mockApi(page);
  state.conflictNextSectionUpdate = true;
  await loginToRules(page);
  await page.locator('#rule-section-sort').fill('30');
  await page.getByTestId('rule-save-settings').click();
  await expect(page.getByText('Dữ liệu đã thay đổi ở cửa sổ khác. Hãy tải lại rồi thực hiện lại.')).toBeVisible();
});

const VIEWPORTS = [
  { name: 'phone-390', width: 390, height: 844 },
  { name: 'tablet-820', width: 820, height: 1180 },
  { name: 'desktop-1280', width: 1280, height: 800 },
];

for (const viewport of VIEWPORTS) {
  test(`rules no horizontal overflow + no console errors @ ${viewport.name}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (error) => errors.push(`pageerror: ${error.message}`));
    page.on('console', (message) => { if (message.type() === 'error') errors.push(`console.error: ${message.text()}`); });
    await mockApi(page);
    await page.setViewportSize({ width: 1280, height: 800 });
    await loginToRules(page);
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    await page.waitForTimeout(300);
    const metrics = await page.evaluate(() => { const element = document.scrollingElement ?? document.documentElement; return { scrollWidth: element.scrollWidth, clientWidth: element.clientWidth }; });
    expect(metrics.scrollWidth, `rules overflow @ ${viewport.name}`).toBeLessThanOrEqual(metrics.clientWidth + 1);
    expect(errors, `rules console/page errors @ ${viewport.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}

test('screenshot rules desktop + phone', async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1280, height: 800 });
  await loginToRules(page);
  await page.screenshot({ path: '../screenshots/admin-rules-desktop.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(300);
  await page.screenshot({ path: '../screenshots/admin-rules-phone.png', fullPage: true });
});
