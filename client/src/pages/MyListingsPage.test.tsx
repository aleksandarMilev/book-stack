import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { listingsApi } from '@/features/marketplace/api/listings.api';
import { sellerProfilesApi } from '@/features/sellerProfiles/api/sellerProfiles.api';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { MyListingsPage } from '@/pages/MyListingsPage';
import { useAuthStore } from '@/store/auth.store';

vi.mock('@/features/marketplace/api/listings.api', () => ({
  listingsApi: {
    getListings: vi.fn(),
    getMineListings: vi.fn(),
    getListingById: vi.fn(),
    deleteListing: vi.fn(),
  },
}));

vi.mock('@/features/sellerProfiles/api/sellerProfiles.api', () => ({
  sellerProfilesApi: {
    getMine: vi.fn(),
    upsertMine: vi.fn(),
  },
}));

describe('MyListingsPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ session: null });
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'idle',
      loadedForUserId: null,
    });
  });

  it('renders approval and rejection states for seller listings', async () => {
    useAuthStore.setState({
      session: {
        accessToken: 'token',
        expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
        user: { id: 'seller-1', role: 'buyer' },
      },
    });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue({
      userId: 'seller-1',
      displayName: 'Seller One',
      phoneNumber: null,
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      isActive: true,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
    });
    vi.mocked(listingsApi.getMineListings).mockResolvedValue({
      items: [
        {
          id: 'listing-approved',
          bookId: 'book-1',
          title: 'Approved Listing',
          author: 'Author One',
          genre: 'Fiction',
          creatorId: 'seller-1',
          condition: 'veryGood',
          quantity: 1,
          description: 'A listing',
          imageUrl: '',
          isApproved: true,
          rejectionReason: null,
          createdOn: '2026-01-01T10:00:00Z',
          modifiedOn: null,
          price: { primary: { amount: 21.5, currency: 'BGN' } },
        },
        {
          id: 'listing-rejected',
          bookId: 'book-2',
          title: 'Rejected Listing',
          author: 'Author Two',
          genre: 'History',
          creatorId: 'seller-1',
          condition: 'good',
          quantity: 2,
          description: 'Another listing',
          imageUrl: '',
          isApproved: false,
          rejectionReason: 'Missing book condition photos',
          createdOn: '2026-01-02T10:00:00Z',
          modifiedOn: null,
          price: { primary: { amount: 12, currency: 'EUR' } },
        },
      ],
      totalItems: 2,
      pageIndex: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <MyListingsPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Approved')).toBeInTheDocument();
    expect(await screen.findByText('Rejected')).toBeInTheDocument();
    expect(await screen.findByText(/Rejection reason:/)).toBeInTheDocument();
    expect(await screen.findByText(/Missing book condition photos/)).toBeInTheDocument();
  });
});
