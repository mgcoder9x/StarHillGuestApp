import { test, expect, type Page } from '@playwright/test';

// Admin Dashboard (SPA :5174). Backend KHÔNG chạy ở máy này → MOCK /v1/identity/token/login + /v1/dashboard/stats bằng
// page.route (chặn trước network, bất kể Vite proxy). Verify: login flow → KPI hiển thị đúng + no-overflow đa-viewport
// + không lỗi console. Đây là browser-substitute cho FE admin (song song BE test).
const ADMIN = 'http://localhost:5174';

const STATS = {
  unreadConversations: 5,
  openConversations: 3,
  openHousekeepingTickets: 7,
  activeRooms: 42,
  rulesAcksToday: 11,
};

async function mockApi(page: Page): Promise<void> {
  await page.route('**/v1/identity/token/login', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        accessToken: 'fake.jwt.token',
        refreshToken: 'fake.refresh',
        refreshTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      }),
    }),
  );
  await page.route('**/v1/dashboard/stats', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(STATS) }),
  );
}

async function login(page: Page): Promise<void> {
  await page.goto(`${ADMIN}/login`);
  await page.locator('#username').fill('admin');
  await page.locator('#password').fill('secret-123');
  await page.getByRole('button', { name: 'Đăng nhập' }).click();
  await expect(page.getByTestId('kpi-unread')).toBeVisible();
}

test('login flow shows dashboard KPIs from API', async ({ page }) => {
  await mockApi(page);
  await login(page);

  await expect(page.getByTestId('kpi-unread')).toHaveText(String(STATS.unreadConversations));
  await expect(page.getByTestId('kpi-openConversations')).toHaveText(String(STATS.openConversations));
  await expect(page.getByTestId('kpi-openTickets')).toHaveText(String(STATS.openHousekeepingTickets));
  await expect(page.getByTestId('kpi-activeRooms')).toHaveText(String(STATS.activeRooms));
  await expect(page.getByTestId('kpi-acksToday')).toHaveText(String(STATS.rulesAcksToday));
});

const VIEWPORTS = [
  { name: 'phone-390', width: 390, height: 844 },
  { name: 'tablet-820', width: 820, height: 1180 },
  { name: 'desktop-1280', width: 1280, height: 800 },
];

for (const vp of VIEWPORTS) {
  test(`admin no horizontal overflow + no console errors @ ${vp.name}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (m) => {
      if (m.type() === 'error') {
        errors.push(`console.error: ${m.text()}`);
      }
    });

    await page.setViewportSize({ width: vp.width, height: vp.height });
    await mockApi(page);
    await login(page); // qua login → tới dashboard (shell + KPI).
    await page.waitForLoadState('networkidle');

    const metrics = await page.evaluate(() => {
      const el = document.scrollingElement ?? document.documentElement;
      return { scrollWidth: el.scrollWidth, clientWidth: el.clientWidth };
    });
    expect(
      metrics.scrollWidth,
      `admin overflow @ ${vp.name}: ${metrics.scrollWidth} > ${metrics.clientWidth}`,
    ).toBeLessThanOrEqual(metrics.clientWidth + 1);
    expect(errors, `admin console/page errors @ ${vp.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}

// Ảnh giao diện admin (login + dashboard) cho người dùng XEM.
test('screenshot admin login', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 800 });
  await page.goto(`${ADMIN}/login`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: '../screenshots/admin-login-desktop.png', fullPage: true });
});

test('screenshot admin dashboard desktop + phone', async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1280, height: 800 });
  await login(page);
  await page.screenshot({ path: '../screenshots/admin-dashboard-desktop.png', fullPage: true });

  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload();
  await login(page);
  await page.screenshot({ path: '../screenshots/admin-dashboard-phone.png', fullPage: true });
});
