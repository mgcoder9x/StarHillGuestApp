import { test, expect, type Page, type Route } from '@playwright/test';

const ADMIN = 'http://localhost:5174';
const STAFF_TOKEN = 'eyJhbGciOiJub25lIn0.eyJyb2xlIjoic3RhZmYifQ.test';

interface Translation {
  translationId: string;
  rowVersion: number;
  languageCode: string;
  name?: string | null;
  question?: string | null;
  answerHtmlSanitized?: string | null;
}

interface Item {
  itemId: string;
  rowVersion: number;
  categoryId: string;
  parentId: string | null;
  sortOrder: number;
  isActive: boolean;
  missingLanguages: string[];
  translations: Translation[];
  children: Item[];
}

interface Category {
  categoryId: string;
  rowVersion: number;
  key: string;
  sortOrder: number;
  isActive: boolean;
  missingLanguages: string[];
  translations: Translation[];
  items: Item[];
}

interface MockState {
  categoryUpdates: Array<Record<string, unknown>>;
  categoryTranslationUpdates: Array<Record<string, unknown>>;
  itemCreates: Array<Record<string, unknown>>;
  itemUpdates: Array<Record<string, unknown>>;
  itemTranslationUpdates: Array<Record<string, unknown>>;
  categoryReorders: Array<Array<Record<string, unknown>>>;
  conflictNextCategoryUpdate: boolean;
}

function json(route: Route, body: unknown, status = 200): Promise<void> {
  return route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });
}

function findItem(items: Item[], id: string): Item | null {
  for (const item of items) {
    if (item.itemId === id) return item;
    const child = findItem(item.children, id);
    if (child) return child;
  }
  return null;
}

function removeItem(items: Item[], id: string): Item | null {
  const index = items.findIndex((item) => item.itemId === id);
  if (index >= 0) return items.splice(index, 1)[0];
  for (const item of items) {
    const removed = removeItem(item.children, id);
    if (removed) return removed;
  }
  return null;
}

