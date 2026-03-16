import { useMemo } from 'react';
import { create } from 'zustand';

import { authStorage } from '@/api/authStorage';
import { isExpirationIsoExpired, mapJwtToSession } from '@/features/auth/utils/jwtSession';
import type { AuthCapabilities, AuthSession, UserRole } from '@/types/auth.types';

interface AuthState {
  session: AuthSession | null;
  setSession: (session: AuthSession) => void;
  setSessionFromToken: (token: string) => boolean;
  clearSession: () => void;
}

const hasRoleAccess = (role: UserRole | undefined, requiredRole: UserRole): boolean => {
  if (!role) {
    return false;
  }

  if (requiredRole === 'buyer') {
    return true;
  }

  if (requiredRole === 'seller') {
    return role === 'seller' || role === 'admin';
  }

  return role === 'admin';
};

export const deriveAuthCapabilities = (session: AuthSession | null): AuthCapabilities => {
  const isAuthenticated = Boolean(session?.accessToken) && !isExpirationIsoExpired(session?.expiresAtUtc);
  const role = session?.user.role;

  return {
    isAuthenticated,
    canAccessSellerArea: isAuthenticated && hasRoleAccess(role, 'seller'),
    canAccessAdminArea: isAuthenticated && hasRoleAccess(role, 'admin'),
  };
};

export const useAuthStore = create<AuthState>((set) => ({
  session: authStorage.getSession(),
  setSession: (session) => {
    if (isExpirationIsoExpired(session.expiresAtUtc)) {
      authStorage.clearSession();
      set({ session: null });
      return;
    }

    authStorage.setSession(session);
    set({ session });
  },
  setSessionFromToken: (token) => {
    const session = mapJwtToSession(token);
    if (!session) {
      authStorage.clearSession();
      set({ session: null });
      return false;
    }

    authStorage.setSession(session);
    set({ session });
    return true;
  },
  clearSession: () => {
    authStorage.clearSession();
    set({ session: null });
  },
}));

export function useAuthCapabilities(): AuthCapabilities {
  const session = useAuthStore((state) => state.session);

  return useMemo(() => deriveAuthCapabilities(session), [session]);
}
