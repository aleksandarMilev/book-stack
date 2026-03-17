import type { BookModel } from '@/features/books/api/books.api';
import type { MarketplaceListingCondition } from '@/types/marketplace.types';

export interface AdminMonthlyRevenue {
  year: number;
  month: number;
  currency: string;
  grossOrderVolume: number;
  recognizedPlatformFeeRevenue: number;
  recognizedSellerNetRevenue: number;
  pendingSettlementAmount: number;
  unearnedPlatformFeeAmount: number;
  orders: number;
  paidOnlineOrders: number;
  codOrders: number;
}

export interface AdminStatistics {
  totalUsers: number;
  totalSellerProfiles: number;
  activeSellerProfiles: number;
  totalBooks: number;
  totalListings: number;
  pendingBooks: number;
  pendingListings: number;
  totalOrders: number;
  paidOnlineOrders: number;
  codOrders: number;
  totalPendingSettlementAmount: number;
  revenueByMonth: AdminMonthlyRevenue[];
}

export type ModerationStatus = 'approved' | 'pending' | 'rejected';
export type AdminApprovalFilter = 'all' | 'approved' | 'unapproved';

export type AdminBookSortOption =
  | 'newest'
  | 'oldest'
  | 'titleAsc'
  | 'titleDesc'
  | 'publishedDateDesc'
  | 'publishedDateAsc';

export const ADMIN_BOOK_SORT_TO_BACKEND: Record<AdminBookSortOption, number> = {
  newest: 0,
  oldest: 1,
  titleAsc: 2,
  titleDesc: 3,
  publishedDateDesc: 4,
  publishedDateAsc: 5,
};

export interface AdminBooksModerationQuery {
  searchTerm?: string | undefined;
  title?: string | undefined;
  author?: string | undefined;
  genre?: string | undefined;
  approval?: AdminApprovalFilter | undefined;
  sorting?: AdminBookSortOption | undefined;
  pageIndex?: number | undefined;
  pageSize?: number | undefined;
}

export type AdminBooksModerationItem = BookModel;

export type AdminListingSortOption =
  | 'newest'
  | 'oldest'
  | 'priceAsc'
  | 'priceDesc'
  | 'titleAsc'
  | 'titleDesc'
  | 'publishedDateAsc'
  | 'publishedDateDesc';

export const ADMIN_LISTING_SORT_TO_BACKEND: Record<AdminListingSortOption, number> = {
  newest: 0,
  oldest: 1,
  priceAsc: 2,
  priceDesc: 3,
  titleAsc: 4,
  titleDesc: 5,
  publishedDateAsc: 6,
  publishedDateDesc: 7,
};

export interface AdminListingsModerationQuery {
  searchTerm?: string | undefined;
  title?: string | undefined;
  author?: string | undefined;
  genre?: string | undefined;
  condition?: MarketplaceListingCondition | undefined;
  priceFrom?: number | undefined;
  priceTo?: number | undefined;
  approval?: AdminApprovalFilter | undefined;
  sorting?: AdminListingSortOption | undefined;
  pageIndex?: number | undefined;
  pageSize?: number | undefined;
}
