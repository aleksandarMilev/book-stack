import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { adminListingsModerationApi } from '@/features/admin/api/adminListingsModeration.api';
import { AdminListingsModerationPage } from '@/pages/admin/AdminListingsModerationPage';

vi.mock('@/features/admin/api/adminListingsModeration.api', () => ({
  adminListingsModerationApi: {
    getListings: vi.fn(),
    approveListing: vi.fn(),
    rejectListing: vi.fn(),
    deleteListing: vi.fn(),
  },
}));

const listingsResponse = {
  items: [
    {
      id: 'listing-1',
      bookId: 'book-1',
      title: 'Sapiens',
      author: 'Yuval Noah Harari',
      genre: 'History',
      publisher: 'Harvill Secker',
      publishedOn: '2014-01-01',
      isbn: '9780062316097',
      creatorId: 'seller-42',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      condition: 'veryGood' as const,
      quantity: 2,
      description: 'Excellent condition listing',
      imageUrl: '',
      isApproved: false,
      rejectionReason: null,
      createdOn: '2026-02-02T09:30:00Z',
      modifiedOn: null,
      price: {
        primary: {
          amount: 25.5,
          currency: 'BGN' as const,
        },
      },
    },
  ],
  totalItems: 1,
  pageIndex: 1,
  pageSize: 10,
};

describe('AdminListingsModerationPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
    vi.unstubAllGlobals();
  });

  it('renders seller-scoped listing information', async () => {
    vi.mocked(adminListingsModerationApi.getListings).mockResolvedValue(listingsResponse);

    render(<AdminListingsModerationPage />);

    expect(await screen.findByText('Sapiens')).toBeInTheDocument();
    expect(screen.getByText('seller-42')).toBeInTheDocument();
    expect(screen.getAllByText('Very good').length).toBeGreaterThan(0);
    expect(screen.getByText('Pending review')).toBeInTheDocument();
  });

  it('triggers delete action with listing id', async () => {
    vi.mocked(adminListingsModerationApi.getListings).mockResolvedValue(listingsResponse);
    vi.mocked(adminListingsModerationApi.deleteListing).mockResolvedValue();
    vi.stubGlobal('confirm', vi.fn(() => true));

    render(<AdminListingsModerationPage />);

    await screen.findByText('Sapiens');

    await userEvent.click(screen.getByRole('button', { name: 'Delete' }));

    await waitFor(() => {
      expect(adminListingsModerationApi.deleteListing).toHaveBeenCalledWith('listing-1');
    });
  });
});
