import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { adminStatisticsApi } from '@/features/admin/api/adminStatistics.api';
import { AdminDashboardPage } from '@/pages/admin/AdminDashboardPage';

vi.mock('@/features/admin/api/adminStatistics.api', () => ({
  adminStatisticsApi: {
    getStatistics: vi.fn(),
  },
}));

describe('AdminDashboardPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders statistics and revenue groups from backend data', async () => {
    vi.mocked(adminStatisticsApi.getStatistics).mockResolvedValue({
      totalUsers: 120,
      totalBooks: 340,
      totalListings: 890,
      pendingBooks: 12,
      pendingListings: 17,
      totalOrders: 220,
      paidOrders: 170,
      revenueByMonth: [
        {
          year: 2026,
          month: 1,
          currency: 'BGN',
          revenue: 1540.5,
          paidOrders: 11,
        },
      ],
    });

    render(<AdminDashboardPage />);

    expect(await screen.findByText('Total users')).toBeInTheDocument();
    expect(screen.getByText('120')).toBeInTheDocument();
    expect(screen.getByText('Revenue by month')).toBeInTheDocument();
    expect(screen.getByText('11 paid orders')).toBeInTheDocument();
  });

  it('renders error state and retries loading', async () => {
    vi.mocked(adminStatisticsApi.getStatistics)
      .mockRejectedValueOnce(new Error('boom'))
      .mockResolvedValueOnce({
        totalUsers: 1,
        totalBooks: 2,
        totalListings: 3,
        pendingBooks: 0,
        pendingListings: 1,
        totalOrders: 4,
        paidOrders: 2,
        revenueByMonth: [],
      });

    render(<AdminDashboardPage />);

    expect(await screen.findByText('Could not load dashboard')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));

    await waitFor(() => {
      expect(screen.getByText('Total users')).toBeInTheDocument();
    });
    expect(adminStatisticsApi.getStatistics).toHaveBeenCalledTimes(2);
  });
});
