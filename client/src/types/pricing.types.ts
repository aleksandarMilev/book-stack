export type CurrencyCode = 'BGN' | 'EUR';

export interface MoneyAmount {
  amount: number;
  currency: CurrencyCode;
}

export interface PriceDisplayValue {
  primary: MoneyAmount;
  secondary?: MoneyAmount;
}
