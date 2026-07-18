import { test, expect, type Page } from '@playwright/test';

// FE.3a Admin Rooms (SPA :5174). Backend KHÔNG chạy → mock /v1/token/login + /v1/dashboard/stats + /v1/rooms +
// /v1/rooms/{id}/qr.png bằng page.route (chặn trước network). Verify: vào /rooms → bảng đúng + lọc trạng thái +
// mở dialog QR + no-overflow đa-viewport + no-console-error. Browser-substitute cho FE admin (anti-drift FE).
const ADMIN = 'http://localhost:5174';
const ADMIN_TOKEN = 'eyJhbGciOiJub25lIn0.eyJyb2xlIjoiYWRtaW4ifQ.test';
const STAFF_TOKEN = 'eyJhbGciOiJub25lIn0.eyJyb2xlIjoic3RhZmYifQ.test';

interface MockRoom {
  roomId: string;
  roomNumber: string;
  building: string | null;
  floor: number | null;
  status: 'Active' | 'Inactive' | 'Maintenance';
  activeTokenPreview: string | null;
  activeTokenVersion: number;
  createdAt: string;
}

const ALL_ROOMS: MockRoom[] = [
  { roomId: '00000000-0000-0000-0000-000000000101', roomNumber: '101', building: 'A', floor: 1, status: 'Active', activeTokenPreview: 'qr_101…', activeTokenVersion: 1, createdAt: '2026-07-01T00:00:00Z' },
  { roomId: '00000000-0000-0000-0000-000000000102', roomNumber: '102', building: 'A', floor: 1, status: 'Maintenance', activeTokenPreview: null, activeTokenVersion: 0, createdAt: '2026-07-02T00:00:00Z' },
  { roomId: '00000000-0000-0000-0000-000000000103', roomNumber: '103', building: 'B', floor: 2, status: 'Inactive', activeTokenPreview: null, activeTokenVersion: 0, createdAt: '2026-07-03T00:00:00Z' },
];

const QR_SVG =
  '<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200"><rect width="200" height="200" fill="#fff"/><rect x="20" y="20" width="60" height="60" fill="#000"/></svg>';

async function mockApi(page: Page, role: 'admin' | 'staff' = 'admin'): Promise<void> {
  const rooms = ALL_ROOMS.map((room) => ({ ...room }));
  await page.route('**/v1/token/login', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        accessToken: role === 'admin' ? ADMIN_TOKEN : STAFF_TOKEN,
        refreshToken: 'fake.refresh',
        refreshTokenExpiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      }),
    }),
  );
  await page.route('**/v1/dashboard/stats', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ unreadConversations: 5, openConversations: 3, openHousekeepingTickets: 7, activeRooms: 42, rulesAcksToday: 11 }),
    }),
  );
  await page.route(/\/v1\/rooms\/[^/]+\/rotate-token$/, (route) => {
    const roomId = route.request().url().split('/').at(-2);
    const room = rooms.find((item) => item.roomId === roomId);
    if (!room || room.status !== 'Active') {
      return route.fulfill({ status: 409, contentType: 'application/problem+json', body: JSON.stringify({ code: 'qr_generation_failed' }) });
    }
    room.activeTokenPreview = 'qr_rot…';
    room.activeTokenVersion += 1;
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ tokenPreview: room.activeTokenPreview }) });
  });
  await page.route(/\/v1\/rooms\/[^/]+\/status$/, (route) => {
    const roomId = route.request().url().split('/').at(-2);
    const room = rooms.find((item) => item.roomId === roomId);
    if (!room) return route.fulfill({ status: 404, contentType: 'application/problem+json', body: JSON.stringify({ code: 'not_found' }) });
    const body = route.request().postDataJSON() as { status: MockRoom['status'] };
    room.status = body.status;
    return route.fulfill({ status: 204 });
  });
  await page.route(/\/v1\/rooms\/[^/]+$/, (route) => {
    const roomId = route.request().url().split('/').at(-1);
    const index = rooms.findIndex((item) => item.roomId === roomId);
    if (index < 0) return route.fulfill({ status: 404, contentType: 'application/problem+json', body: JSON.stringify({ code: 'not_found' }) });
    if (route.request().method() === 'PUT') {
      const body = route.request().postDataJSON() as Pick<MockRoom, 'roomNumber' | 'building' | 'floor'>;
      Object.assign(rooms[index], body);
      return route.fulfill({ status: 204 });
    }
    if (route.request().method() === 'DELETE') {
      rooms.splice(index, 1);
      return route.fulfill({ status: 204 });
    }
    return route.fallback();
  });
  // Danh sách + create (status-aware). Regex chỉ khớp route gốc → KHÔNG đụng room detail/qr.png.
  await page.route(/\/v1\/rooms(\?|$)/, (route) => {
    if (route.request().method() === 'POST') {
      const body = route.request().postDataJSON() as Pick<MockRoom, 'roomNumber' | 'building' | 'floor'>;
      const created: MockRoom = {
        roomId: '00000000-0000-0000-0000-000000000104',
        roomNumber: body.roomNumber,
        building: body.building,
        floor: body.floor,
        status: 'Active',
        activeTokenPreview: 'qr_new…',
        activeTokenVersion: 1,
        createdAt: '2026-07-18T00:00:00Z',
      };
      rooms.push(created);
      return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ roomId: created.roomId, tokenPreview: created.activeTokenPreview }) });
    }
    const status = new URL(route.request().url()).searchParams.get('status');
    const items = status ? rooms.filter((r) => r.status === status) : rooms;
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items, page: 1, pageSize: 20, total: items.length }),
    });
  });
  // QR (SVG thay PNG cho test — client dùng blob→objectURL nên định dạng ảnh nào cũng render trong <img>).
  await page.route(/\/v1\/rooms\/[^/]+\/qr\.png/, (route) =>
    route.fulfill({ status: 200, contentType: 'image/svg+xml', body: QR_SVG }),
  );
}

