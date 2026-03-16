import { useMemo } from 'react';
import { create } from 'zustand';

import { authStorage } from '@/api/authStorage';
import type { AuthCapabilities, AuthSession, UserRole } from '@/types/auth.types';

interface AuthState {
  session: AuthSession | null;
  setSession: (session: AuthSession) => void;
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
  const role = session?.user.role;

  return {
    isAuthenticated: Boolean(session?.accessToken),
    canAccessSellerArea: hasRoleAccess(role, 'seller'),
    canAccessAdminArea: hasRoleAccess(role, 'admin'),
  };
};

export const useAuthStore = create<AuthState>((set) => ({
  session: authStorage.getSession(),
  setSession: (session) => {
    authStorage.setSession(session);
    set({ session });
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
