export type UserRole = 'buyer' | 'seller' | 'admin';

export interface AuthUser {
  id: string;
  role: UserRole;
  displayName?: string;
  email?: string;
}

export interface AuthSession {
  accessToken: string;
  refreshToken?: string;
  expiresAtUtc?: string;
  user: AuthUser;
}

export interface AuthCapabilities {
  isAuthenticated: boolean;
  canAccessSellerArea: boolean;
  canAccessAdminArea: boolean;
}