async function loginToRooms(page: Page): Promise<void> {
  await page.goto(`${ADMIN}/login`);
  await page.locator('#username').fill('admin');
  await page.locator('#password').fill('secret-123');
  await page.getByRole('button', { name: 'Đăng nhập' }).click();
  // Điều hướng SPA sang /rooms (click sidebar — KHÔNG goto để giữ token in-memory).
  await page.locator('.admin__sidebar').getByRole('link', { name: 'Phòng & QR' }).click();
  await expect(page.getByText('101', { exact: true })).toBeVisible();
}

test('rooms list renders + filter by status + QR dialog', async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1280, height: 800 });
  await loginToRooms(page);

  // Bảng hiện 3 phòng.
  await expect(page.getByText('101', { exact: true })).toBeVisible();
  await expect(page.getByText('102', { exact: true })).toBeVisible();
  await expect(page.getByText('103', { exact: true })).toBeVisible();

  // Mở dialog QR trên phòng Active (101) — nút đầu tiên (enabled).
  await page.getByRole('button', { name: 'Xem QR' }).first().click();
  await expect(page.getByTestId('room-qr-img')).toBeVisible();
  // Đóng dialog.
  await page.getByRole('button', { name: 'Đóng' }).click();

  // Lọc "Bảo trì" → chỉ còn 102, mất 101.
  await page.locator('.rooms__filter').click();
  await page.getByRole('option', { name: 'Bảo trì' }).click();
  await expect(page.getByText('102', { exact: true })).toBeVisible();
  await expect(page.getByText('101', { exact: true })).toHaveCount(0);
});

test('admin can create, edit, rotate, change status and delete a room', async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1280, height: 900 });
  await loginToRooms(page);

  await page.getByRole('button', { name: 'Thêm phòng' }).click();
  await page.locator('#room-number').fill('104');
  await page.locator('#room-building').fill('C');
  await page.locator('#room-floor').fill('3');
  await page.getByRole('dialog').getByRole('button', { name: 'Lưu' }).click();
  await expect(page.getByText('104', { exact: true })).toBeVisible();

  let row = page.getByRole('row').filter({ hasText: '104' });
  await row.getByRole('button', { name: 'Sửa phòng' }).click();
  await page.locator('#room-number').fill('104A');
  await page.getByRole('dialog').getByRole('button', { name: 'Lưu' }).click();
  await expect(page.getByText('104A', { exact: true })).toBeVisible();

  row = page.getByRole('row').filter({ hasText: '104A' });
  await row.getByRole('button', { name: 'Đổi mã QR' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Đổi mã QR' }).click();
  await expect(row).toContainText('v2');

  await row.getByRole('button', { name: 'Đổi trạng thái' }).click();
  await page.locator('#room-status').click();
  await page.getByRole('option', { name: 'Bảo trì' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Lưu' }).click();
  await expect(row).toContainText('Bảo trì');

  await row.getByRole('button', { name: 'Xoá phòng' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Xoá phòng' }).click();
  await expect(page.getByText('104A', { exact: true })).toHaveCount(0);
});

test('staff sees the room list without admin mutation controls', async ({ page }) => {
  await mockApi(page, 'staff');
  await page.setViewportSize({ width: 1280, height: 800 });
  await loginToRooms(page);
  await expect(page.getByRole('button', { name: 'Thêm phòng' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Sửa phòng' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Xoá phòng' })).toHaveCount(0);
});

const VIEWPORTS = [
  { name: 'phone-390', width: 390, height: 844 },
  { name: 'tablet-820', width: 820, height: 1180 },
  { name: 'desktop-1280', width: 1280, height: 800 },
];

for (const vp of VIEWPORTS) {
  test(`rooms no horizontal overflow + no console errors @ ${vp.name}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (m) => {
      if (m.type() === 'error') {
        errors.push(`console.error: ${m.text()}`);
      }
    });

    await mockApi(page);
    await page.setViewportSize({ width: 1280, height: 800 });
    await loginToRooms(page); // tới /rooms ở desktop (SPA, giữ token)
    await page.setViewportSize({ width: vp.width, height: vp.height }); // đổi viewport KHÔNG reload
    await page.waitForTimeout(300);

    const metrics = await page.evaluate(() => {
      const el = document.scrollingElement ?? document.documentElement;
      return { scrollWidth: el.scrollWidth, clientWidth: el.clientWidth };
    });
    expect(
      metrics.scrollWidth,
      `rooms overflow @ ${vp.name}: ${metrics.scrollWidth} > ${metrics.clientWidth}`,
    ).toBeLessThanOrEqual(metrics.clientWidth + 1);
    expect(errors, `rooms console/page errors @ ${vp.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}

// Ảnh giao diện Rooms (desktop + phone) cho người dùng XEM.
test('screenshot rooms desktop + phone', async ({ page }) => {
  await mockApi(page);
  await page.setViewportSize({ width: 1280, height: 800 });
  await loginToRooms(page);
  await page.screenshot({ path: '../screenshots/admin-rooms-desktop.png', fullPage: true });

  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(300);
  await page.screenshot({ path: '../screenshots/admin-rooms-phone.png', fullPage: true });
});
