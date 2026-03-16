import { describe, expect, it } from 'vitest';

import { deriveAuthCapabilities } from '@/store/auth.store';
import type { AuthSession } from '@/types/auth.types';

const createSession = (role: AuthSession['user']['role'], expiresAtUtc: string): AuthSession => ({
  accessToken: 'token',
  expiresAtUtc,
  user: {
    id: 'user-1',
    role,
  },
});

describe('deriveAuthCapabilities', () => {
  it('returns unauthenticated capabilities for expired sessions', () => {
    const capabilities = deriveAuthCapabilities(createSession('admin', new Date(Date.now() - 1000).toISOString()));

    expect(capabilities).toEqual({
      isAuthenticated: false,
      canAccessSellerArea: false,
      canAccessAdminArea: false,
    });
  });

  it('grants seller area access for seller role', () => {
    const capabilities = deriveAuthCapabilities(createSession('seller', new Date(Date.now() + 1000).toISOString()));

    expect(capabilities.isAuthenticated).toBe(true);
    expect(capabilities.canAccessSellerArea).toBe(true);
    expect(capabilities.canAccessAdminArea).toBe(false);
  });
});
