import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { ordersApi } from '@/features/orders/api/orders.api';
import { SellerSoldOrdersPage } from '@/pages/SellerSoldOrdersPage';

vi.mock('@/features/orders/api/orders.api', () => ({
  ordersApi: {
    getMyOrders: vi.fn(),
    getMyOrderDetails: vi.fn(),
    getSoldOrders: vi.fn(),
    getSoldOrderDetails: vi.fn(),
  },
}));

describe('SellerSoldOrdersPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders sold orders and opens seller details', async () => {
    vi.mocked(ordersApi.getSoldOrders).mockResolvedValue({
      items: [
        {
          id: 'order-1',
          customerFirstName: 'Maria',
          customerLastName: 'Ivanova',
          email: 'maria@example.com',
          phoneNumber: null,
          country: 'Bulgaria',
          city: 'Sofia',
          addressLine: '1 Vitosha Blvd',
          postalCode: '1000',
          sellerTotal: { primary: { amount: 32.2, currency: 'BGN' } },
          status: 'confirmed',
          paymentStatus: 'paid',
          createdOn: '2026-01-05T12:00:00Z',
          items: [
            {
              id: 'item-1',
              listingId: 'listing-1',
              bookId: 'book-1',
              bookTitle: 'Seller Book',
              bookAuthor: 'Author Seller',
              bookGenre: 'Fiction',
              bookPublisher: null,
              bookPublishedOn: null,
              bookIsbn: null,
              unitPrice: { primary: { amount: 16.1, currency: 'BGN' } },
              quantity: 2,
              totalPrice: { primary: { amount: 32.2, currency: 'BGN' } },
              condition: 'good',
              listingDescription: 'A sold listing',
              listingImageUrl: '',
            },
          ],
        },
      ],
      totalItems: 1,
      pageIndex: 1,
      pageSize: 10,
    });

    vi.mocked(ordersApi.getSoldOrderDetails).mockResolvedValue({
      id: 'order-1',
      customerFirstName: 'Maria',
      customerLastName: 'Ivanova',
      email: 'maria@example.com',
      phoneNumber: null,
      country: 'Bulgaria',
      city: 'Sofia',
      addressLine: '1 Vitosha Blvd',
      postalCode: '1000',
      sellerTotal: { primary: { amount: 32.2, currency: 'BGN' } },
      status: 'confirmed',
      paymentStatus: 'paid',
      createdOn: '2026-01-05T12:00:00Z',
      items: [
        {
          id: 'item-1',
          listingId: 'listing-1',
          bookId: 'book-1',
          bookTitle: 'Seller Book',
          bookAuthor: 'Author Seller',
          bookGenre: 'Fiction',
          bookPublisher: null,
          bookPublishedOn: null,
          bookIsbn: null,
          unitPrice: { primary: { amount: 16.1, currency: 'BGN' } },
          quantity: 2,
          totalPrice: { primary: { amount: 32.2, currency: 'BGN' } },
          condition: 'good',
          listingDescription: 'A sold listing',
          listingImageUrl: '',
        },
      ],
    });

    render(
      <MemoryRouter>
        <SellerSoldOrdersPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText(/Seller Book/)).toBeInTheDocument();

    await userEvent.click(await screen.findByRole('button', { name: 'Open fulfillment details' }));

    expect(await screen.findByText('Fulfillment details')).toBeInTheDocument();
    expect(ordersApi.getSoldOrderDetails).toHaveBeenCalledWith('order-1');
  });
});
