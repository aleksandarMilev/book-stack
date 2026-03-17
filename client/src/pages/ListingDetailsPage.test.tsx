import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { listingsApi } from '@/features/marketplace/api/listings.api';
import { ListingDetailsPage } from '@/pages/ListingDetailsPage';

vi.mock('@/features/marketplace/api/listings.api', () => ({
  listingsApi: {
    getListings: vi.fn(),
    getMineListings: vi.fn(),
    getListingById: vi.fn(),
    deleteListing: vi.fn(),
  },
}));

describe('ListingDetailsPage purchase flow', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('navigates to checkout with selected quantity', async () => {
    vi.mocked(listingsApi.getListingById).mockResolvedValue({
      id: 'listing-10',
      bookId: 'book-10',
      title: 'The Pragmatic Reader',
      author: 'Ana Writer',
      genre: 'Fiction',
      creatorId: 'seller-1',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      condition: 'likeNew',
      quantity: 4,
      description: 'desc',
      imageUrl: '',
      isApproved: true,
      rejectionReason: null,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
      price: { primary: { amount: 15, currency: 'BGN' } },
    });

    render(
      <MemoryRouter initialEntries={['/marketplace/listing-10']}>
        <Routes>
          <Route element={<ListingDetailsPage />} path="/marketplace/:listingId" />
          <Route element={<p>checkout-screen</p>} path="/checkout" />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText('Buy this listing')).toBeInTheDocument();

    await userEvent.clear(screen.getByLabelText('Quantity'));
    await userEvent.type(screen.getByLabelText('Quantity'), '3');
    await userEvent.click(screen.getByRole('button', { name: 'Buy now' }));

    await waitFor(() => {
      expect(screen.getByText('checkout-screen')).toBeInTheDocument();
    });
  });
});
