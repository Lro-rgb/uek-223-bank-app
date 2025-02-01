import { test, expect } from '@playwright/test';
import credentials from './credentials';

test('has title', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
});

test('can login', async ({ page }) => {
  await page.goto('/');

  await page.fill('[name="username"]', credentials.admin.username);
  await page.fill('[name="password"]', credentials.admin.password);
  await page.click('[type="submit"]');

  await expect(page).toHaveURL('/ledgers');
});
