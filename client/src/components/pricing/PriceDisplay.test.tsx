import { render, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import i18n from '@/i18n';
import { toPriceDisplayValue } from '@/utils/priceDisplay';

const EURO_PATTERN = /\u20ac|EUR/;
const BGN_PATTERN = /BGN|\u043b\u0432/;

describe('PriceDisplay currency rendering', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en');
  });

  afterEach(async () => {
    await i18n.changeLanguage('en');
  });

  it('keeps official EUR as primary in both EN and BG locales', async () => {
    const value = toPriceDisplayValue(10, 'EUR');
    const { container, rerender } = render(<PriceDisplay value={value} />);

    const primaryInEn = container.querySelector('.price-display-primary');
    const secondaryInEn = container.querySelector('.price-display-secondary');

    expect(EURO_PATTERN.test(primaryInEn?.textContent ?? '')).toBe(true);
    expect(BGN_PATTERN.test(primaryInEn?.textContent ?? '')).toBe(false);
    expect(secondaryInEn).not.toBeNull();

    await i18n.changeLanguage('bg');
    rerender(<PriceDisplay value={value} />);

    await waitFor(() => {
      const primaryInBg = container.querySelector('.price-display-primary');
      const secondaryInBg = container.querySelector('.price-display-secondary');

      expect(EURO_PATTERN.test(primaryInBg?.textContent ?? '')).toBe(true);
      expect(BGN_PATTERN.test(primaryInBg?.textContent ?? '')).toBe(false);
      expect(BGN_PATTERN.test(secondaryInBg?.textContent ?? '')).toBe(true);
    });
  });
});
