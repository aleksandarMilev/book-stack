import { describe, expect, it } from 'vitest';

import { toPriceDisplayValue } from '@/utils/priceDisplay';

describe('toPriceDisplayValue', () => {
  it('keeps EUR as primary and BGN as secondary for official EUR amounts', () => {
    const result = toPriceDisplayValue(10, 'EUR');

    expect(result.primary).toEqual({
      amount: 10,
      currency: 'EUR',
    });
    expect(result.secondary).toEqual({
      amount: 19.56,
      currency: 'BGN',
    });
  });

  it('converts BGN input to EUR primary while keeping BGN as secondary informational value', () => {
    const result = toPriceDisplayValue(19.56, 'BGN');

    expect(result.primary).toEqual({
      amount: 10,
      currency: 'EUR',
    });
    expect(result.secondary).toEqual({
      amount: 19.56,
      currency: 'BGN',
    });
  });

  it('defaults unknown currencies to EUR official display', () => {
    const result = toPriceDisplayValue(12.5, 'EU');

    expect(result.primary.currency).toBe('EUR');
    expect(result.secondary?.currency).toBe('BGN');
  });
});
