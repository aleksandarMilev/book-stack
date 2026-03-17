import { httpClient } from '@/api/httpClient';
import type { PaginatedResponse } from '@/api/types/api.types';
import type {
  AdminApprovalFilter,
  AdminListingsModerationQuery,
} from '@/features/admin/types/admin.types';
import { ADMIN_LISTING_SORT_TO_BACKEND } from '@/features/admin/types/admin.types';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import { MARKETPLACE_CONDITION_TO_BACKEND } from '@/features/marketplace/types';
import type { MarketplaceListing } from '@/types/marketplace.types';

const ADMIN_LISTINGS_BASE_PATH = '/Admin/BookListings';

const toApprovedFilter = (approval: AdminApprovalFilter | undefined): boolean | undefined => {
  if (approval === 'approved') {
    return true;
  }

  if (approval === 'unapproved') {
    return false;
  }

  return undefined;
};

export const adminListingsModerationApi = {
  async getListings(query: AdminListingsModerationQuery): Promise<PaginatedResponse<MarketplaceListing>> {
    return listingsApi.getListings({
      searchTerm: query.searchTerm,
      title: query.title,
      author: query.author,
      genre: query.genre,
      condition: query.condition ? MARKETPLACE_CONDITION_TO_BACKEND[query.condition] : undefined,
      priceFrom: query.priceFrom,
      priceTo: query.priceTo,
      sorting: query.sorting ? ADMIN_LISTING_SORT_TO_BACKEND[query.sorting] : undefined,
      pageIndex: query.pageIndex,
      pageSize: query.pageSize,
      isApproved: toApprovedFilter(query.approval),
    });
  },

  async approveListing(id: string): Promise<void> {
    await httpClient.post(`${ADMIN_LISTINGS_BASE_PATH}/${id}/approve/`);
  },

  async rejectListing(id: string, rejectionReason: string): Promise<void> {
    await httpClient.post(`${ADMIN_LISTINGS_BASE_PATH}/${id}/reject/`, { rejectionReason });
  },

  async deleteListing(id: string): Promise<void> {
    await httpClient.delete(`${ADMIN_LISTINGS_BASE_PATH}/${id}/`);
  },
};
