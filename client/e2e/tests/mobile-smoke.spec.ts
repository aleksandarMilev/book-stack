import { expect, test } from '@playwright/test';

test('@mobile mobile navigation and marketplace filters are usable', async ({ page }) => {
  await page.goto('/');

  await page.getByRole('button', { name: 'Open menu', exact: true }).click();
  const mobileNavigationDialog = page.getByRole('dialog', { name: 'Mobile navigation' });
  await expect(mobileNavigationDialog).toBeVisible();
  await mobileNavigationDialog.getByRole('button', { name: 'Close', exact: true }).click();
  await expect(mobileNavigationDialog).toBeHidden();

  await page.goto('/marketplace');
  await page.getByRole('button', { name: 'Open filters', exact: true }).click();
  const filtersDialog = page.getByRole('dialog', { name: 'Filters' });
  await expect(filtersDialog).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(filtersDialog).toBeHidden();
});
