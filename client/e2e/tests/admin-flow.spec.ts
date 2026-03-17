import { expect, test } from '@playwright/test';

import { getCredentialsFromEnv, loginAs, skipIfMissingCredentials } from '../helpers/auth';

const adminCredentials = getCredentialsFromEnv('E2E_ADMIN_CREDENTIALS', 'E2E_ADMIN_PASSWORD');

test('admin can access dashboard and moderation areas', async ({ page }) => {
  skipIfMissingCredentials(adminCredentials, 'admin');
  await loginAs(page, adminCredentials);

  await page.goto('/admin');
  await expect(page.getByRole('heading', { name: 'Admin dashboard' })).toBeVisible();

  await page.goto('/admin/books');
  await expect(page.getByRole('heading', { name: 'Books moderation' })).toBeVisible();

  const approveButtons = page.getByRole('button', { name: 'Approve' });
  if ((await approveButtons.count()) > 0) {
    await approveButtons.first().click();
    await expect(page.getByRole('heading', { name: 'Books moderation' })).toBeVisible();
  }

  await page.goto('/admin/listings');
  await expect(page.getByRole('heading', { name: 'Listings moderation' })).toBeVisible();
});
