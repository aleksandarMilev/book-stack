import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { listingsApi } from '@/features/marketplace/api/listings.api';
import { sellerProfilesApi } from '@/features/sellerProfiles/api/sellerProfiles.api';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { MyListingEditPage } from '@/pages/MyListingEditPage';
import { useAuthStore } from '@/store/auth.store';
import type { AuthSession } from '@/types/auth.types';

vi.mock('@/features/marketplace/api/listings.api', () => ({
  listingsApi: {
    getListings: vi.fn(),
    getMineListings: vi.fn(),
    getListingById: vi.fn(),
    deleteListing: vi.fn(),
    createListing: vi.fn(),
    editListing: vi.fn(),
  },
}));

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
    id: 'seller-1',
    role: 'buyer',
  },
});

const activeSellerProfile = {
  userId: 'seller-1',
  displayName: 'Seller',
  phoneNumber: '+359111111111',
  supportsOnlinePayment: true,
  supportsCashOnDelivery: true,
  isActive: true,
  createdOn: '2026-01-01T10:00:00Z',
  modifiedOn: null,
};

const listingResponse = {
  id: 'listing-1',
  bookId: 'book-1',
  title: 'Editable Listing',
  author: 'Author One',
  genre: 'History',
  creatorId: 'seller-1',
  supportsOnlinePayment: true,
  supportsCashOnDelivery: true,
  condition: 'good' as const,
  quantity: 2,
  description: 'A ready listing description.',
  imageUrl: '',
  isApproved: true,
  rejectionReason: null,
  createdOn: '2026-01-01T10:00:00Z',
  modifiedOn: null,
  price: { primary: { amount: 14.5, currency: 'EUR' as const } },
};

describe('MyListingEditPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ session: null });
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'idle',
      loadedForUserId: null,
    });
  });

  it('loads listing, shows reapproval message, and submits edited data', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue(activeSellerProfile);
    vi.mocked(listingsApi.getListingById).mockResolvedValue(listingResponse);
    vi.mocked(listingsApi.editListing).mockResolvedValue();

    render(
      <MemoryRouter initialEntries={['/my-listings/listing-1/edit']}>
        <Routes>
          <Route element={<MyListingEditPage />} path="/my-listings/:listingId/edit" />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText('Editable Listing')).toBeInTheDocument();
    expect(screen.getByText(/triggers reapproval/i)).toBeInTheDocument();

    await userEvent.clear(screen.getByLabelText('Price (EUR)'));
    await userEvent.type(screen.getByLabelText('Price (EUR)'), '18');
    await userEvent.clear(screen.getByLabelText('Quantity'));
    await userEvent.type(screen.getByLabelText('Quantity'), '3');
    await userEvent.clear(screen.getByLabelText('Listing description'));
    await userEvent.type(
      screen.getByLabelText('Listing description'),
      'Updated listing description for moderation.',
    );

    await userEvent.click(screen.getByRole('button', { name: 'Save listing changes' }));

    await waitFor(() => {
      expect(listingsApi.editListing).toHaveBeenCalledWith(
        'listing-1',
        expect.objectContaining({
          bookId: 'book-1',
          price: 18,
          quantity: 3,
          currency: 'EUR',
        }),
      );
    });
  });
});
