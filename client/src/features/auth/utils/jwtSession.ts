import { jwtDecode } from 'jwt-decode';

import type { AuthSession, UserRole } from '@/types/auth.types';

interface JwtClaims {
  exp?: number;
  sub?: string;
  name?: string;
  email?: string;
  role?: string | string[];
  [key: string]: unknown;
}

const CLAIM_NAME_IDENTIFIER = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
const CLAIM_NAME = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const CLAIM_EMAIL = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';
const CLAIM_ROLE = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

const isNonEmptyString = (value: unknown): value is string => typeof value === 'string' && value.length > 0;

const normalizeRole = (rawRole: string | undefined): UserRole => {
  if (!rawRole) {
    return 'buyer';
  }

  const normalizedRole = rawRole.trim().toLowerCase();
  if (normalizedRole === 'administrator' || normalizedRole === 'admin') {
    return 'admin';
  }

  if (normalizedRole === 'seller') {
    return 'seller';
  }

  return 'buyer';
};

const getClaim = (claims: JwtClaims, ...keys: string[]): string | undefined => {
  for (const key of keys) {
    const claimValue = claims[key];
    if (isNonEmptyString(claimValue)) {
      return claimValue;
    }
  }

  return undefined;
};

const getRoleClaim = (claims: JwtClaims): string | undefined => {
  const roleClaim = claims[CLAIM_ROLE] ?? claims.role;

  if (Array.isArray(roleClaim)) {
    return roleClaim.find(isNonEmptyString);
  }

  return isNonEmptyString(roleClaim) ? roleClaim : undefined;
};

export function getTokenExpirationIso(expirationUnixSeconds?: number): string | undefined {
  if (!expirationUnixSeconds || Number.isNaN(expirationUnixSeconds)) {
    return undefined;
  }

  return new Date(expirationUnixSeconds * 1000).toISOString();
}

export function isExpirationIsoExpired(expirationIso?: string): boolean {
  if (!expirationIso) {
    return false;
  }

  const expirationTime = Date.parse(expirationIso);
  if (Number.isNaN(expirationTime)) {
    return true;
  }

  return expirationTime <= Date.now();
}

export function mapJwtToSession(token: string): AuthSession | null {
  try {
    const claims = jwtDecode<JwtClaims>(token);
    const userId = getClaim(claims, CLAIM_NAME_IDENTIFIER, 'sub');
    const displayName = getClaim(claims, CLAIM_NAME, 'name');
    const email = getClaim(claims, CLAIM_EMAIL, 'email');

    if (!userId) {
      return null;
    }

    const expirationIso = getTokenExpirationIso(claims.exp);
    if (isExpirationIsoExpired(expirationIso)) {
      return null;
    }

    return {
      accessToken: token,
      ...(expirationIso ? { expiresAtUtc: expirationIso } : {}),
      user: {
        id: userId,
        role: normalizeRole(getRoleClaim(claims)),
        ...(displayName ? { displayName } : {}),
        ...(email ? { email } : {}),
      },
    };
  } catch {
    return null;
  }
}
