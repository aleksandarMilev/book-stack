import { httpClient } from '@/api/httpClient';

export type PaymentRecordStatus = 'pending' | 'processing' | 'succeeded' | 'failed' | 'refunded' | 'canceled';

export interface CreatePaymentSessionRequest {
  provider?: string;
  paymentToken?: string;
}

interface PaymentCheckoutSessionApiModel {
  paymentId: string;
  orderId: string;
  provider: string;
  providerPaymentId: string;
  checkoutUrl: string;
  status: number | string;
}

export interface PaymentCheckoutSession {
  paymentId: string;
  orderId: string;
  provider: string;
  providerPaymentId: string;
  checkoutUrl: string;
  status: PaymentRecordStatus;
}

interface MockWebhookPayload {
  eventId: string;
  paymentSessionId: string;
  status: string;
  failureReason?: string;
  occurredOnUtc?: string;
}

const PAYMENTS_BASE_PATH = '/Payments';

const toPaymentRecordStatus = (status: number | string): PaymentRecordStatus => {
  if (typeof status === 'number') {
    if (status === 1) {
      return 'processing';
    }

    if (status === 2) {
      return 'succeeded';
    }

    if (status === 3) {
      return 'failed';
    }

    if (status === 4) {
      return 'refunded';
    }

    if (status === 5) {
      return 'canceled';
    }

    return 'pending';
  }

  const normalizedStatus = status.trim().toLowerCase();
  if (normalizedStatus === 'processing') {
    return 'processing';
  }

  if (normalizedStatus === 'succeeded' || normalizedStatus === 'success') {
    return 'succeeded';
  }

  if (normalizedStatus === 'failed') {
    return 'failed';
  }

  if (normalizedStatus === 'refunded') {
    return 'refunded';
  }

  if (normalizedStatus === 'canceled' || normalizedStatus === 'cancelled') {
    return 'canceled';
  }

  return 'pending';
};

const mapPaymentSession = (session: PaymentCheckoutSessionApiModel): PaymentCheckoutSession => ({
  paymentId: session.paymentId,
  orderId: session.orderId,
  provider: session.provider,
  providerPaymentId: session.providerPaymentId,
  checkoutUrl: session.checkoutUrl,
  status: toPaymentRecordStatus(session.status),
});

export const paymentsApi = {
  async createCheckoutSession(
    orderId: string,
    payload?: CreatePaymentSessionRequest,
  ): Promise<PaymentCheckoutSession> {
    const response = await httpClient.post<PaymentCheckoutSessionApiModel>(
      `${PAYMENTS_BASE_PATH}/checkout/${orderId}/`,
      payload ?? {},
    );

    return mapPaymentSession(response.data);
  },

  async sendMockWebhook(payload: MockWebhookPayload): Promise<void> {
    await httpClient.post(`${PAYMENTS_BASE_PATH}/webhook/mock/`, payload, {
      headers: {
        'Content-Type': 'application/json',
      },
    });
  },
};
