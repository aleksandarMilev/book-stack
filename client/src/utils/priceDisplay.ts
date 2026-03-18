import type { CurrencyCode, PriceDisplayValue } from '@/types/pricing.types';

const EUR_TO_BGN_RATE = 1.95583;
const OFFICIAL_CURRENCY: CurrencyCode = 'EUR';
const INFORMATIONAL_CURRENCY: CurrencyCode = 'BGN';

const roundMoney = (value: number): number => Number(value.toFixed(2));

export const toCurrencyCode = (currency: string): CurrencyCode => {
  const normalizedCurrency = currency.trim().toUpperCase();
  if (normalizedCurrency === INFORMATIONAL_CURRENCY) {
    return INFORMATIONAL_CURRENCY;
  }

  return OFFICIAL_CURRENCY;
};

export const toPriceDisplayValue = (amount: number, currency: string): PriceDisplayValue => {
  const normalizedCurrency = toCurrencyCode(currency);
  const officialAmount = normalizedCurrency === INFORMATIONAL_CURRENCY
    ? roundMoney(amount / EUR_TO_BGN_RATE)
    : amount;
  const informationalAmount = normalizedCurrency === INFORMATIONAL_CURRENCY
    ? amount
    : roundMoney(amount * EUR_TO_BGN_RATE);

  return {
    primary: {
      amount: officialAmount,
      currency: OFFICIAL_CURRENCY,
    },
    secondary: {
      amount: informationalAmount,
      currency: INFORMATIONAL_CURRENCY,
    },
  };
};
