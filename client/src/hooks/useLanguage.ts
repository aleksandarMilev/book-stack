import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';

import type { SupportedLanguage } from '@/i18n';
import { SUPPORTED_LANGUAGES } from '@/i18n';

interface UseLanguageResult {
  language: SupportedLanguage;
  supportedLanguages: SupportedLanguage[];
  changeLanguage: (language: SupportedLanguage) => void;
}

export function useLanguage(): UseLanguageResult {
  const { i18n } = useTranslation();

  const language = SUPPORTED_LANGUAGES.includes(i18n.language as SupportedLanguage)
    ? (i18n.language as SupportedLanguage)
    : 'en';

  const changeLanguage = useCallback(
    (nextLanguage: SupportedLanguage) => {
      void i18n.changeLanguage(nextLanguage);
    },
    [i18n],
  );

  return { language, supportedLanguages: SUPPORTED_LANGUAGES, changeLanguage };
}
