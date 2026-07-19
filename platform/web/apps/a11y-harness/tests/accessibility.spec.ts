import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

test('page and dialog have no serious accessibility violations', async ({ page }) => {
  await page.goto('index.html');
  const pageResults = await new AxeBuilder({ page }).analyze();
  expect(pageResults.violations.filter((item) => ['serious', 'critical'].includes(item.impact ?? ''))).toEqual([]);

  const trigger = page.getByRole('button', { name: 'Review operation' });
  await trigger.focus();
  await trigger.click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Confirm' })).toBeFocused();
  const dialogResults = await new AxeBuilder({ page }).include('dialog').analyze();
  expect(dialogResults.violations.filter((item) => ['serious', 'critical'].includes(item.impact ?? ''))).toEqual([]);

  await page.getByRole('button', { name: 'Cancel' }).click();
  await expect(trigger).toBeFocused();
});
