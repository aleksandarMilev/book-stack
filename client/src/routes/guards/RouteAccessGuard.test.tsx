import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';

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

  it('blocks buyer users from seller routes', () => {
    useAuthStore.setState({ session: createSession('buyer') });

    renderGuard('seller');

    expect(screen.getByText('home-page')).toBeInTheDocument();
  });

  it('allows seller users into seller routes', () => {
    useAuthStore.setState({ session: createSession('seller') });

    renderGuard('seller');

    expect(screen.getByText('protected-page')).toBeInTheDocument();
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
