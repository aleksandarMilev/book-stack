import type { MarketplaceListingCondition } from '@/types/marketplace.types';

export type MarketplaceSortOption =
  | 'newest'
  | 'oldest'
  | 'priceAsc'
  | 'priceDesc'
  | 'titleAsc'
  | 'titleDesc';

export const MARKETPLACE_SORT_TO_BACKEND: Record<MarketplaceSortOption, number> = {
  newest: 0,
  oldest: 1,
  priceAsc: 2,
  priceDesc: 3,
  titleAsc: 4,
  titleDesc: 5,
};

export const MARKETPLACE_CONDITIONS: MarketplaceListingCondition[] = [
  'new',
  'likeNew',
  'veryGood',
  'good',
  'acceptable',
  'poor',
];

export const MARKETPLACE_CONDITION_TO_BACKEND: Record<MarketplaceListingCondition, number> = {
  new: 0,
  likeNew: 1,
  veryGood: 2,
  good: 3,
  acceptable: 4,
  poor: 5,
};
