import { afterEach, describe, expect, it } from 'vitest';

import { authStorage } from '@/api/authStorage';

const AUTH_STORAGE_KEY = 'bookstack.auth.session';

const toBase64Url = (value: string): string =>
  btoa(value)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');

const createMockJwt = (payload: Record<string, unknown>): string => {
  const header = toBase64Url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = toBase64Url(JSON.stringify(payload));
  return `${header}.${body}.signature`;
};

describe('authStorage', () => {
  afterEach(() => {
    window.localStorage.clear();
  });

  it('restores a valid stored session', () => {
    const token = createMockJwt({
      sub: 'user-123',
      role: 'seller',
      exp: Math.floor(Date.now() / 1000) + 60 * 60,
    });

    window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify({ accessToken: token }));

    const restoredSession = authStorage.getSession();

    expect(restoredSession).not.toBeNull();
    expect(restoredSession?.user.id).toBe('user-123');
    expect(restoredSession?.user.role).toBe('seller');
  });

  it('clears malformed session payloads', () => {
    window.localStorage.setItem(AUTH_STORAGE_KEY, 'not-json');

    expect(authStorage.getSession()).toBeNull();
    expect(window.localStorage.getItem(AUTH_STORAGE_KEY)).toBeNull();
  });

  it('returns null and clears expired sessions', () => {
    const expiredToken = createMockJwt({
      sub: 'expired-user',
      role: 'buyer',
      exp: Math.floor(Date.now() / 1000) - 60,
    });

    window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify({ accessToken: expiredToken }));

    expect(authStorage.getSession()).toBeNull();
    expect(window.localStorage.getItem(AUTH_STORAGE_KEY)).toBeNull();
  });
});