async function mockApi(page: Page): Promise<MockState> {
  const state: MockState = {
    categoryUpdates: [],
    categoryTranslationUpdates: [],
    itemCreates: [],
    itemUpdates: [],
    itemTranslationUpdates: [],
    categoryReorders: [],
    conflictNextCategoryUpdate: false,
  };
  let rowVersion = 200;
  let sequence = 10;
  const nextVersion = (): number => ++rowVersion;
  const nextId = (prefix: string): string => prefix.padEnd(8, '0') + '-0000-0000-0000-' + String(sequence++).padStart(12, '0');
  const categories: Category[] = [
    {
      categoryId: '21000000-0000-0000-0000-000000000001',
      rowVersion: 101,
      key: 'arrival',
      sortOrder: 10,
      isActive: true,
      missingLanguages: ['ko'],
      translations: [
        { translationId: '22000000-0000-0000-0000-000000000001', rowVersion: 102, languageCode: 'vi', name: 'Nhận phòng' },
        { translationId: '22000000-0000-0000-0000-000000000002', rowVersion: 103, languageCode: 'en', name: 'Arrival' },
      ],
      items: [
        {
          itemId: '23000000-0000-0000-0000-000000000001',
          rowVersion: 104,
          categoryId: '21000000-0000-0000-0000-000000000001',
          parentId: null,
          sortOrder: 10,
          isActive: true,
          missingLanguages: ['en', 'ko'],
          translations: [{ translationId: '24000000-0000-0000-0000-000000000001', rowVersion: 105, languageCode: 'vi', question: 'Nhận phòng lúc mấy giờ?', answerHtmlSanitized: '<p>Sau 14:00.</p>' }],
          children: [
            {
              itemId: '23000000-0000-0000-0000-000000000002',
              rowVersion: 106,
              categoryId: '21000000-0000-0000-0000-000000000001',
              parentId: '23000000-0000-0000-0000-000000000001',
              sortOrder: 10,
              isActive: false,
              missingLanguages: ['en', 'ko'],
              translations: [{ translationId: '24000000-0000-0000-0000-000000000002', rowVersion: 107, languageCode: 'vi', question: 'Gửi hành lý ở đâu?', answerHtmlSanitized: '<p>Tại quầy lễ tân.</p>' }],
              children: [],
            },
          ],
        },
        {
          itemId: '23000000-0000-0000-0000-000000000003',
          rowVersion: 108,
          categoryId: '21000000-0000-0000-0000-000000000001',
          parentId: null,
          sortOrder: 20,
          isActive: true,
          missingLanguages: ['en', 'ko'],
          translations: [{ translationId: '24000000-0000-0000-0000-000000000003', rowVersion: 109, languageCode: 'vi', question: 'Có xe đưa đón không?', answerHtmlSanitized: '<p>Vui lòng liên hệ lễ tân.</p>' }],
          children: [],
        },
      ],
    },
    {
      categoryId: '21000000-0000-0000-0000-000000000002',
      rowVersion: 110,
      key: 'dining',
      sortOrder: 20,
      isActive: false,
      missingLanguages: ['en', 'ko'],
      translations: [{ translationId: '22000000-0000-0000-0000-000000000003', rowVersion: 111, languageCode: 'vi', name: 'Ăn uống' }],
      items: [],
    },
  ];
  const tree = { enabledLanguageCodes: ['vi', 'en', 'ko'], defaultLanguageCode: 'vi', categories };
  const allItems = (): Item[] => categories.flatMap((category) => {
    const collect = (items: Item[]): Item[] => items.flatMap((item) => [item, ...collect(item.children)]);
    return collect(category.items);
  });

  await page.route('**/v1/token/login', (route) => json(route, { accessToken: STAFF_TOKEN, refreshToken: 'fake.refresh', refreshTokenExpiresAt: '2026-07-19T00:00:00Z' }));
  await page.route('**/v1/dashboard/stats', (route) => json(route, { unreadConversations: 5, openConversations: 3, openHousekeepingTickets: 7, activeRooms: 42, rulesAcksToday: 11 }));
  await page.route('**/v1/faq/admin', (route) => json(route, tree));

  await page.route(/\/v1\/faq\/categories\/[^/]+\/translations\/[^/]+$/, async (route) => {
    const categoryId = route.request().url().split('/').at(-3);
    const languageCode = decodeURIComponent(route.request().url().split('/').at(-1) ?? '');
    const body = route.request().postDataJSON() as Record<string, unknown>;
    state.categoryTranslationUpdates.push(body);
    const category = categories.find((entry) => entry.categoryId === categoryId);
    if (!category) return json(route, { code: 'faq_category_not_found' }, 404);
    const existing = category.translations.find((entry) => entry.languageCode === languageCode);
    if ((existing?.rowVersion ?? null) !== body.expectedRowVersion) return json(route, { code: 'concurrency_conflict' }, 409);
    if (existing) {
      existing.name = body.name as string | null;
      existing.rowVersion = nextVersion();
    } else {
      category.translations.push({ translationId: nextId('22'), rowVersion: nextVersion(), languageCode, name: body.name as string | null });
    }
    category.missingLanguages = tree.enabledLanguageCodes.filter((language) => !category.translations.some((entry) => entry.languageCode === language && Boolean(entry.name?.trim())));
    return json(route, { translationId: existing?.translationId ?? category.translations.at(-1)?.translationId });
  });
  await page.route(/\/v1\/faq\/categories\/[^/]+(\?|$)/, async (route) => {
    const url = new URL(route.request().url());
    const categoryId = url.pathname.split('/').at(-1);
    const categoryIndex = categories.findIndex((entry) => entry.categoryId === categoryId);
    if (categoryIndex < 0) return json(route, { code: 'faq_category_not_found' }, 404);
    const category = categories[categoryIndex];
    if (route.request().method() === 'PUT') {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.categoryUpdates.push(body);
      if (state.conflictNextCategoryUpdate || category.rowVersion !== body.expectedRowVersion) {
        state.conflictNextCategoryUpdate = false;
        return json(route, { code: 'concurrency_conflict' }, 409);
      }
      category.sortOrder = body.sortOrder as number;
      category.isActive = body.isActive as boolean;
      category.rowVersion = nextVersion();
      return route.fulfill({ status: 204 });
    }
    if (route.request().method() === 'DELETE') {
      if (category.items.length) return json(route, { code: 'faq_category_not_empty' }, 409);
      if (category.rowVersion !== Number(url.searchParams.get('expectedRowVersion'))) return json(route, { code: 'concurrency_conflict' }, 409);
      categories.splice(categoryIndex, 1);
      return route.fulfill({ status: 204 });
    }
    return route.fallback();
  });
  await page.route(/\/v1\/faq\/categories$/, async (route) => {
    if (route.request().method() !== 'POST') return route.fallback();
    const body = route.request().postDataJSON() as Record<string, unknown>;
    const category: Category = {
      categoryId: nextId('21'),
      rowVersion: nextVersion(),
      key: body.key as string,
      sortOrder: body.sortOrder as number,
      isActive: body.isActive as boolean,
      missingLanguages: [...tree.enabledLanguageCodes],
      translations: [],
      items: [],
    };
    categories.push(category);
    return json(route, { categoryId: category.categoryId }, 201);
  });

  await page.route(/\/v1\/faq\/items\/[^/]+\/translations\/[^/]+$/, async (route) => {
    const itemId = route.request().url().split('/').at(-3);
    const languageCode = decodeURIComponent(route.request().url().split('/').at(-1) ?? '');
    const body = route.request().postDataJSON() as Record<string, unknown>;
    state.itemTranslationUpdates.push(body);
    const item = allItems().find((entry) => entry.itemId === itemId);
    if (!item) return json(route, { code: 'faq_item_not_found' }, 404);
    const existing = item.translations.find((entry) => entry.languageCode === languageCode);
    if ((existing?.rowVersion ?? null) !== body.expectedRowVersion) return json(route, { code: 'concurrency_conflict' }, 409);
    if (existing) {
      existing.question = body.question as string | null;
      existing.answerHtmlSanitized = body.answerHtml as string | null;
      existing.rowVersion = nextVersion();
    } else {
      item.translations.push({ translationId: nextId('24'), rowVersion: nextVersion(), languageCode, question: body.question as string | null, answerHtmlSanitized: body.answerHtml as string | null });
    }
    return json(route, { translationId: existing?.translationId ?? item.translations.at(-1)?.translationId });
  });
  await page.route(/\/v1\/faq\/items\/[^/]+(\?|$)/, async (route) => {
    const url = new URL(route.request().url());
    const itemId = url.pathname.split('/').at(-1) ?? '';
    const item = allItems().find((entry) => entry.itemId === itemId);
    if (!item) return json(route, { code: 'faq_item_not_found' }, 404);
    if (route.request().method() === 'PUT') {
      const body = route.request().postDataJSON() as Record<string, unknown>;
      state.itemUpdates.push(body);
      if (item.rowVersion !== body.expectedRowVersion) return json(route, { code: 'concurrency_conflict' }, 409);
      item.sortOrder = body.sortOrder as number;
      item.isActive = body.isActive as boolean;
      item.parentId = body.parentId as string | null;
      item.rowVersion = nextVersion();
      return route.fulfill({ status: 204 });
    }
    if (route.request().method() === 'DELETE') {
      if (item.children.length) return json(route, { code: 'faq_item_has_children' }, 409);
      if (item.rowVersion !== Number(url.searchParams.get('expectedRowVersion'))) return json(route, { code: 'concurrency_conflict' }, 409);
      for (const category of categories) {
        if (removeItem(category.items, itemId)) break;
      }
      return route.fulfill({ status: 204 });
    }
    return route.fallback();
  });
  await page.route(/\/v1\/faq\/items$/, async (route) => {
    if (route.request().method() !== 'POST') return route.fallback();
    const body = route.request().postDataJSON() as Record<string, unknown>;
    state.itemCreates.push(body);
    const category = categories.find((entry) => entry.categoryId === body.categoryId);
    if (!category) return json(route, { code: 'faq_category_not_found' }, 404);
    const item: Item = {
      itemId: nextId('23'),
      rowVersion: nextVersion(),
      categoryId: category.categoryId,
      parentId: body.parentId as string | null,
      sortOrder: body.sortOrder as number,
      isActive: body.isActive as boolean,
      missingLanguages: [...tree.enabledLanguageCodes],
      translations: [],
      children: [],
    };
    const parent = item.parentId ? findItem(category.items, item.parentId) : null;
    (parent?.children ?? category.items).push(item);
    return json(route, { itemId: item.itemId }, 201);
  });
  await page.route('**/v1/faq/reorder/categories', async (route) => {
    const entries = (route.request().postDataJSON() as { entries: Array<Record<string, unknown>> }).entries;
    state.categoryReorders.push(entries);
    if (entries.some((entry) => categories.find((category) => category.categoryId === entry.id)?.rowVersion !== entry.expectedRowVersion)) {
      return json(route, { code: 'concurrency_conflict' }, 409);
    }
    for (const entry of entries) {
      const category = categories.find((candidate) => candidate.categoryId === entry.id);
      if (category) {
        category.sortOrder = entry.sortOrder as number;
        category.rowVersion = nextVersion();
      }
    }
    return route.fulfill({ status: 204 });
  });
  await page.route(/\/v1\/faq\/reorder\/items\/[^/]+$/, async (route) => {
    const entries = (route.request().postDataJSON() as { entries: Array<Record<string, unknown>> }).entries;
    if (entries.some((entry) => allItems().find((item) => item.itemId === entry.id)?.rowVersion !== entry.expectedRowVersion)) {
      return json(route, { code: 'concurrency_conflict' }, 409);
    }
    for (const entry of entries) {
      const item = allItems().find((candidate) => candidate.itemId === entry.id);
      if (item) {
        item.sortOrder = entry.sortOrder as number;
        item.rowVersion = nextVersion();
      }
    }
    return route.fulfill({ status: 204 });
  });
  return state;
}

