import { describe, expect, it } from 'vitest';

import { mapJwtToSession } from '@/features/auth/utils/jwtSession';

const CLAIM_NAME_IDENTIFIER = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
const CLAIM_NAME = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const CLAIM_EMAIL = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';
const CLAIM_ROLE = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

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

describe('jwtSession.mapJwtToSession', () => {
  it('maps valid claims to session model', () => {
    const token = createMockJwt({
      [CLAIM_NAME_IDENTIFIER]: 'user-1',
      [CLAIM_NAME]: 'Reader One',
      [CLAIM_EMAIL]: 'reader@example.com',
      [CLAIM_ROLE]: 'Seller',
      exp: Math.floor(Date.now() / 1000) + 60 * 60,
    });

    const session = mapJwtToSession(token);

    expect(session).not.toBeNull();
    expect(session?.user.id).toBe('user-1');
    expect(session?.user.displayName).toBe('Reader One');
    expect(session?.user.email).toBe('reader@example.com');
    expect(session?.user.role).toBe('seller');
  });

  it('returns null for expired token', () => {
    const token = createMockJwt({
      sub: 'user-1',
      exp: Math.floor(Date.now() / 1000) - 60,
    });

    expect(mapJwtToSession(token)).toBeNull();
  });
});
