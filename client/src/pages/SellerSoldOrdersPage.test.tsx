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
    confirmSoldOrder: vi.fn(),
    shipSoldOrder: vi.fn(),
    deliverSoldOrder: vi.fn(),
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
          paymentMethod: 'online',
          status: 'confirmed',
          paymentStatus: 'paid',
          settlementStatus: 'pending',
          platformFeePercent: 10,
          platformFeeAmount: { primary: { amount: 3.22, currency: 'EUR' } },
          sellerNetAmount: { primary: { amount: 28.98, currency: 'EUR' } },
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
      paymentMethod: 'online',
      status: 'confirmed',
      paymentStatus: 'paid',
      settlementStatus: 'pending',
      platformFeePercent: 10,
      platformFeeAmount: { primary: { amount: 3.22, currency: 'EUR' } },
      sellerNetAmount: { primary: { amount: 28.98, currency: 'EUR' } },
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
    expect(await screen.findByRole('button', { name: 'Mark as shipped' })).toBeInTheDocument();

    await userEvent.click(await screen.findByRole('button', { name: 'Open fulfillment details' }));

    expect(await screen.findByText('Fulfillment details')).toBeInTheDocument();
    expect(ordersApi.getSoldOrderDetails).toHaveBeenCalledWith('order-1');
  });

  it('shows confirm action for pending confirmation orders and triggers seller action', async () => {
    vi.mocked(ordersApi.getSoldOrders).mockResolvedValue({
      items: [
        {
          id: 'order-2',
          customerFirstName: 'Petar',
          customerLastName: 'Petrov',
          email: 'petar@example.com',
          phoneNumber: null,
          country: 'Bulgaria',
          city: 'Plovdiv',
          addressLine: '2 Center',
          postalCode: null,
          sellerTotal: { primary: { amount: 20, currency: 'EUR' } },
          paymentMethod: 'cashOnDelivery',
          status: 'pendingConfirmation',
          paymentStatus: 'notRequired',
          settlementStatus: 'pending',
          platformFeePercent: 10,
          platformFeeAmount: { primary: { amount: 2, currency: 'EUR' } },
          sellerNetAmount: { primary: { amount: 18, currency: 'EUR' } },
          createdOn: '2026-01-05T12:00:00Z',
          items: [],
        },
      ],
      totalItems: 1,
      pageIndex: 1,
      pageSize: 10,
    });
    vi.mocked(ordersApi.confirmSoldOrder).mockResolvedValue();

    render(
      <MemoryRouter>
        <SellerSoldOrdersPage />
      </MemoryRouter>,
    );

    await screen.findByText('Seller sold orders');
    await userEvent.click(screen.getByRole('button', { name: 'Confirm order' }));

    expect(ordersApi.confirmSoldOrder).toHaveBeenCalledWith('order-2');
  });

  it('shows action error message when seller status update fails', async () => {
    vi.mocked(ordersApi.getSoldOrders).mockResolvedValue({
      items: [
        {
          id: 'order-3',
          customerFirstName: 'Petar',
          customerLastName: 'Petrov',
          email: 'petar@example.com',
          phoneNumber: null,
          country: 'Bulgaria',
          city: 'Plovdiv',
          addressLine: '2 Center',
          postalCode: null,
          sellerTotal: { primary: { amount: 20, currency: 'EUR' } },
          paymentMethod: 'cashOnDelivery',
          status: 'pendingConfirmation',
          paymentStatus: 'notRequired',
          settlementStatus: 'pending',
          platformFeePercent: 10,
          platformFeeAmount: { primary: { amount: 2, currency: 'EUR' } },
          sellerNetAmount: { primary: { amount: 18, currency: 'EUR' } },
          createdOn: '2026-01-05T12:00:00Z',
          items: [],
        },
      ],
      totalItems: 1,
      pageIndex: 1,
      pageSize: 10,
    });
    vi.mocked(ordersApi.confirmSoldOrder).mockRejectedValue(new Error('boom'));

    render(
      <MemoryRouter>
        <SellerSoldOrdersPage />
      </MemoryRouter>,
    );

    await screen.findByText('Seller sold orders');
    await userEvent.click(screen.getByRole('button', { name: 'Confirm order' }));

    expect(await screen.findByText('Could not update sold order status.')).toBeInTheDocument();
  });
});
