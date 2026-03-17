import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { adminBooksModerationApi } from '@/features/admin/api/adminBooksModeration.api';
import { AdminBooksModerationPage } from '@/pages/admin/AdminBooksModerationPage';

vi.mock('@/features/admin/api/adminBooksModeration.api', () => ({
  adminBooksModerationApi: {
    getBooks: vi.fn(),
    approveBook: vi.fn(),
    rejectBook: vi.fn(),
    deleteBook: vi.fn(),
  },
}));

const booksResponse = {
  items: [
    {
      id: 'book-1',
      title: 'Deep Work',
      author: 'Cal Newport',
      genre: 'Productivity',
      description: null,
      publisher: 'Piatkus',
      publishedOn: '2016-01-05',
      isbn: '9781455586691',
      creatorId: 'seller-1',
      isApproved: false,
      rejectionReason: 'Missing ISBN proof image',
      createdOn: '2026-02-01T12:00:00Z',
      modifiedOn: null,
      approvedOn: null,
      approvedBy: null,
    },
  ],
  totalItems: 1,
  pageIndex: 1,
  pageSize: 10,
};

describe('AdminBooksModerationPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders moderation status and rejection reason', async () => {
    vi.mocked(adminBooksModerationApi.getBooks).mockResolvedValue(booksResponse);

    render(<AdminBooksModerationPage />);

    expect(await screen.findByText('Rejected')).toBeInTheDocument();
    expect(screen.getByText(/Rejection reason:/)).toBeInTheDocument();
    expect(screen.getByText(/Missing ISBN proof image/)).toBeInTheDocument();
  });

  it('submits approve and reject moderation actions', async () => {
    vi.mocked(adminBooksModerationApi.getBooks).mockResolvedValue(booksResponse);
    vi.mocked(adminBooksModerationApi.approveBook).mockResolvedValue();
    vi.mocked(adminBooksModerationApi.rejectBook).mockResolvedValue();

    render(<AdminBooksModerationPage />);

    await screen.findByText('Deep Work');

    await userEvent.click(screen.getByRole('button', { name: 'Approve' }));
    await waitFor(() => {
      expect(adminBooksModerationApi.approveBook).toHaveBeenCalledWith('book-1');
    });

    await userEvent.click(screen.getByRole('button', { name: 'Reject' }));

    const dialog = await screen.findByRole('dialog');
    await userEvent.type(within(dialog).getByLabelText('Rejection reason'), 'Needs a complete metadata update');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Reject' }));

    await waitFor(() => {
      expect(adminBooksModerationApi.rejectBook).toHaveBeenCalledWith('book-1', 'Needs a complete metadata update');
    });
  });
});
