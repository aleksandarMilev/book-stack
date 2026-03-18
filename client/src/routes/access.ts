import { ROUTES } from '@/routes/paths';
import type { AuthCapabilities } from '@/types/auth.types';

export type RouteAccessLevel = 'public' | 'authenticated' | 'seller' | 'admin';
export type SellerProfileLoadState = 'idle' | 'loading' | 'ready' | 'error';

export interface SellerCapabilityContext {
  isAuthenticated: boolean;
  currentUserId: string | null;
  sellerProfileIsActive: boolean;
  sellerProfileLoadState: SellerProfileLoadState;
  sellerProfileLoadedForUserId: string | null;
}

export const ROUTE_ACCESS_LEVELS = {
  [ROUTES.home]: 'public',
  [ROUTES.marketplace]: 'public',
  [ROUTES.listingDetails]: 'public',
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

export const isSellerCapabilityResolving = (context: SellerCapabilityContext): boolean => {
  if (!context.isAuthenticated || !context.currentUserId) {
    return false;
  }

  if (context.sellerProfileLoadedForUserId !== context.currentUserId) {
    return true;
  }

  return context.sellerProfileLoadState === 'idle' || context.sellerProfileLoadState === 'loading';
};

export const hasActiveSellerCapability = (context: SellerCapabilityContext): boolean => {
  if (!context.isAuthenticated || !context.currentUserId) {
    return false;
  }

  if (context.sellerProfileLoadedForUserId !== context.currentUserId) {
    return false;
  }

  if (context.sellerProfileLoadState !== 'ready') {
    return false;
  }

  return context.sellerProfileIsActive;
};
