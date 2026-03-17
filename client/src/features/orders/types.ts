import type { MarketplaceListingCondition } from '@/types/marketplace.types';
import type { PriceDisplayValue } from '@/types/pricing.types';

export type PaymentMethod = 'online' | 'cashOnDelivery';

export type OrderStatus =
  | 'pendingPayment'
  | 'pendingConfirmation'
  | 'confirmed'
  | 'shipped'
  | 'delivered'
  | 'completed'
  | 'cancelled'
  | 'expired';

export type PaymentStatus = 'pending' | 'paid' | 'failed' | 'refunded' | 'notRequired' | 'expired' | 'cancelled';

export type SettlementStatus = 'pending' | 'settled' | 'waived' | 'disputed';

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
  paymentMethod: PaymentMethod;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  settlementStatus: SettlementStatus;
  platformFeePercent: number;
  platformFeeAmount: PriceDisplayValue;
  sellerNetAmount: PriceDisplayValue;
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
  paymentMethod: PaymentMethod;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  settlementStatus: SettlementStatus;
  platformFeePercent: number;
  platformFeeAmount: PriceDisplayValue;
  sellerNetAmount: PriceDisplayValue;
  createdOn: string;
  items: OrderItem[];
}
