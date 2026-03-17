import type { Page } from '@playwright/test';
import { expect, test } from '@playwright/test';

export interface E2ECredentials {
  credentials: string;
  password: string;
}

const readEnvValue = (key: string): string | undefined => {
  const rawValue = process.env[key];
  if (!rawValue) {
    return undefined;
  }

  const normalizedValue = rawValue.trim();
  return normalizedValue.length > 0 ? normalizedValue : undefined;
};

export const getCredentialsFromEnv = (
  credentialsKey: string,
  passwordKey: string,
): E2ECredentials | undefined => {
  const credentials = readEnvValue(credentialsKey);
  const password = readEnvValue(passwordKey);

  if (!credentials || !password) {
    return undefined;
  }

  return { credentials, password };
};

export const skipIfMissingCredentials = (
  credentials: E2ECredentials | undefined,
  label: string,
): asserts credentials is E2ECredentials => {
  test.skip(!credentials, `Missing ${label} credentials environment variables.`);
};

export const loginAs = async (page: Page, credentials: E2ECredentials): Promise<void> => {
  await page.goto('/login');
  await page.getByLabel('Username or email').fill(credentials.credentials);
  await page.getByLabel('Password').fill(credentials.password);
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).not.toHaveURL(/\/login$/);
};
