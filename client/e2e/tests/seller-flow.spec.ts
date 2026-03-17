import { expect, test } from '@playwright/test';

import { getCredentialsFromEnv, loginAs, skipIfMissingCredentials } from '../helpers/auth';

const sellerCredentials = getCredentialsFromEnv('E2E_SELLER_CREDENTIALS', 'E2E_SELLER_PASSWORD');

test('seller can access seller areas and protected navigation', async ({ page }) => {
  skipIfMissingCredentials(sellerCredentials, 'seller');
  await loginAs(page, sellerCredentials);

  await page.goto('/my-listings');
  await expect(page.getByRole('heading', { name: 'My listings' })).toBeVisible();

  await page.goto('/seller/sold-orders');
  await expect(page.getByRole('heading', { name: 'Seller sold orders' })).toBeVisible();

  await expect(page.getByText('Dashboard')).toHaveCount(0);

  await page.goto('/admin');
  await expect(page).toHaveURL('/');
});
