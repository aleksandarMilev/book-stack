import { useTranslation } from 'react-i18next';

import { Button } from '@/components/ui/Button';
import { useLanguage } from '@/hooks/useLanguage';
import { classNames } from '@/utils/classNames';

interface LanguageSwitcherProps {
  compact?: boolean;
}

export function LanguageSwitcher({ compact = false }: LanguageSwitcherProps) {
  const { t } = useTranslation();
  const { changeLanguage, language, supportedLanguages } = useLanguage();

  return (
    <div className={classNames('language-switcher', compact && 'language-switcher--compact')}>
      <span className="language-switcher-label">{t('common.language')}</span>
      <div className="language-switcher-actions">
        {supportedLanguages.map((supportedLanguage) => (
          <Button
            aria-pressed={language === supportedLanguage}
            className={classNames(
              'language-switcher-button',
              language === supportedLanguage && 'language-switcher-button--active',
            )}
            key={supportedLanguage}
            onClick={() => {
              changeLanguage(supportedLanguage);
            }}
            size="sm"
            variant="ghost"
          >
            {supportedLanguage.toUpperCase()}
          </Button>
        ))}
      </div>
    </div>
  );
}
