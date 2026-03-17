import type { CheckoutSubmissionInput } from '@/features/checkout/types';
import type { CreateOrderRequest } from '@/features/orders/api/orders.api';

const normalizeOptional = (value: string | null | undefined): string | undefined => {
  if (!value) {
    return undefined;
  }

  const normalizedValue = value.trim();
  if (!normalizedValue) {
    return undefined;
  }

  return normalizedValue;
};

export const mapCheckoutSubmissionToCreateOrderRequest = (
  payload: CheckoutSubmissionInput,
): CreateOrderRequest => {
  const normalizedPhoneNumber = normalizeOptional(payload.phoneNumber);
  const normalizedPostalCode = normalizeOptional(payload.postalCode);

  return {
    customerFirstName: payload.customerFirstName.trim(),
    customerLastName: payload.customerLastName.trim(),
    email: payload.email.trim(),
    ...(normalizedPhoneNumber ? { phoneNumber: normalizedPhoneNumber } : {}),
    country: payload.country.trim(),
    city: payload.city.trim(),
    addressLine: payload.addressLine.trim(),
    ...(normalizedPostalCode ? { postalCode: normalizedPostalCode } : {}),
    paymentMethod: payload.paymentMethod,
    items: payload.items.map((item) => ({
      listingId: item.listingId,
      quantity: item.quantity,
    })),
  };
};
