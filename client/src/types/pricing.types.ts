export type CurrencyCode = 'BGN' | 'EUR';

export interface MoneyAmount {
  amount: number;
  currency: CurrencyCode;
}

export interface OfficialMoneyAmount {
  amount: number;
  currency: 'EUR';
}

export interface InformationalMoneyAmount {
  amount: number;
  currency: 'BGN';
}

export interface PriceDisplayValue {
  primary: OfficialMoneyAmount;
  secondary?: InformationalMoneyAmount;
}
