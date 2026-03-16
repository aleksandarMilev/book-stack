import type { CurrencyCode } from '@/types/pricing.types';

interface FormatMoneyInput {
  amount: number;
  currency: CurrencyCode;
  language?: string;
  locale?: string;
}

interface FormatDateTimeInput {
  value: string;
  language?: string;
  locale?: string;
  options?: Intl.DateTimeFormatOptions;
}

const DEFAULT_LOCALE = 'en-GB';

const LANGUAGE_TO_LOCALE: Record<string, string> = {
  bg: 'bg-BG',
  en: 'en-GB',
};

export function resolveLocale(language?: string, locale?: string): string {
  if (locale) {
    return locale;
  }

  const normalizedLanguage = language?.slice(0, 2).toLowerCase();
  if (normalizedLanguage && LANGUAGE_TO_LOCALE[normalizedLanguage]) {
    return LANGUAGE_TO_LOCALE[normalizedLanguage];
  }

  return DEFAULT_LOCALE;
}

export function formatMoney({ amount, currency, language, locale }: FormatMoneyInput): string {
  return new Intl.NumberFormat(resolveLocale(language, locale), {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(amount);
}

const DEFAULT_DATE_TIME_OPTIONS: Intl.DateTimeFormatOptions = {
  dateStyle: 'medium',
  timeStyle: 'short',
};

export function formatDateTime({ value, language, locale, options }: FormatDateTimeInput): string {
  const dateValue = new Date(value);
  if (Number.isNaN(dateValue.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat(resolveLocale(language, locale), options ?? DEFAULT_DATE_TIME_OPTIONS).format(
    dateValue,
  );
}
