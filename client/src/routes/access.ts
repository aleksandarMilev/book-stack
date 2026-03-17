import { ROUTES } from '@/routes/paths';
import type { AuthCapabilities } from '@/types/auth.types';

export type RouteAccessLevel = 'public' | 'authenticated' | 'seller' | 'admin';

export const ROUTE_ACCESS_LEVELS = {
  [ROUTES.home]: 'public',
  [ROUTES.marketplace]: 'public',
  [ROUTES.listingDetails]: 'public',
  [ROUTES.books]: 'public',
  [ROUTES.checkout]: 'public',
  [ROUTES.orderConfirmation]: 'public',
  [ROUTES.mockPaymentCheckout]: 'public',
  [ROUTES.login]: 'public',
  [ROUTES.register]: 'public',
  [ROUTES.profile]: 'authenticated',
  [ROUTES.sellerProfile]: 'authenticated',
  [ROUTES.myOrders]: 'authenticated',
  [ROUTES.myListings]: 'seller',
  [ROUTES.myListingCreate]: 'seller',
  [ROUTES.myListingEdit]: 'seller',
  [ROUTES.sellerSoldOrders]: 'seller',
  [ROUTES.paymentReturn]: 'public',
  [ROUTES.adminDashboard]: 'admin',
  [ROUTES.adminBooksModeration]: 'admin',
  [ROUTES.adminListingsModeration]: 'admin',
} as const satisfies Record<string, RouteAccessLevel>;

export const canAccessLevel = (
  level: RouteAccessLevel,
  capabilities: AuthCapabilities,
): boolean => {
  if (level === 'public') {
    return true;
  }

  if (level === 'authenticated') {
    return capabilities.isAuthenticated;
  }

  if (level === 'seller') {
    return capabilities.isAuthenticated;
  }

  return capabilities.canAccessAdminArea;
};
