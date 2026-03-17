import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';

import { AppHeader } from '@/components/navigation/AppHeader';
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

const renderHeader = () =>
  render(
    <MemoryRouter>
      <AppHeader />
    </MemoryRouter>,
  );

describe('AppHeader', () => {
  afterEach(() => {
    useAuthStore.setState({ session: null });
  });

  it('shows public actions and hides admin links for guests', () => {
    useAuthStore.setState({ session: null });

    renderHeader();

    expect(screen.getAllByText('Login').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Register').length).toBeGreaterThan(0);
    expect(screen.queryByText('Dashboard')).not.toBeInTheDocument();
  });

  it('shows admin navigation for admin sessions', () => {
    useAuthStore.setState({ session: createSession('admin') });

    renderHeader();

    expect(screen.getAllByText('Dashboard').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Books moderation').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Listings moderation').length).toBeGreaterThan(0);
  });

  it('hides admin navigation for non-admin authenticated users', () => {
    useAuthStore.setState({ session: createSession('seller') });

    renderHeader();

    expect(screen.queryByText('Dashboard')).not.toBeInTheDocument();
  });
});
