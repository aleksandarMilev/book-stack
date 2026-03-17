import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';

import { LanguageSwitcher } from '@/components/i18n/LanguageSwitcher';
import { Button, Container } from '@/components/ui';
import { authService } from '@/features/auth/services/auth.service';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { useDisclosure } from '@/hooks/useDisclosure';
import { ROUTES } from '@/routes/paths';
import { useAuthCapabilities, useAuthStore } from '@/store/auth.store';
import { classNames } from '@/utils/classNames';

interface HeaderNavItem {
  labelKey: string;
  to: string;
}

const primaryNavItems: HeaderNavItem[] = [
  { labelKey: 'nav.primary.home', to: ROUTES.home },
  { labelKey: 'nav.primary.marketplace', to: ROUTES.marketplace },
  { labelKey: 'nav.primary.books', to: ROUTES.books },
];

const guestItems: HeaderNavItem[] = [
  { labelKey: 'nav.account.login', to: ROUTES.login },
  { labelKey: 'nav.account.register', to: ROUTES.register },
];

const userItems: HeaderNavItem[] = [
  { labelKey: 'nav.account.profile', to: ROUTES.profile },
  { labelKey: 'nav.account.sellerProfile', to: ROUTES.sellerProfile },
  { labelKey: 'nav.account.myOrders', to: ROUTES.myOrders },
];

const sellerItems: HeaderNavItem[] = [
  { labelKey: 'nav.seller.myListings', to: ROUTES.myListings },
  { labelKey: 'nav.seller.soldOrders', to: ROUTES.sellerSoldOrders },
];

const adminItems: HeaderNavItem[] = [
  { labelKey: 'nav.admin.dashboard', to: ROUTES.adminDashboard },
  { labelKey: 'nav.admin.books', to: ROUTES.adminBooksModeration },
  { labelKey: 'nav.admin.listings', to: ROUTES.adminListingsModeration },
];

function HeaderLink({ labelKey, to, onClick }: HeaderNavItem & { onClick?: () => void }) {
  const { t } = useTranslation();

  return (
    <NavLink
      className={({ isActive }) => classNames('header-link', isActive && 'header-link--active')}
      onClick={onClick}
      to={to}
    >
      {t(labelKey)}
    </NavLink>
  );
}

interface HeaderGroupProps {
  title: string;
  items: HeaderNavItem[];
  onItemClick?: () => void;
}

function HeaderGroup({ title, items, onItemClick }: HeaderGroupProps) {
  return (
    <div className="header-group">
      <p className="header-group-title">{title}</p>
      <div className="header-group-links">
        {items.map((item) => (
          <HeaderLink
            key={item.to}
            labelKey={item.labelKey}
            to={item.to}
            {...(onItemClick ? { onClick: onItemClick } : {})}
          />
        ))}
      </div>
    </div>
  );
}

export function AppHeader() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const session = useAuthStore((state) => state.session);
  const capabilities = useAuthCapabilities();
  const { close, isOpen, toggle } = useDisclosure();
  const location = useLocation();
  const sellerProfile = useSellerProfileStore((state) => state.profile);
  const isAuthenticated = capabilities.isAuthenticated && Boolean(session);
  const mobileDrawerId = 'app-mobile-navigation';
  const hasActiveSellerProfile = Boolean(sellerProfile?.isActive);

  useEffect(() => {
    close();
  }, [close, location.pathname]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const handleEscape = (event: KeyboardEvent): void => {
      if (event.key === 'Escape') {
        close();
      }
    };

    window.addEventListener('keydown', handleEscape);
    return () => {
      window.removeEventListener('keydown', handleEscape);
    };
  }, [close, isOpen]);

  const desktopQuickItems = isAuthenticated ? userItems : guestItems;
  const mobileAccountItems = isAuthenticated ? userItems : guestItems;

  const handleLogout = (): void => {
    authService.logout();
    navigate(ROUTES.home, { replace: true });
    close();
  };

  return (
    <header className="site-header">
      <Container className="site-header-main">
        <NavLink className="brand" to={ROUTES.home}>
          <span className="brand-mark" />
          <div>
            <p className="brand-name">{t('common.appName')}</p>
            <p className="brand-subtitle">{t('common.labels.trustedMarketplace')}</p>
          </div>
        </NavLink>

        <nav aria-label={t('common.appName')} className="desktop-primary-nav">
          {primaryNavItems.map((item) => (
            <HeaderLink key={item.to} labelKey={item.labelKey} to={item.to} />
          ))}
        </nav>

        <div className="desktop-header-right">
          <nav className="desktop-account-nav">
            {desktopQuickItems.map((item) => (
              <HeaderLink key={item.to} labelKey={item.labelKey} to={item.to} />
            ))}
            {isAuthenticated ? (
              <Button onClick={handleLogout} size="sm" variant="ghost">
                {t('common.actions.logout')}
              </Button>
            ) : null}
          </nav>
          <LanguageSwitcher compact />
          <Button
            aria-controls={mobileDrawerId}
            aria-expanded={isOpen}
            aria-label={isOpen ? t('nav.mobile.closeMenu') : t('nav.mobile.openMenu')}
            className="mobile-menu-button"
            onClick={toggle}
            variant="ghost"
          >
            {isOpen ? t('nav.mobile.closeMenu') : t('nav.mobile.openMenu')}
          </Button>
        </div>
      </Container>

      {isAuthenticated ? (
        <Container className="site-header-meta">
          <div className="desktop-group-row">
            <HeaderGroup items={userItems} title={t('shell.accountArea')} />
            {hasActiveSellerProfile ? (
              <HeaderGroup items={sellerItems} title={t('shell.sellerArea')} />
            ) : null}
            {capabilities.canAccessAdminArea ? (
              <HeaderGroup items={adminItems} title={t('shell.adminArea')} />
            ) : null}
          </div>
        </Container>
      ) : null}

      <div
        className={classNames('mobile-nav-overlay', isOpen && 'mobile-nav-overlay--open')}
        onClick={close}
      />
      <aside
        aria-label={t('nav.mobile.navigationLabel')}
        aria-modal="true"
        aria-hidden={!isOpen}
        className={classNames('mobile-nav-drawer', isOpen && 'mobile-nav-drawer--open')}
        id={mobileDrawerId}
        role="dialog"
      >
        <div className="mobile-nav-head">
          <p className="mobile-nav-title">{t('common.appName')}</p>
          <Button onClick={close} size="sm" variant="ghost">
            {t('common.actions.close')}
          </Button>
        </div>

        <div className="mobile-nav-sections">
          <HeaderGroup
            items={primaryNavItems}
            onItemClick={close}
            title={t('common.labels.premiumSelection')}
          />
          <HeaderGroup
            items={mobileAccountItems}
            onItemClick={close}
            title={t('shell.accountArea')}
          />
          {hasActiveSellerProfile ? (
            <HeaderGroup items={sellerItems} onItemClick={close} title={t('shell.sellerArea')} />
          ) : null}
          {capabilities.canAccessAdminArea ? (
            <HeaderGroup items={adminItems} onItemClick={close} title={t('shell.adminArea')} />
          ) : null}
          {isAuthenticated ? (
            <Button fullWidth onClick={handleLogout} variant="secondary">
              {t('common.actions.logout')}
            </Button>
          ) : null}
          <LanguageSwitcher />
        </div>
      </aside>
    </header>
  );
}
