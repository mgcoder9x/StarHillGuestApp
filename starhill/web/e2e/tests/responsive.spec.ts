import { test, expect } from '@playwright/test';

// Ma trận viewport phủ "vô số điện thoại" thực tế: Android nhỏ nhất → foldable gập → phone lớn → tablet → desktop admin.
// Mỗi viewport kiểm bất biến responsive #1: KHÔNG scroll ngang + KHÔNG lỗi console/page (bắt lỗi runtime).
const VIEWPORTS = [
  { name: 'galaxy-fold-closed', width: 280, height: 653 },
  { name: 'android-small', width: 320, height: 568 },
  { name: 'iphone-se', width: 375, height: 667 },
  { name: 'pixel', width: 393, height: 851 },
  { name: 'iphone-pro-max', width: 430, height: 932 },
  { name: 'tablet-portrait', width: 768, height: 1024 },
  { name: 'desktop', width: 1280, height: 800 },
];

for (const vp of VIEWPORTS) {
  test(`no horizontal overflow + no console errors @ ${vp.name} (${vp.width}x${vp.height})`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (e) => errors.push(`pageerror: ${e.message}`));
    page.on('console', (m) => {
      if (m.type() === 'error') {
        errors.push(`console.error: ${m.text()}`);
      }
    });

    await page.setViewportSize({ width: vp.width, height: vp.height });
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const metrics = await page.evaluate(() => {
      const el = document.scrollingElement ?? document.documentElement;
      return { scrollWidth: el.scrollWidth, clientWidth: el.clientWidth };
    });

    // Bug responsive #1: nội dung KHÔNG được tràn ngang (cho phép sai số 1px do làm tròn subpixel).
    expect(
      metrics.scrollWidth,
      `horizontal overflow at ${vp.name}: scrollWidth=${metrics.scrollWidth} > clientWidth=${metrics.clientWidth}`,
    ).toBeLessThanOrEqual(metrics.clientWidth + 1);

    expect(errors, `console/page errors at ${vp.name}:\n${errors.join('\n')}`).toEqual([]);
  });
}
