import { afterEach, describe, expect, it } from 'vitest';

import { paymentSessionStorage } from '@/features/payments/storage/paymentSession.storage';

describe('paymentSessionStorage', () => {
  afterEach(() => {
    window.sessionStorage.clear();
  });

  it('stores and restores guest payment token per order', () => {
    paymentSessionStorage.setGuestPaymentToken('order-1', 'token-1');
    paymentSessionStorage.setGuestPaymentToken('order-2', 'token-2');

    expect(paymentSessionStorage.getGuestPaymentToken('order-1')).toBe('token-1');
    expect(paymentSessionStorage.getGuestPaymentToken('order-2')).toBe('token-2');

    paymentSessionStorage.clearGuestPaymentToken('order-1');
    expect(paymentSessionStorage.getGuestPaymentToken('order-1')).toBeNull();
  });

  it('stores pending order id for payment continuation', () => {
    paymentSessionStorage.setPendingOrderId('order-77');

    expect(paymentSessionStorage.getPendingOrderId()).toBe('order-77');

    paymentSessionStorage.clearPendingOrderId();
    expect(paymentSessionStorage.getPendingOrderId()).toBeNull();
  });
});
