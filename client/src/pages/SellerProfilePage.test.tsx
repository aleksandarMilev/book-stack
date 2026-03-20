import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { sellerProfilesApi } from '@/features/sellerProfiles/api/sellerProfiles.api';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import i18n from '@/i18n';
import { SellerProfilePage } from '@/pages/SellerProfilePage';
import { useAuthStore } from '@/store/auth.store';
import type { AuthSession } from '@/types/auth.types';

vi.mock('@/features/sellerProfiles/api/sellerProfiles.api', () => ({
  sellerProfilesApi: {
    getMine: vi.fn(),
    upsertMine: vi.fn(),
  },
}));

const createSession = (): AuthSession => ({
  accessToken: 'mock-token',
  expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
  user: {
    id: 'user-1',
    role: 'buyer',
  },
});

describe('SellerProfilePage', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en');
  });

  afterEach(async () => {
    vi.clearAllMocks();
    useAuthStore.setState({ session: null });
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'idle',
      loadedForUserId: null,
    });
    await i18n.changeLanguage('en');
  });

  it('shows onboarding when seller profile is missing', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue(null);

    render(
      <MemoryRouter>
        <SellerProfilePage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Become a seller')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Save seller profile' })).toBeInTheDocument();
    expect(screen.getByText('Seller profile status is controlled by administrators.')).toBeInTheDocument();
    expect(screen.queryByLabelText('Seller profile is active')).not.toBeInTheDocument();
  });

  it('shows seller route-access notice when redirected from seller-protected routes', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue(null);

    render(
      <MemoryRouter initialEntries={[{ pathname: '/seller/profile', state: { reason: 'sellerProfileRequired' } }]}>
        <SellerProfilePage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Seller access requires profile setup')).toBeInTheDocument();
    expect(
      screen.getByText('Complete or reactivate your seller profile to continue with seller tools.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Browse marketplace' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Continue setup' })).toBeInTheDocument();
  });

  it('renders seller profile onboarding and form actions in Bulgarian locale', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue(null);
    await i18n.changeLanguage('bg');

    render(
      <MemoryRouter>
        <SellerProfilePage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Стани продавач')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Запази профила на продавача' })).toBeInTheDocument();
    expect(
      screen.getByText('Статусът на профила на продавача се управлява от администраторите.'),
    ).toBeInTheDocument();
    expect(screen.getByLabelText('Търговско име на продавача')).toBeInTheDocument();
  });

  it('saves seller profile and shows success message', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue(null);
    vi.mocked(sellerProfilesApi.upsertMine).mockResolvedValue({
      userId: 'user-1',
      displayName: 'Book Corner',
      phoneNumber: '+359888111222',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      isActive: true,
      createdOn: '2026-03-17T10:00:00Z',
      modifiedOn: '2026-03-17T10:02:00Z',
    });

    render(
      <MemoryRouter>
        <SellerProfilePage />
      </MemoryRouter>,
    );

    await screen.findByText('Become a seller');

    await userEvent.clear(screen.getByLabelText('Seller display name'));
    await userEvent.type(screen.getByLabelText('Seller display name'), 'Book Corner');
    await userEvent.type(screen.getByLabelText('Phone number'), '+359888111222');

    await userEvent.click(screen.getByRole('button', { name: 'Save seller profile' }));

    await waitFor(() => {
      expect(sellerProfilesApi.upsertMine).toHaveBeenCalledWith({
        displayName: 'Book Corner',
        phoneNumber: '+359888111222',
        supportsOnlinePayment: true,
        supportsCashOnDelivery: true,
      });
    });

    expect(await screen.findByText('Seller profile saved successfully.')).toBeInTheDocument();
  });

  it('shows save error when upsert fails', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue(null);
    vi.mocked(sellerProfilesApi.upsertMine).mockRejectedValue(new Error('boom'));

    render(
      <MemoryRouter>
        <SellerProfilePage />
      </MemoryRouter>,
    );

    await screen.findByText('Become a seller');
    await userEvent.type(screen.getByLabelText('Seller display name'), 'Book Corner');
    await userEvent.click(screen.getByRole('button', { name: 'Save seller profile' }));

    expect(await screen.findByText('Could not save seller profile.')).toBeInTheDocument();
  });

  it('shows load error state when seller profile request fails', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockRejectedValue(new Error('load failed'));

    render(
      <MemoryRouter>
        <SellerProfilePage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Could not load seller profile')).toBeInTheDocument();
    expect(screen.getByText('Please try again in a moment.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument();
  });
});
