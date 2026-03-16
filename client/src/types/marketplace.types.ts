import type { PriceDisplayValue } from '@/types/pricing.types';

export type MarketplaceGenre = 'fiction' | 'nonfiction' | 'children' | 'science' | 'poetry';

export type ListingCondition = 'new' | 'likeNew' | 'good' | 'acceptable';

export interface MarketplaceListing {
  id: string;
  titleKey: string;
  authorKey: string;
  cityKey: string;
  genre: MarketplaceGenre;
  condition: ListingCondition;
  price: PriceDisplayValue;
}
