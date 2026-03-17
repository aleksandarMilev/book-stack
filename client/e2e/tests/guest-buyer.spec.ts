import { expect, test } from '@playwright/test';

test('guest buyer can go from marketplace to checkout and payment return', async ({ page }) => {
  await page.goto('/marketplace');
  await expect(page.getByRole('heading', { name: 'Marketplace' })).toBeVisible();

  const viewListingButtons = page.getByRole('button', { name: 'View listing' });
  const listingCount = await viewListingButtons.count();
  test.skip(listingCount === 0, 'No marketplace listings are available for guest checkout flow.');

  await viewListingButtons.first().click();
  await expect(page.getByRole('heading', { name: 'Buy this listing' })).toBeVisible();

  await page.getByRole('button', { name: 'Buy now' }).click();
  await expect(page).toHaveURL(/\/checkout/);

  await page.getByLabel('First name').fill('Guest');
  await page.getByLabel('Last name').fill('Buyer');
  await page.getByLabel('Email').fill('guest.buyer@example.com');
  await page.getByLabel('Country').fill('Bulgaria');
  await page.getByLabel('City').fill('Sofia');
  await page.getByLabel('Address line').fill('1 Vitosha Blvd');

  await page.getByRole('button', { name: 'Create order and continue' }).click();
  await expect(page).toHaveURL(/\/payments\/mock\/checkout/);

  await page.getByRole('button', { name: 'Simulate success' }).click();
  await expect(page).toHaveURL(/\/payment\/return/);
  await expect(page.getByRole('heading', { name: 'Payment successful' })).toBeVisible();
});
