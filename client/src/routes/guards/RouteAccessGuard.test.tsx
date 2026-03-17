import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';

import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { RouteAccessGuard } from '@/routes/guards/RouteAccessGuard';
import { useAuthStore } from '@/store/auth.store';
import type { AuthSession, UserRole } from '@/types/auth.types';

const createSession = (role: UserRole): AuthSession => ({
  accessToken: 'mock-token',
  expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
  user: {
    id: 'user-1',
    role,
  },
});

const renderGuard = (level: 'authenticated' | 'seller' | 'admin') =>
  render(
    <MemoryRouter initialEntries={['/protected']}>
      <Routes>
        <Route path="/" element={<p>home-page</p>} />
        <Route path="/login" element={<p>login-page</p>} />
        <Route
          path="/seller/profile"
          element={
            <RouteAccessGuard level="authenticated">
              <p>seller-profile-page</p>
            </RouteAccessGuard>
          }
        />
        <Route
          path="/protected"
          element={
            <RouteAccessGuard level={level}>
              <p>protected-page</p>
            </RouteAccessGuard>
          }
        />
      </Routes>
    </MemoryRouter>,
  );

const renderAuthenticatedSellerProfileRoute = () =>
  render(
    <MemoryRouter initialEntries={['/seller/profile']}>
      <Routes>
        <Route path="/login" element={<p>login-page</p>} />
        <Route
          path="/seller/profile"
          element={
            <RouteAccessGuard level="authenticated">
              <p>seller-profile-page</p>
            </RouteAccessGuard>
          }
        />
      </Routes>
    </MemoryRouter>,
  );

function LoginStateProbe() {
  const location = useLocation();
  const state = location.state as { reason?: string; from?: string } | null;

  return (
    <>
      <p>login-page</p>
      <p>reason:{state?.reason ?? '-'}</p>
      <p>from:{state?.from ?? '-'}</p>
    </>
  );
}

describe('RouteAccessGuard', () => {
  afterEach(() => {
    useAuthStore.setState({ session: null });
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'idle',
      loadedForUserId: null,
    });
  });

  it('redirects unauthenticated users to login for authenticated routes', () => {
    useAuthStore.setState({ session: null });

    renderGuard('authenticated');

    expect(screen.getByText('login-page')).toBeInTheDocument();
  });

  it('includes auth reason in login redirect state', () => {
    useAuthStore.setState({ session: null });

    render(
      <MemoryRouter initialEntries={['/protected?tab=recent']}>
        <Routes>
          <Route path="/login" element={<LoginStateProbe />} />
          <Route
            path="/protected"
            element={
              <RouteAccessGuard level="authenticated">
                <p>protected-page</p>
              </RouteAccessGuard>
            }
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText('reason:authRequired')).toBeInTheDocument();
    expect(screen.getByText('from:/protected?tab=recent')).toBeInTheDocument();
  });

  it('blocks unauthenticated users from seller routes', () => {
    useAuthStore.setState({ session: null });

    renderGuard('seller');

    expect(screen.getByText('login-page')).toBeInTheDocument();
  });

  it('redirects authenticated non-seller users to seller profile onboarding route', () => {
    useAuthStore.setState({ session: createSession('buyer') });
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'ready',
      loadedForUserId: 'user-1',
    });

    renderGuard('seller');

    expect(screen.getByText('seller-profile-page')).toBeInTheDocument();
  });

  it('allows authenticated non-seller users to access seller profile route without loop', () => {
    useAuthStore.setState({ session: createSession('buyer') });
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'ready',
      loadedForUserId: 'user-1',
    });

    renderAuthenticatedSellerProfileRoute();

    expect(screen.getByText('seller-profile-page')).toBeInTheDocument();
  });

  it('allows active seller users into seller routes', () => {
    useAuthStore.setState({ session: createSession('seller') });
    useSellerProfileStore.setState({
      profile: {
        userId: 'user-1',
        displayName: 'Seller User',
        phoneNumber: '+359111111111',
        supportsOnlinePayment: true,
        supportsCashOnDelivery: true,
        isActive: true,
        createdOn: '2026-01-01T10:00:00Z',
        modifiedOn: null,
      },
      loadState: 'ready',
      loadedForUserId: 'user-1',
    });

    renderGuard('seller');

    expect(screen.getByText('protected-page')).toBeInTheDocument();
  });

  it('blocks inactive sellers from seller routes', () => {
    useAuthStore.setState({ session: createSession('seller') });
    useSellerProfileStore.setState({
      profile: {
        userId: 'user-1',
        displayName: 'Inactive Seller',
        phoneNumber: '+359111111111',
        supportsOnlinePayment: true,
        supportsCashOnDelivery: true,
        isActive: false,
        createdOn: '2026-01-01T10:00:00Z',
        modifiedOn: null,
      },
      loadState: 'ready',
      loadedForUserId: 'user-1',
    });

    renderGuard('seller');

    expect(screen.getByText('seller-profile-page')).toBeInTheDocument();
  });

  it('shows safe loading state while seller capability is resolving', () => {
    useAuthStore.setState({ session: createSession('seller') });
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'loading',
      loadedForUserId: 'user-1',
    });

    renderGuard('seller');

    expect(screen.getByText('Checking seller access')).toBeInTheDocument();
    expect(screen.queryByText('seller-profile-page')).not.toBeInTheDocument();
    expect(screen.queryByText('protected-page')).not.toBeInTheDocument();
  });

  it('allows admin users into admin routes', () => {
    useAuthStore.setState({ session: createSession('admin') });

    renderGuard('admin');

    expect(screen.getByText('protected-page')).toBeInTheDocument();
  });

  it('blocks non-admin users from admin routes', () => {
    useAuthStore.setState({ session: createSession('seller') });

    renderGuard('admin');

    expect(screen.getByText('home-page')).toBeInTheDocument();
  });
});
