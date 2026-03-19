import { render, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { listingsApi } from '@/features/marketplace/api/listings.api';
import { MarketplacePage } from '@/pages/MarketplacePage';
import { toPriceDisplayValue } from '@/utils/priceDisplay';

vi.mock('@/features/marketplace/api/listings.api', () => ({
  listingsApi: {
    getListings: vi.fn(),
    getMineListings: vi.fn(),
    getListingById: vi.fn(),
    deleteListing: vi.fn(),
    createListing: vi.fn(),
    updateListing: vi.fn(),
  },
}));

const mockListing = {
  id: 'listing-1',
  bookId: 'book-1',
  title: 'Discovery Ready',
  author: 'Ava Stone',
  genre: 'Fiction',
  publisher: null,
  publishedOn: null,
  isbn: null,
  creatorId: 'seller-1',
  supportsOnlinePayment: true,
  supportsCashOnDelivery: true,
  condition: 'good' as const,
  quantity: 2,
  description: 'Curated listing for tests.',
  imageUrl: '',
  isApproved: true,
  rejectionReason: null,
  createdOn: '2026-01-10T00:00:00Z',
  modifiedOn: null,
  price: toPriceDisplayValue(19.9, 'EUR'),
};

const renderMarketplacePage = (initialEntry = '/marketplace') =>
  render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <MarketplacePage />
    </MemoryRouter>,
  );

describe('MarketplacePage discovery surface', () => {
  beforeEach(() => {
    vi.mocked(listingsApi.getListings).mockResolvedValue({
      items: [mockListing],
      totalItems: 1,
      pageIndex: 1,
      pageSize: 10,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders discovery toolbar and grid structure classes', async () => {
    const { container } = renderMarketplacePage();

    await waitFor(() => {
      expect(listingsApi.getListings).toHaveBeenCalled();
    });

    expect(container.querySelector('.marketplace-page--discovery')).not.toBeNull();
    expect(container.querySelector('.marketplace-toolbar--discovery')).not.toBeNull();
    expect(container.querySelector('.marketplace-toolbar-controls')).not.toBeNull();
    expect(container.querySelector('.marketplace-results--discovery')).not.toBeNull();
    expect(container.querySelector('.marketplace-grid--discovery')).not.toBeNull();
  });

  it('shows results clear button only when active filters are present', async () => {
    const withoutFilters = renderMarketplacePage('/marketplace');

    await waitFor(() => {
      expect(listingsApi.getListings).toHaveBeenCalled();
    });
    expect(withoutFilters.container.querySelector('.marketplace-results-clear')).toBeNull();

    withoutFilters.unmount();

    const withFilters = renderMarketplacePage('/marketplace?genre=Fiction');
    await waitFor(() => {
      expect(listingsApi.getListings).toHaveBeenCalled();
    });
    expect(withFilters.container.querySelector('.marketplace-results-clear')).not.toBeNull();
  });
});
