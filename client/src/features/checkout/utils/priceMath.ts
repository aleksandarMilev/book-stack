import type { PriceDisplayValue } from '@/types/pricing.types';

export const multiplyPriceDisplayValue = (price: PriceDisplayValue, quantity: number): PriceDisplayValue => ({
  primary: {
    amount: Number((price.primary.amount * quantity).toFixed(2)),
    currency: price.primary.currency,
  },
  ...(price.secondary
    ? {
        secondary: {
          amount: Number((price.secondary.amount * quantity).toFixed(2)),
          currency: price.secondary.currency,
        },
      }
    : {}),
});
