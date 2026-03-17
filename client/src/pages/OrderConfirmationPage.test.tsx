import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it } from 'vitest';

import { OrderConfirmationPage } from '@/pages/OrderConfirmationPage';
import { useAuthStore } from '@/store/auth.store';

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
    expect(screen.getByText('Order ID: order-1')).toBeInTheDocument();
  });
});
