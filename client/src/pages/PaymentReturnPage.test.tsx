import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { checkoutService } from '@/features/checkout/services/checkout.service';
import { ordersApi } from '@/features/orders/api/orders.api';
import { PaymentReturnPage } from '@/pages/PaymentReturnPage';
import { useAuthStore } from '@/store/auth.store';
import type { AuthSession } from '@/types/auth.types';
import { redirectTo } from '@/utils/navigation';

vi.mock('@/features/checkout/services/checkout.service', () => ({
  checkoutService: {
    createOrderAndStartCheckout: vi.fn(),
    startCheckoutForOrder: vi.fn(),
  },
}));

vi.mock('@/features/orders/api/orders.api', () => ({
  ordersApi: {
    createOrder: vi.fn(),
    getMyOrders: vi.fn(),
    getMyOrderDetails: vi.fn(),
    getSoldOrders: vi.fn(),
    getSoldOrderDetails: vi.fn(),
  },
}));

vi.mock('@/utils/navigation', () => ({
  redirectTo: vi.fn(),
}));

const renderPaymentReturnRoute = (initialPath: string) =>
  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={<PaymentReturnPage />} path="/payment/return" />
      </Routes>
    </MemoryRouter>,
  );

const createSession = (role: AuthSession['user']['role']): AuthSession => ({
  accessToken: 'token',
  expiresAtUtc: new Date(Date.now() + 1000 * 60 * 60).toISOString(),
  user: {
    id: 'user-1',
    role,
    displayName: 'John Reader',
    email: 'john@example.com',
  },
});

describe('PaymentReturnPage', () => {
  afterEach(() => {
    useAuthStore.setState({ session: null });
    vi.clearAllMocks();
  });

  it('renders failed return outcome and allows retry', async () => {
    vi.mocked(checkoutService.startCheckoutForOrder).mockResolvedValue({
      orderId: 'order-101',
      paymentMethod: 'online',
      checkoutUrl: '/payments/mock/checkout?sessionId=retry-101',
    });

    renderPaymentReturnRoute('/payment/return?orderId=order-101&status=failed');

    expect(await screen.findByText('Payment failed')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry payment' })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Retry payment' }));

    expect(checkoutService.startCheckoutForOrder).toHaveBeenCalledWith('order-101', false);
    expect(redirectTo).toHaveBeenCalledWith('/payments/mock/checkout?sessionId=retry-101');
  });

  it('uses authenticated order status when available', async () => {
    useAuthStore.setState({ session: createSession('buyer') });
    vi.mocked(ordersApi.getMyOrderDetails).mockResolvedValue({
      id: 'order-202',
      buyerId: 'user-1',
      customerFirstName: 'John',
      customerLastName: 'Reader',
      email: 'john@example.com',
      phoneNumber: null,
      country: 'Bulgaria',
      city: 'Sofia',
      addressLine: '1 Vitosha Blvd',
      postalCode: '1000',
      total: { primary: { amount: 30, currency: 'EUR' } },
      paymentMethod: 'online',
      status: 'confirmed',
      paymentStatus: 'paid',
      settlementStatus: 'pending',
      platformFeePercent: 10,
      platformFeeAmount: { primary: { amount: 3, currency: 'EUR' } },
      sellerNetAmount: { primary: { amount: 27, currency: 'EUR' } },
      createdOn: '2026-01-01T10:00:00Z',
      items: [],
    });

    renderPaymentReturnRoute('/payment/return?orderId=order-202&status=processing');

    expect(await screen.findByText('Payment successful')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'View my orders' })).toBeInTheDocument();
  });
});