async function loginToFaq(page: Page): Promise<void> {
  await page.goto(`${ADMIN}/login`);
  await page.locator('#username').fill('staff');
  await page.locator('#password').fill('secret-123');
  await page.getByRole('button', { name: 'Đăng nhập' }).click();
  await expect(page).toHaveURL(`${ADMIN}/`);
  await expect(page.getByRole('heading', { name: 'Tổng quan vận hành' })).toBeVisible();
  await page.locator('.admin__sidebar').getByRole('link', { name: 'FAQ' }).click();
  await expect(page).toHaveURL(`${ADMIN}/faq`);
  await expect(page.getByRole('heading', { name: 'FAQ', exact: true })).toBeVisible();
  await expect(page.getByTestId('faq-category-arrival')).toBeVisible();
}

test('FAQ renders full hierarchy including inactive and missing-language indicators', async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1280, height: 900 });
  await loginToFaq(page);
  await expect(page.getByText('Nhận phòng lúc mấy giờ?', { exact: true })).toBeVisible();
  await expect(page.getByText('Gửi hành lý ở đâu?', { exact: true })).toBeVisible();
  await expect(page.getByText('Đã ẩn', { exact: true })).toHaveCount(2);
  await expect(page.getByText('Thiếu 1 ngôn ngữ', { exact: true })).toBeVisible();
});

