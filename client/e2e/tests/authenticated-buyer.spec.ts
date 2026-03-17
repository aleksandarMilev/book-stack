import { expect, test } from '@playwright/test';

import { getCredentialsFromEnv, loginAs, skipIfMissingCredentials } from '../helpers/auth';

const buyerCredentials = getCredentialsFromEnv('E2E_BUYER_CREDENTIALS', 'E2E_BUYER_PASSWORD');

test('authenticated buyer sees account areas and can logout safely', async ({ page }) => {
  skipIfMissingCredentials(buyerCredentials, 'buyer');
  await loginAs(page, buyerCredentials);

  await page.goto('/profile');
  await expect(page.getByRole('heading', { name: 'Profile' })).toBeVisible();

  await page.goto('/my-orders');
  await expect(page.getByRole('heading', { name: 'My orders' })).toBeVisible();

  await page.getByRole('button', { name: 'Logout' }).first().click();
  await expect(page.getByText('Login').first()).toBeVisible();

  await page.goto('/profile');
  await expect(page).toHaveURL(/\/login/);
  await expect(page.getByText('Please sign in to continue.')).toBeVisible();
});
