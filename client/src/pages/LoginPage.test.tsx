import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';

import { LoginPage } from '@/pages/LoginPage';
import { useAuthStore } from '@/store/auth.store';

const renderLoginRoute = (initialState?: { from?: string; reason?: string }) =>
  render(
    <MemoryRouter initialEntries={[{ pathname: '/login', state: initialState }]}>
      <Routes>
        <Route element={<LoginPage />} path="/login" />
      </Routes>
    </MemoryRouter>,
  );

describe('LoginPage route guard context hints', () => {
  afterEach(() => {
    useAuthStore.setState({ session: null });
  });

  it('shows auth required hint when redirected from protected route', async () => {
    renderLoginRoute({ from: '/profile', reason: 'authRequired' });

    expect(await screen.findByText('Sign in required')).toBeInTheDocument();
    expect(await screen.findByText('Please sign in to continue.')).toBeInTheDocument();
  });

  it('shows session expired hint when redirected due expired session', async () => {
    renderLoginRoute({ from: '/my-orders?page=2', reason: 'sessionExpired' });

    expect(await screen.findByText('Session expired')).toBeInTheDocument();
    expect(await screen.findByText('Your session has expired. Please sign in again.')).toBeInTheDocument();
  });

  it('renders forgot-password entrypoint link', async () => {
    renderLoginRoute();

    const forgotPasswordLink = await screen.findByRole('link', { name: 'Forgot password?' });
    expect(forgotPasswordLink).toHaveAttribute('href', '/identity/forgot-password');
  });
});
