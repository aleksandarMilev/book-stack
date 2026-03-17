import { httpClient } from '@/api/httpClient';
import type { PaginatedResponse } from '@/api/types/api.types';
import type {
  AdminApprovalFilter,
  AdminBooksModerationItem,
  AdminBooksModerationQuery,
} from '@/features/admin/types/admin.types';
import { ADMIN_BOOK_SORT_TO_BACKEND } from '@/features/admin/types/admin.types';
import { booksApi } from '@/features/books/api/books.api';

const ADMIN_BOOKS_BASE_PATH = '/Admin/Books';

const toApprovedFilter = (approval: AdminApprovalFilter | undefined): boolean | undefined => {
  if (approval === 'approved') {
    return true;
  }

  if (approval === 'unapproved') {
    return false;
  }

  return undefined;
};

const getListQuery = (query: AdminBooksModerationQuery) => ({
  searchTerm: query.searchTerm,
  title: query.title,
  author: query.author,
  genre: query.genre,
  sorting: query.sorting ? ADMIN_BOOK_SORT_TO_BACKEND[query.sorting] : undefined,
  pageIndex: query.pageIndex,
  pageSize: query.pageSize,
  isApproved: toApprovedFilter(query.approval),
});

export const adminBooksModerationApi = {
  async getBooks(query: AdminBooksModerationQuery): Promise<PaginatedResponse<AdminBooksModerationItem>> {
    return booksApi.getBooks(getListQuery(query));
  },

  async approveBook(id: string): Promise<void> {
    await httpClient.post(`${ADMIN_BOOKS_BASE_PATH}/${id}/approve/`);
  },

  async rejectBook(id: string, rejectionReason: string): Promise<void> {
    await httpClient.post(`${ADMIN_BOOKS_BASE_PATH}/${id}/reject/`, { rejectionReason });
  },

  async deleteBook(id: string): Promise<void> {
    await httpClient.delete(`${ADMIN_BOOKS_BASE_PATH}/${id}/`);
  },
};
