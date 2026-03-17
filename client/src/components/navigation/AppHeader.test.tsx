import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';

import { AppHeader } from '@/components/navigation/AppHeader';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
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
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'idle',
      loadedForUserId: null,
    });
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

  it('shows seller navigation links only for users with active seller profile', () => {
    useAuthStore.setState({ session: createSession('buyer') });
    useSellerProfileStore.setState({
      profile: {
        userId: 'user-1',
        displayName: 'Active Seller',
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

    renderHeader();

    expect(screen.getAllByText('My listings').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Sold orders').length).toBeGreaterThan(0);
  });

  it('hides seller navigation links for inactive seller profiles', () => {
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

    renderHeader();

    expect(screen.queryByText('My listings')).not.toBeInTheDocument();
    expect(screen.queryByText('Sold orders')).not.toBeInTheDocument();
  });
});
