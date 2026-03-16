import type { MarketplaceListingCondition } from '@/types/marketplace.types';
import type { PriceDisplayValue } from '@/types/pricing.types';

export type OrderStatus = 'pendingPayment' | 'confirmed' | 'cancelled' | 'completed' | 'expired';
export type PaymentStatus = 'unpaid' | 'paid' | 'failed' | 'refunded';

export interface OrderItem {
  id: string;
  listingId: string;
  bookId: string;
  sellerId?: string;
  bookTitle: string;
  bookAuthor: string;
  bookGenre: string;
  bookPublisher?: string | null;
  bookPublishedOn?: string | null;
  bookIsbn?: string | null;
  unitPrice: PriceDisplayValue;
  quantity: number;
  totalPrice: PriceDisplayValue;
  condition: MarketplaceListingCondition;
  listingDescription: string;
  listingImageUrl: string;
}

export interface UserOrder {
  id: string;
  buyerId?: string | null;
  customerFirstName: string;
  customerLastName: string;
  email: string;
  phoneNumber?: string | null;
  country: string;
  city: string;
  addressLine: string;
  postalCode?: string | null;
  total: PriceDisplayValue;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  createdOn: string;
  items: OrderItem[];
}

export interface SellerOrder {
  id: string;
  customerFirstName: string;
  customerLastName: string;
  email: string;
  phoneNumber?: string | null;
  country: string;
  city: string;
  addressLine: string;
  postalCode?: string | null;
  sellerTotal: PriceDisplayValue;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  createdOn: string;
  items: OrderItem[];
}
