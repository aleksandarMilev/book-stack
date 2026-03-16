import type { ListingCondition, MarketplaceGenre } from '@/types/marketplace.types';

export type SortOption = 'featured' | 'priceAsc' | 'priceDesc' | 'newest';

export const MARKETPLACE_GENRES: MarketplaceGenre[] = [
  'fiction',
  'nonfiction',
  'children',
  'science',
  'poetry',
];

export const MARKETPLACE_CONDITIONS: ListingCondition[] = ['new', 'likeNew', 'good', 'acceptable'];
