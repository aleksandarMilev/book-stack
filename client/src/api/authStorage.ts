import type { AuthSession, UserRole } from '@/types/auth.types';

const AUTH_STORAGE_KEY = 'bookstack.auth.session';

const isBrowser = (): boolean => typeof window !== 'undefined';

const isUserRole = (value: unknown): value is UserRole =>
  value === 'buyer' || value === 'seller' || value === 'admin';

const normalizeSession = (value: unknown): AuthSession | null => {
  if (!value || typeof value !== 'object') {
    return null;
  }

  const candidate = value as Record<string, unknown>;
  if (typeof candidate.accessToken !== 'string' || candidate.accessToken.length === 0) {
    return null;
  }

  const userCandidate =
    candidate.user && typeof candidate.user === 'object'
      ? (candidate.user as Record<string, unknown>)
      : null;

  if (userCandidate && isUserRole(userCandidate.role) && typeof userCandidate.id === 'string') {
    return {
      accessToken: candidate.accessToken,
      ...(typeof candidate.refreshToken === 'string' ? { refreshToken: candidate.refreshToken } : {}),
      ...(typeof candidate.expiresAtUtc === 'string' ? { expiresAtUtc: candidate.expiresAtUtc } : {}),
      user: {
        id: userCandidate.id,
        role: userCandidate.role,
        ...(typeof userCandidate.displayName === 'string' ? { displayName: userCandidate.displayName } : {}),
        ...(typeof userCandidate.email === 'string' ? { email: userCandidate.email } : {}),
      },
    };
  }

  if (isUserRole(candidate.role)) {
    return {
      accessToken: candidate.accessToken,
      ...(typeof candidate.refreshToken === 'string' ? { refreshToken: candidate.refreshToken } : {}),
      user: {
        id: 'legacy-user',
        role: candidate.role,
      },
    };
  }

  return null;
};

export const authStorage = {
  getSession(): AuthSession | null {
    if (!isBrowser()) {
      return null;
    }

    const rawValue = window.localStorage.getItem(AUTH_STORAGE_KEY);
    if (!rawValue) {
      return null;
    }

    try {
      return normalizeSession(JSON.parse(rawValue));
    } catch {
      window.localStorage.removeItem(AUTH_STORAGE_KEY);
      return null;
    }
  },

  setSession(session: AuthSession): void {
    if (!isBrowser()) {
      return;
    }

    window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session));
  },

  clearSession(): void {
    if (!isBrowser()) {
      return;
    }

    window.localStorage.removeItem(AUTH_STORAGE_KEY);
  },
};
