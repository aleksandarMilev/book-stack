import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import bg from '@/i18n/resources/bg';
import en from '@/i18n/resources/en';

export type SupportedLanguage = 'en' | 'bg';

const LANGUAGE_STORAGE_KEY = 'bookstack.language';

const resources = {
  en: { translation: en },
  bg: { translation: bg },
} as const;

const isSupportedLanguage = (value: string | null): value is SupportedLanguage =>
  value === 'en' || value === 'bg';

const getInitialLanguage = (): SupportedLanguage => {
  if (typeof window === 'undefined') {
    return 'en';
  }

  const storedLanguage = window.localStorage.getItem(LANGUAGE_STORAGE_KEY);
  if (isSupportedLanguage(storedLanguage)) {
    return storedLanguage;
  }

  const browserLanguage = window.navigator.language.slice(0, 2);
  if (isSupportedLanguage(browserLanguage)) {
    return browserLanguage;
  }

  return 'en';
};

void i18n.use(initReactI18next).init({
  resources,
  lng: getInitialLanguage(),
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
});

if (typeof window !== 'undefined') {
  i18n.on('languageChanged', (language) => {
    if (isSupportedLanguage(language)) {
      window.localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
    }
  });
}

export const SUPPORTED_LANGUAGES: SupportedLanguage[] = ['en', 'bg'];

export default i18n;