test('category update and new translation send observed row versions', async ({ page }) => {
  const state = await mockApi(page);
  await loginToFaq(page);
  await page.locator('#faq-category-sort').fill('15');
  await page.getByTestId('faq-save-category').click();
  await expect.poll(() => state.categoryUpdates.length).toBe(1);
  expect(state.categoryUpdates[0].expectedRowVersion).toBe(101);

  await page.getByTestId('faq-language-ko').click();
  await page.locator('#faq-category-name').fill('도착');
  await page.getByTestId('faq-save-category-translation').click();
  await expect.poll(() => state.categoryTranslationUpdates.length).toBe(1);
  expect(state.categoryTranslationUpdates[0].expectedRowVersion).toBeNull();
});

test('item update, missing translation, child create and delete round-trip safely', async ({ page }) => {
  const state = await mockApi(page);
  await loginToFaq(page);
  const itemId = '23000000-0000-0000-0000-000000000003';
  await page.getByTestId('faq-item-' + itemId).click();
  await page.locator('#faq-item-sort').fill('25');
  await page.getByTestId('faq-save-item').click();
  await expect.poll(() => state.itemUpdates.length).toBe(1);
  expect(state.itemUpdates[0].expectedRowVersion).toBe(108);

  await page.getByTestId('faq-language-en').click();
  await page.locator('#faq-item-question').fill('Is there a shuttle?');
  await page.locator('#faq-item-answer').fill('<p>Please contact reception.</p>');
  await page.getByTestId('faq-save-item-translation').click();
  await expect.poll(() => state.itemTranslationUpdates.length).toBe(1);
  expect(state.itemTranslationUpdates[0].expectedRowVersion).toBeNull();

  await page.locator('.faq__tools').getByRole('button', { name: 'Thêm câu hỏi' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Thêm câu hỏi' }).click();
  await expect.poll(() => state.itemCreates.length).toBe(1);
  expect(state.itemCreates[0].parentId).toBe(itemId);
  await page.getByRole('button', { name: 'Xoá câu hỏi' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Xoá câu hỏi' }).click();
  await expect(page.getByText('Đã xoá nội dung FAQ.')).toBeVisible();
});

test('category reorder sends every current row version', async ({ page }) => {
  const state = await mockApi(page);
  await loginToFaq(page);
  const firstRow = page.getByTestId('faq-category-arrival').locator('..');
  await firstRow.getByRole('button', { name: 'Đưa xuống' }).click();
  await expect.poll(() => state.categoryReorders.length).toBe(1);
  expect(state.categoryReorders[0].map((entry) => entry.expectedRowVersion)).toEqual([110, 101]);
  await expect(page.locator('.faq-tree__row--category').first()).toContainText('Ăn uống');
});

test('stale FAQ edit shows a concurrency conflict', async ({ page }) => {
  const state = await mockApi(page);
  state.conflictNextCategoryUpdate = true;
  await loginToFaq(page);
  await page.locator('#faq-category-sort').fill('30');
  await page.getByTestId('faq-save-category').click();
  await expect(page.getByText('FAQ đã thay đổi ở cửa sổ khác. Hãy tải lại rồi thực hiện lại.')).toBeVisible();
});

const VIEWPORTS = [
  { name: 'phone-390', width: 390, height: 844 },
  { name: 'tablet-820', width: 820, height: 1180 },
  { name: 'desktop-1280', width: 1280, height: 800 },
];

for (const viewport of VIEWPORTS) {
  test(`FAQ no horizontal overflow + no console errors @ ${viewport.name}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (event) => errors.push('pageerror: ' + event.message));
    page.on('console', (message) => { if (message.type() === 'error') errors.push('console.error: ' + message.text()); });
    await mockApi(page);
    await page.setViewportSize({ width: 1280, height: 800 });
    await loginToFaq(page);
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    await page.waitForTimeout(300);
    const metrics = await page.evaluate(() => {
      const element = document.scrollingElement ?? document.documentElement;
      return { scrollWidth: element.scrollWidth, clientWidth: element.clientWidth };
    });
    expect(metrics.scrollWidth, `FAQ overflow @ ${viewport.name}`).toBeLessThanOrEqual(metrics.clientWidth + 1);
    expect(errors, `FAQ console/page errors @ ${viewport.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}

test('screenshot FAQ desktop + phone', async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1280, height: 900 });
  await loginToFaq(page);
  await page.screenshot({ path: '../screenshots/admin-faq-desktop.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(300);
  await page.screenshot({ path: '../screenshots/admin-faq-phone.png', fullPage: true });
});
