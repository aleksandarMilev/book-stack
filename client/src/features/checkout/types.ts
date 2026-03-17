import type { PaymentMethod } from '@/features/orders/types';
import type { MarketplaceListing } from '@/types/marketplace.types';

export interface CheckoutContactInput {
  customerFirstName: string;
  customerLastName: string;
  email: string;
  phoneNumber?: string | null;
  country: string;
  city: string;
  addressLine: string;
  postalCode?: string | null;
}

export interface CheckoutItemInput {
  listingId: string;
  quantity: number;
}

export interface CheckoutSubmissionInput extends CheckoutContactInput {
  paymentMethod: PaymentMethod;
  items: CheckoutItemInput[];
}

export interface CheckoutQueryState {
  listingId: string;
  quantity: number;
}

export interface CheckoutSelection {
  listing: MarketplaceListing;
  quantity: number;
}
