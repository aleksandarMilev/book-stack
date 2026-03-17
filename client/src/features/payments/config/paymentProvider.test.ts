import { describe, expect, it } from 'vitest';

import { normalizeProviderValue, parseBooleanFlag } from '@/features/payments/config/paymentProvider';

describe('paymentProvider config helpers', () => {
  it('normalizes provider strings safely', () => {
    expect(normalizeProviderValue(undefined)).toBeUndefined();
    expect(normalizeProviderValue('')).toBeUndefined();
    expect(normalizeProviderValue('   ')).toBeUndefined();
    expect(normalizeProviderValue(' stripe ')).toBe('stripe');
  });

  it('parses boolean flag values from env strings', () => {
    expect(parseBooleanFlag(undefined)).toBeUndefined();
    expect(parseBooleanFlag('true')).toBe(true);
    expect(parseBooleanFlag('1')).toBe(true);
    expect(parseBooleanFlag('yes')).toBe(true);
    expect(parseBooleanFlag('false')).toBe(false);
    expect(parseBooleanFlag('0')).toBe(false);
    expect(parseBooleanFlag('no')).toBe(false);
    expect(parseBooleanFlag('maybe')).toBeUndefined();
  });
});
