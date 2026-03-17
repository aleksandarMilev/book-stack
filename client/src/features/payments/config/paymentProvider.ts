const TRUTHY_FLAG_VALUES = new Set(['1', 'true', 'yes', 'on']);
const FALSY_FLAG_VALUES = new Set(['0', 'false', 'no', 'off']);

export const normalizeProviderValue = (value: string | undefined): string | undefined => {
  if (!value) {
    return undefined;
  }

  const normalizedValue = value.trim();
  return normalizedValue.length > 0 ? normalizedValue : undefined;
};

export const parseBooleanFlag = (value: string | undefined): boolean | undefined => {
  if (!value) {
    return undefined;
  }

  const normalizedValue = value.trim().toLowerCase();
  if (TRUTHY_FLAG_VALUES.has(normalizedValue)) {
    return true;
  }

  if (FALSY_FLAG_VALUES.has(normalizedValue)) {
    return false;
  }

  return undefined;
};

const envPaymentProvider = normalizeProviderValue(import.meta.env.VITE_REACT_APP_PAYMENT_PROVIDER);
const explicitMockUiFlag = parseBooleanFlag(import.meta.env.VITE_REACT_APP_ENABLE_MOCK_PAYMENT_UI);

export const appPaymentProvider = envPaymentProvider;
export const isMockPaymentUiEnabled = explicitMockUiFlag ?? import.meta.env.DEV;
