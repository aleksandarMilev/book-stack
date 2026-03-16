import { Outlet } from 'react-router-dom';

import type { RouteAccessLevel } from '@/routes/access';
import { RouteAccessGuard } from '@/routes/guards/RouteAccessGuard';

interface GuardedRouteOutletProps {
  level: RouteAccessLevel;
}

export function GuardedRouteOutlet({ level }: GuardedRouteOutletProps) {
  return (
    <RouteAccessGuard level={level}>
      <Outlet />
    </RouteAccessGuard>
  );
}
