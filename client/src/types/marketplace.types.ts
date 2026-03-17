import type { PriceDisplayValue } from '@/types/pricing.types';

export type MarketplaceListingCondition = 'new' | 'likeNew' | 'veryGood' | 'good' | 'acceptable' | 'poor';

export interface MarketplaceListing {
  id: string;
  bookId: string;
  title: string;
  author: string;
  genre: string;
  publisher?: string | null;
  publishedOn?: string | null;
  isbn?: string | null;
  creatorId: string;
  supportsOnlinePayment: boolean;
  supportsCashOnDelivery: boolean;
  condition: MarketplaceListingCondition;
  quantity: number;
  description: string;
  imageUrl: string;
  isApproved: boolean;
  rejectionReason?: string | null;
  createdOn: string;
  modifiedOn?: string | null;
  price: PriceDisplayValue;
}
