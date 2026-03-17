import type { CreateOrderRequest } from '@/features/orders/api/orders.api';
import { ordersApi } from '@/features/orders/api/orders.api';
import { paymentsApi } from '@/features/payments/api/payments.api';
import { appPaymentProvider } from '@/features/payments/config/paymentProvider';
import { paymentSessionStorage } from '@/features/payments/storage/paymentSession.storage';

interface StartCheckoutOptions {
  provider?: string;
}

interface StartCheckoutResult {
  orderId: string;
  checkoutUrl: string;
}

const normalizeProvider = (provider: string | undefined): string | undefined => {
  if (!provider) {
    return undefined;
  }

  const normalizedProvider = provider.trim();
  return normalizedProvider ? normalizedProvider : undefined;
};

export const checkoutService = {
  async createOrderAndStartCheckout(
    payload: CreateOrderRequest,
    options?: StartCheckoutOptions,
  ): Promise<StartCheckoutResult> {
    const provider = normalizeProvider(options?.provider) ?? appPaymentProvider;
    const createdOrder = await ordersApi.createOrder(payload);

    if (createdOrder.paymentToken) {
      paymentSessionStorage.setGuestPaymentToken(createdOrder.orderId, createdOrder.paymentToken);
    }

    paymentSessionStorage.setPendingOrderId(createdOrder.orderId);

    const paymentSession = await paymentsApi.createCheckoutSession(createdOrder.orderId, {
      ...(provider ? { provider } : {}),
      ...(createdOrder.paymentToken ? { paymentToken: createdOrder.paymentToken } : {}),
    });

    return {
      orderId: createdOrder.orderId,
      checkoutUrl: paymentSession.checkoutUrl,
    };
  },

  async startCheckoutForOrder(
    orderId: string,
    isAuthenticated: boolean,
    options?: StartCheckoutOptions,
  ): Promise<StartCheckoutResult> {
    const provider = normalizeProvider(options?.provider) ?? appPaymentProvider;
    const guestPaymentToken = !isAuthenticated ? paymentSessionStorage.getGuestPaymentToken(orderId) : null;

    const paymentSession = await paymentsApi.createCheckoutSession(orderId, {
      ...(provider ? { provider } : {}),
      ...(guestPaymentToken ? { paymentToken: guestPaymentToken } : {}),
    });

    paymentSessionStorage.setPendingOrderId(orderId);

    return {
      orderId,
      checkoutUrl: paymentSession.checkoutUrl,
    };
  },
};
