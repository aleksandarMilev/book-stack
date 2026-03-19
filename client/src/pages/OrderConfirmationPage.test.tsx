import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';

import { OrderConfirmationPage } from '@/pages/OrderConfirmationPage';
import { useAuthStore } from '@/store/auth.store';
import type { AuthSession } from '@/types/auth.types';

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

describe('OrderConfirmationPage', () => {
  afterEach(() => {
    useAuthStore.setState({ session: null });
  });

  it('renders COD confirmation messaging', async () => {
    render(
      <MemoryRouter initialEntries={['/order/confirmation?orderId=order-1&paymentMethod=cashOnDelivery']}>
        <Routes>
          <Route element={<OrderConfirmationPage />} path="/order/confirmation" />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText('Order submitted successfully')).toBeInTheDocument();
    expect(screen.getByText(/pay on delivery/i)).toBeInTheDocument();
    expect(screen.getByText('Confirmation summary')).toBeInTheDocument();
    expect(screen.getByText('Order ID')).toBeInTheDocument();
    expect(screen.getByText('order-1')).toBeInTheDocument();
    expect(screen.getByText('Cash on delivery')).toBeInTheDocument();
    expect(screen.getByText('Pending confirmation')).toBeInTheDocument();
    expect(
      screen.getByText('Your checkout details are saved and you can continue with confidence.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Back to marketplace' })).toHaveClass('ui-button--primary');
  });

  it('keeps secondary marketplace action when authenticated', async () => {
    useAuthStore.setState({ session: createSession('buyer') });

    render(
      <MemoryRouter initialEntries={['/order/confirmation?orderId=order-2&paymentMethod=cashOnDelivery']}>
        <Routes>
          <Route element={<OrderConfirmationPage />} path="/order/confirmation" />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByRole('button', { name: 'View my orders' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Back to marketplace' })).toHaveClass('ui-button--secondary');
  });
});
