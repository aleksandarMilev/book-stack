import { afterEach, describe, expect, it, vi } from 'vitest';

import { checkoutService } from '@/features/checkout/services/checkout.service';
import { ordersApi } from '@/features/orders/api/orders.api';
import { paymentsApi } from '@/features/payments/api/payments.api';
import { paymentSessionStorage } from '@/features/payments/storage/paymentSession.storage';

vi.mock('@/features/orders/api/orders.api', () => ({
  ordersApi: {
    createOrder: vi.fn(),
    getMyOrders: vi.fn(),
    getMyOrderDetails: vi.fn(),
    getSoldOrders: vi.fn(),
    getSoldOrderDetails: vi.fn(),
  },
}));

vi.mock('@/features/payments/api/payments.api', () => ({
  paymentsApi: {
    createCheckoutSession: vi.fn(),
    sendMockWebhook: vi.fn(),
  },
}));

vi.mock('@/features/payments/storage/paymentSession.storage', () => ({
  paymentSessionStorage: {
    setGuestPaymentToken: vi.fn(),
    getGuestPaymentToken: vi.fn(),
    clearGuestPaymentToken: vi.fn(),
    setPendingOrderId: vi.fn(),
    getPendingOrderId: vi.fn(),
    clearPendingOrderId: vi.fn(),
  },
}));

describe('checkoutService', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('stores guest token and uses it for payment session creation', async () => {
    vi.mocked(ordersApi.createOrder).mockResolvedValue({
      orderId: 'order-1',
      paymentToken: 'guest-token-1',
    });
    vi.mocked(paymentsApi.createCheckoutSession).mockResolvedValue({
      paymentId: 'payment-1',
      orderId: 'order-1',
      provider: 'mock',
      providerPaymentId: 'session-1',
      checkoutUrl: '/payments/mock/checkout?sessionId=session-1',
      status: 'pending',
    });

    const result = await checkoutService.createOrderAndStartCheckout(
      {
        customerFirstName: 'John',
        customerLastName: 'Doe',
        email: 'john@example.com',
        country: 'Bulgaria',
        city: 'Sofia',
        addressLine: '1 Vitosha Blvd',
        items: [{ listingId: 'listing-1', quantity: 1 }],
      },
      { provider: 'mock' },
    );

    expect(paymentSessionStorage.setGuestPaymentToken).toHaveBeenCalledWith('order-1', 'guest-token-1');
    expect(paymentsApi.createCheckoutSession).toHaveBeenCalledWith('order-1', {
      provider: 'mock',
      paymentToken: 'guest-token-1',
    });
    expect(result.checkoutUrl).toContain('session-1');
  });

  it('starts payment for authenticated users without guest token', async () => {
    vi.mocked(paymentsApi.createCheckoutSession).mockResolvedValue({
      paymentId: 'payment-2',
      orderId: 'order-2',
      provider: 'mock',
      providerPaymentId: 'session-2',
      checkoutUrl: '/payments/mock/checkout?sessionId=session-2',
      status: 'pending',
    });

    await checkoutService.startCheckoutForOrder('order-2', true, { provider: 'mock' });

    expect(paymentsApi.createCheckoutSession).toHaveBeenCalledWith('order-2', { provider: 'mock' });
  });

  it('uses guest token when retrying checkout as guest', async () => {
    vi.mocked(paymentSessionStorage.getGuestPaymentToken).mockReturnValue('stored-guest-token');
    vi.mocked(paymentsApi.createCheckoutSession).mockResolvedValue({
      paymentId: 'payment-3',
      orderId: 'order-3',
      provider: 'mock',
      providerPaymentId: 'session-3',
      checkoutUrl: '/payments/mock/checkout?sessionId=session-3',
      status: 'pending',
    });

    await checkoutService.startCheckoutForOrder('order-3', false, { provider: 'mock' });

    expect(paymentsApi.createCheckoutSession).toHaveBeenCalledWith('order-3', {
      provider: 'mock',
      paymentToken: 'stored-guest-token',
    });
  });
});
