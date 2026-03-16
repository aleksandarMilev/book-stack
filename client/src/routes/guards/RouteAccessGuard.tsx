import type { PropsWithChildren } from 'react';
import { Navigate, useLocation } from 'react-router-dom';

import type { RouteAccessLevel } from '@/routes/access';
import { canAccessLevel } from '@/routes/access';
import { ROUTES } from '@/routes/paths';
import { useAuthCapabilities } from '@/store/auth.store';

interface RouteAccessGuardProps extends PropsWithChildren {
  level: RouteAccessLevel;
}

const getRedirectPath = (level: RouteAccessLevel): string => {
  if (level === 'authenticated') {
    return ROUTES.login;
  }

  return ROUTES.home;
};

export function RouteAccessGuard({ level, children }: RouteAccessGuardProps) {
  const capabilities = useAuthCapabilities();
  const location = useLocation();

  if (canAccessLevel(level, capabilities)) {
    return <>{children}</>;
  }

  return <Navigate replace state={{ from: location.pathname }} to={getRedirectPath(level)} />;
}
