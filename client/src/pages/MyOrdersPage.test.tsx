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
});
