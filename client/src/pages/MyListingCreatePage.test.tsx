import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { booksApi } from '@/features/books/api/books.api';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import { sellerProfilesApi } from '@/features/sellerProfiles/api/sellerProfiles.api';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { MyListingCreatePage } from '@/pages/MyListingCreatePage';
import { useAuthStore } from '@/store/auth.store';
import type { AuthSession } from '@/types/auth.types';

vi.mock('@/features/books/api/books.api', () => ({
  booksApi: {
    getBooks: vi.fn(),
    getBookById: vi.fn(),
    lookupBooks: vi.fn(),
    createBook: vi.fn(),
  },
}));

vi.mock('@/features/marketplace/api/listings.api', () => ({
  listingsApi: {
    getListings: vi.fn(),
    getMineListings: vi.fn(),
    getListingById: vi.fn(),
    deleteListing: vi.fn(),
    createListing: vi.fn(),
    createListingWithBook: vi.fn(),
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

describe('MyListingCreatePage', () => {
  afterEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ session: null });
    useSellerProfileStore.setState({
      profile: null,
      loadState: 'idle',
      loadedForUserId: null,
    });
  });

  it('creates listing through existing canonical book path', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue(activeSellerProfile);
    vi.mocked(booksApi.lookupBooks).mockResolvedValue([
      {
        id: 'book-1',
        title: 'Existing Book',
        author: 'Author Existing',
        genre: 'Fiction',
        isbn: '9780000000001',
      },
    ]);
    vi.mocked(listingsApi.createListing).mockResolvedValue('listing-1');

    render(
      <MemoryRouter>
        <MyListingCreatePage />
      </MemoryRouter>,
    );

    await screen.findByText('Create listing');

    await userEvent.type(screen.getByLabelText('Search canonical books'), 'Existing');
    await userEvent.click(screen.getByRole('button', { name: 'Search' }));
    const existingBookLabel = await screen.findByText('Existing Book');
    const existingBookButton = existingBookLabel.closest('button');
    if (!existingBookButton) {
      throw new Error('Expected existing book result button.');
    }

    await userEvent.click(existingBookButton);

    await userEvent.type(screen.getByLabelText('Price (EUR)'), '19.99');
    await userEvent.clear(screen.getByLabelText('Quantity'));
    await userEvent.type(screen.getByLabelText('Quantity'), '2');
    await userEvent.type(
      screen.getByLabelText('Listing description'),
      'Very clean copy with minimal signs of use.',
    );

    await userEvent.click(screen.getByRole('button', { name: 'Submit listing for moderation' }));

    await waitFor(() => {
      expect(listingsApi.createListing).toHaveBeenCalledWith(
        expect.objectContaining({
          bookId: 'book-1',
          price: 19.99,
          quantity: 2,
          currency: 'EUR',
        }),
      );
    });
    expect(booksApi.createBook).not.toHaveBeenCalled();
  });

  it('switches to missing-book flow and submits atomic create-with-book request', async () => {
    useAuthStore.setState({ session: createSession() });
    vi.mocked(sellerProfilesApi.getMine).mockResolvedValue(activeSellerProfile);
    vi.mocked(listingsApi.createListingWithBook).mockResolvedValue('listing-new');

    render(
      <MemoryRouter>
        <MyListingCreatePage />
      </MemoryRouter>,
    );

    await screen.findByText('Create listing');
    await userEvent.click(screen.getByRole('button', { name: 'Book is missing' }));

    await userEvent.type(screen.getByLabelText('Book title'), 'Missing Book');
    await userEvent.type(screen.getByLabelText('Book author'), 'New Author');
    await userEvent.type(screen.getByLabelText('Book genre'), 'Fantasy');
    await userEvent.type(screen.getByLabelText('Price (EUR)'), '15');
    await userEvent.clear(screen.getByLabelText('Quantity'));
    await userEvent.type(screen.getByLabelText('Quantity'), '1');
    await userEvent.type(
      screen.getByLabelText('Listing description'),
      'First listing for this canonical book.',
    );

    await userEvent.click(screen.getByRole('button', { name: 'Submit listing for moderation' }));

    await waitFor(() => {
      expect(listingsApi.createListingWithBook).toHaveBeenCalledWith(
        expect.objectContaining({
          title: 'Missing Book',
          author: 'New Author',
          genre: 'Fantasy',
          currency: 'EUR',
        }),
      );
    });
    expect(booksApi.createBook).not.toHaveBeenCalled();
    expect(listingsApi.createListing).not.toHaveBeenCalled();
  });
});
