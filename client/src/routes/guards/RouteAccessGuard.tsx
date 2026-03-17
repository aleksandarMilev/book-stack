import type { PropsWithChildren } from 'react';
import { useTranslation } from 'react-i18next';
import { Navigate, useLocation } from 'react-router-dom';

import { LoadingState } from '@/components/ui';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import type { RouteAccessLevel } from '@/routes/access';
import { canAccessLevel, hasActiveSellerCapability, isSellerCapabilityResolving } from '@/routes/access';
import { ROUTES } from '@/routes/paths';
import { useAuthCapabilities, useAuthStore } from '@/store/auth.store';

interface RouteAccessGuardProps extends PropsWithChildren {
  level: RouteAccessLevel;
}

const getRedirectPath = (
  level: RouteAccessLevel,
  isAuthenticated: boolean,
): string => {
  if (level === 'authenticated') {
    return ROUTES.login;
  }

  if (level === 'seller') {
    return isAuthenticated ? ROUTES.sellerProfile : ROUTES.login;
  }

  if (!isAuthenticated) {
    return ROUTES.login;
  }

  return ROUTES.home;
};

export function RouteAccessGuard({ level, children }: RouteAccessGuardProps) {
  const { t } = useTranslation();
  const capabilities = useAuthCapabilities();
  const session = useAuthStore((state) => state.session);
  const sellerProfile = useSellerProfileStore((state) => state.profile);
  const sellerProfileLoadState = useSellerProfileStore((state) => state.loadState);
  const sellerProfileLoadedForUserId = useSellerProfileStore((state) => state.loadedForUserId);
  const location = useLocation();
  const isAuthenticated = capabilities.isAuthenticated;

  if (level === 'seller') {
    const sellerCapabilityContext = {
      isAuthenticated,
      currentUserId: session?.user.id ?? null,
      sellerProfileIsActive: Boolean(sellerProfile?.isActive),
      sellerProfileLoadState,
      sellerProfileLoadedForUserId,
    };

    if (hasActiveSellerCapability(sellerCapabilityContext)) {
      return <>{children}</>;
    }

    if (isSellerCapabilityResolving(sellerCapabilityContext)) {
      return (
        <LoadingState
          description={t('pages.routeAccess.sellerLoadingDescription')}
          title={t('pages.routeAccess.sellerLoadingTitle')}
        />
      );
    }
  } else if (canAccessLevel(level, capabilities)) {
    return <>{children}</>;
  }

  const isRedirectingToLogin = !isAuthenticated && level !== 'public';
  const isRedirectingToSellerProfile = level === 'seller' && isAuthenticated;

  return (
    <Navigate
      replace
      state={{
        from: `${location.pathname}${location.search}${location.hash}`,
        ...(isRedirectingToLogin
          ? { reason: session ? 'sessionExpired' : 'authRequired' }
          : {}),
        ...(isRedirectingToSellerProfile ? { reason: 'sellerProfileRequired' } : {}),
      }}
      to={getRedirectPath(level, isAuthenticated)}
    />
  );
}
