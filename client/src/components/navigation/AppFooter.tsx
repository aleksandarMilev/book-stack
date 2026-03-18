import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';

import { Container } from '@/components/ui';
import { ROUTES } from '@/routes/paths';

export function AppFooter() {
  const { t } = useTranslation();

  const currentYear = useMemo(() => new Date().getFullYear(), []);

  return (
    <footer className="site-footer">
      <Container className="site-footer-inner">
        <div className="site-footer-brand">
          <p className="site-footer-logo">{t('common.appName')}</p>
          <p className="site-footer-tagline">{t('shell.footerTagline')}</p>
        </div>

        <nav className="site-footer-nav">
          <NavLink to={ROUTES.home}>{t('nav.primary.home')}</NavLink>
          <NavLink to={ROUTES.marketplace}>{t('nav.primary.marketplace')}</NavLink>
        </nav>
      </Container>
      <Container>
        <p className="site-footer-copy">
          {currentYear} {t('shell.footerCopyright')}
        </p>
      </Container>
    </footer>
  );
}
