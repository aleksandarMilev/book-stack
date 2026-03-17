import type { CurrencyCode, PriceDisplayValue } from '@/types/pricing.types';

const EUR_TO_BGN_RATE = 1.95583;

const roundMoney = (value: number): number => Number(value.toFixed(2));

export const toCurrencyCode = (currency: string): CurrencyCode => {
  const normalizedCurrency = currency.trim().toUpperCase();
  if (normalizedCurrency === 'EUR') {
    return 'EUR';
  }

  return 'BGN';
};

export const toPriceDisplayValue = (amount: number, currency: string): PriceDisplayValue => {
  const normalizedCurrency = toCurrencyCode(currency);
  if (normalizedCurrency === 'EUR') {
    return {
      primary: {
        amount,
        currency: 'EUR',
      },
      secondary: {
        amount: roundMoney(amount * EUR_TO_BGN_RATE),
        currency: 'BGN',
      },
    };
  }

  return {
    primary: {
      amount,
      currency: normalizedCurrency,
    },
  };
};
