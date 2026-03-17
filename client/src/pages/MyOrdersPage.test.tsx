import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { ordersApi } from '@/features/orders/api/orders.api';
import { MyOrdersPage } from '@/pages/MyOrdersPage';

vi.mock('@/features/orders/api/orders.api', () => ({
  ordersApi: {
    getMyOrders: vi.fn(),
    getMyOrderDetails: vi.fn(),
    getSoldOrders: vi.fn(),
    getSoldOrderDetails: vi.fn(),
  },
}));

describe('MyOrdersPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders empty state when there are no orders', async () => {
    vi.mocked(ordersApi.getMyOrders).mockResolvedValue({
      items: [],
      totalItems: 0,
      pageIndex: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <MyOrdersPage />
      </MemoryRouter>,
    );

    expect(screen.getByText('Loading your orders')).toBeInTheDocument();
    expect(await screen.findByText('No orders found')).toBeInTheDocument();
  });

  it('renders error state when API request fails', async () => {
    vi.mocked(ordersApi.getMyOrders).mockRejectedValue(new Error('Network error'));

    render(
      <MemoryRouter>
        <MyOrdersPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(screen.getByText('Could not load your orders')).toBeInTheDocument();
    });
  });

  it('renders richer order, payment, method, and settlement statuses', async () => {
    vi.mocked(ordersApi.getMyOrders).mockResolvedValue({
      items: [
        {
          id: 'order-1',
          buyerId: 'buyer-1',
          customerFirstName: 'Maria',
          customerLastName: 'Ivanova',
          email: 'maria@example.com',
          phoneNumber: null,
          country: 'Bulgaria',
          city: 'Sofia',
          addressLine: '1 Vitosha Blvd',
          postalCode: null,
          total: { primary: { amount: 20, currency: 'EUR' } },
          paymentMethod: 'cashOnDelivery',
          status: 'pendingConfirmation',
          paymentStatus: 'notRequired',
          settlementStatus: 'pending',
          platformFeePercent: 10,
          platformFeeAmount: { primary: { amount: 2, currency: 'EUR' } },
          sellerNetAmount: { primary: { amount: 18, currency: 'EUR' } },
          createdOn: '2026-01-01T10:00:00Z',
          items: [],
        },
      ],
      totalItems: 1,
      pageIndex: 1,
      pageSize: 10,
    });

    render(
      <MemoryRouter>
        <MyOrdersPage />
      </MemoryRouter>,
    );

    expect(await screen.findByText('Cash on delivery')).toBeInTheDocument();
    expect(screen.getAllByText('Pending confirmation').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Not required').length).toBeGreaterThan(0);
    expect(screen.getByText('Pending settlement')).toBeInTheDocument();
  });
});
