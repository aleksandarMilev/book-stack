import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { profileApi } from '@/features/auth/api/profile.api';
import { checkoutService } from '@/features/checkout/services/checkout.service';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import { CheckoutPage } from '@/pages/CheckoutPage';
import { useAuthStore } from '@/store/auth.store';
import type { AuthSession } from '@/types/auth.types';
import { redirectTo } from '@/utils/navigation';

vi.mock('@/features/marketplace/api/listings.api', () => ({
  listingsApi: {
    getListings: vi.fn(),
    getMineListings: vi.fn(),
    getListingById: vi.fn(),
    deleteListing: vi.fn(),
  },
}));

vi.mock('@/features/checkout/services/checkout.service', () => ({
  checkoutService: {
    createOrderAndStartCheckout: vi.fn(),
    startCheckoutForOrder: vi.fn(),
  },
}));

vi.mock('@/utils/navigation', () => ({
  redirectTo: vi.fn(),
}));

vi.mock('@/features/auth/api/profile.api', () => ({
  profileApi: {
    getMine: vi.fn(),
    updateMine: vi.fn(),
  },
}));

const renderCheckoutRoute = (initialPath: string) =>
  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <Routes>
        <Route element={<CheckoutPage />} path="/checkout" />
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

describe('CheckoutPage', () => {
  afterEach(() => {
    useAuthStore.setState({ session: null });
    vi.clearAllMocks();
  });

  it('renders invalid checkout state when listing query params are missing', async () => {
    renderCheckoutRoute('/checkout');

    expect(await screen.findByText('Checkout selection is invalid')).toBeInTheDocument();
  });

  it('shows validation errors before submitting', async () => {
    vi.mocked(listingsApi.getListingById).mockResolvedValue({
      id: 'listing-1',
      bookId: 'book-1',
      title: 'Checkout Listing',
      author: 'Author',
      genre: 'Fiction',
      creatorId: 'seller-1',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      condition: 'good',
      quantity: 3,
      description: 'desc',
      imageUrl: '',
      isApproved: true,
      rejectionReason: null,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
      price: { primary: { amount: 12.5, currency: 'EUR' } },
    });

    renderCheckoutRoute('/checkout?listingId=listing-1&quantity=1');

    expect(await screen.findByText('Order summary')).toBeInTheDocument();
    expect(screen.getByText('Payment method')).toBeInTheDocument();
    expect(screen.getByText('Guest checkout is available. Sign in if you want order history in your account.')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Create order and continue' }));

    expect(await screen.findByText('First name is required.')).toBeInTheDocument();
    expect(await screen.findByText('Last name is required.')).toBeInTheDocument();
    expect(await screen.findByText('Email is required.')).toBeInTheDocument();
  });

  it('shows both payment methods when seller supports both', async () => {
    vi.mocked(listingsApi.getListingById).mockResolvedValue({
      id: 'listing-both-1',
      bookId: 'book-both-1',
      title: 'Both Methods Listing',
      author: 'Author',
      genre: 'Fiction',
      creatorId: 'seller-1',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      condition: 'good',
      quantity: 3,
      description: 'desc',
      imageUrl: '',
      isApproved: true,
      rejectionReason: null,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
      price: { primary: { amount: 12.5, currency: 'EUR' } },
    });

    renderCheckoutRoute('/checkout?listingId=listing-both-1&quantity=1');

    await screen.findByText('Order summary');

    const onlineRadio = screen.getByDisplayValue('online');
    const cashOnDeliveryRadio = screen.getByDisplayValue('cashOnDelivery');

    expect(onlineRadio).toBeInTheDocument();
    expect(cashOnDeliveryRadio).toBeInTheDocument();
    expect(document.querySelectorAll('.checkout-payment-option--selected')).toHaveLength(1);
    expect((onlineRadio as HTMLInputElement).checked || (cashOnDeliveryRadio as HTMLInputElement).checked).toBe(true);
    expect(document.querySelectorAll('.checkout-reassurance-pill')).toHaveLength(2);
  });

  it('prefills known user data for authenticated checkout', async () => {
    useAuthStore.setState({ session: createSession('buyer') });
    vi.mocked(listingsApi.getListingById).mockResolvedValue({
      id: 'listing-2',
      bookId: 'book-2',
      title: 'Another Listing',
      author: 'Author',
      genre: 'Fiction',
      creatorId: 'seller-1',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      condition: 'new',
      quantity: 2,
      description: 'desc',
      imageUrl: '',
      isApproved: true,
      rejectionReason: null,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
      price: { primary: { amount: 20, currency: 'EUR' } },
    });
    vi.mocked(profileApi.getMine).mockResolvedValue({
      id: 'user-1',
      firstName: 'John',
      lastName: 'Reader',
      imagePath: '',
      imageUrl: '',
    });

    renderCheckoutRoute('/checkout?listingId=listing-2&quantity=1');

    await waitFor(() => {
      expect(screen.getByLabelText('First name')).toHaveValue('John');
    });
    expect(screen.getByLabelText('Last name')).toHaveValue('Reader');
    expect(screen.getByLabelText('Email')).toHaveValue('john@example.com');
  });

  it('shows submit error when order creation/payment start fails', async () => {
    vi.mocked(listingsApi.getListingById).mockResolvedValue({
      id: 'listing-3',
      bookId: 'book-3',
      title: 'Error Listing',
      author: 'Author',
      genre: 'Fiction',
      creatorId: 'seller-1',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      condition: 'good',
      quantity: 2,
      description: 'desc',
      imageUrl: '',
      isApproved: true,
      rejectionReason: null,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
      price: { primary: { amount: 10, currency: 'EUR' } },
    });
    vi.mocked(checkoutService.createOrderAndStartCheckout).mockRejectedValue(new Error('boom'));

    renderCheckoutRoute('/checkout?listingId=listing-3&quantity=1');

    expect(await screen.findByText('Order summary')).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText('First name'), 'John');
    await userEvent.type(screen.getByLabelText('Last name'), 'Doe');
    await userEvent.type(screen.getByLabelText('Email'), 'john@example.com');
    await userEvent.type(screen.getByLabelText('Country'), 'Bulgaria');
    await userEvent.type(screen.getByLabelText('City'), 'Sofia');
    await userEvent.type(screen.getByLabelText('Address line'), '1 Vitosha Blvd');

    await userEvent.click(screen.getByRole('button', { name: 'Create order and continue' }));

    expect(
      await screen.findByText('Could not create your order. Please review details and try again.'),
    ).toBeInTheDocument();
  });

  it('handles missing payment-support data safely', async () => {
    vi.mocked(listingsApi.getListingById).mockResolvedValue({
      id: 'listing-invalid-support',
      bookId: 'book-invalid-support',
      title: 'Invalid Support Listing',
      author: 'Author',
      genre: 'Fiction',
      creatorId: 'seller-1',
      supportsOnlinePayment: undefined as unknown as boolean,
      supportsCashOnDelivery: undefined as unknown as boolean,
      condition: 'good',
      quantity: 2,
      description: 'desc',
      imageUrl: '',
      isApproved: true,
      rejectionReason: null,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
      price: { primary: { amount: 10, currency: 'EUR' } },
    });

    renderCheckoutRoute('/checkout?listingId=listing-invalid-support&quantity=1');

    await screen.findByText('Order summary');
    expect(
      screen.getByText(
        'Seller payment-method configuration is unavailable for this listing. Please try again later.',
      ),
    ).toBeInTheDocument();
    expect(screen.queryByDisplayValue('online')).not.toBeInTheDocument();
    expect(screen.queryByDisplayValue('cashOnDelivery')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create order and continue' })).toBeDisabled();
  });

  it('submits online checkout and redirects to payment provider', async () => {
    vi.mocked(listingsApi.getListingById).mockResolvedValue({
      id: 'listing-4',
      bookId: 'book-4',
      title: 'Online Listing',
      author: 'Author',
      genre: 'Fiction',
      creatorId: 'seller-1',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: false,
      condition: 'good',
      quantity: 2,
      description: 'desc',
      imageUrl: '',
      isApproved: true,
      rejectionReason: null,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
      price: { primary: { amount: 10, currency: 'EUR' } },
    });
    vi.mocked(checkoutService.createOrderAndStartCheckout).mockResolvedValue({
      orderId: 'order-4',
      paymentMethod: 'online',
      checkoutUrl: '/payments/mock/checkout?sessionId=abc',
    });

    renderCheckoutRoute('/checkout?listingId=listing-4&quantity=1');

    await screen.findByText('Order summary');
    const onlineRadio = screen.getByDisplayValue('online');

    if (!(onlineRadio as HTMLInputElement).checked) {
      await userEvent.click(onlineRadio);
    }

    expect(onlineRadio).toBeChecked();
    expect(screen.queryByDisplayValue('cashOnDelivery')).not.toBeInTheDocument();

    await userEvent.type(screen.getByLabelText('First name'), 'John');
    await userEvent.type(screen.getByLabelText('Last name'), 'Doe');
    await userEvent.type(screen.getByLabelText('Email'), 'john@example.com');
    await userEvent.type(screen.getByLabelText('Country'), 'Bulgaria');
    await userEvent.type(screen.getByLabelText('City'), 'Sofia');
    await userEvent.type(screen.getByLabelText('Address line'), '1 Vitosha Blvd');

    await userEvent.click(screen.getByRole('button', { name: 'Create order and continue' }));

    expect(checkoutService.createOrderAndStartCheckout).toHaveBeenCalledWith(
      expect.objectContaining({
        paymentMethod: 'online',
      }),
    );
    expect(redirectTo).toHaveBeenCalledWith('/payments/mock/checkout?sessionId=abc');
  });

  it('submits COD checkout and redirects to order confirmation', async () => {
    vi.mocked(listingsApi.getListingById).mockResolvedValue({
      id: 'listing-5',
      bookId: 'book-5',
      title: 'COD Listing',
      author: 'Author',
      genre: 'Fiction',
      creatorId: 'seller-1',
      supportsOnlinePayment: false,
      supportsCashOnDelivery: true,
      condition: 'good',
      quantity: 2,
      description: 'desc',
      imageUrl: '',
      isApproved: true,
      rejectionReason: null,
      createdOn: '2026-01-01T10:00:00Z',
      modifiedOn: null,
      price: { primary: { amount: 10, currency: 'EUR' } },
    });
    vi.mocked(checkoutService.createOrderAndStartCheckout).mockResolvedValue({
      orderId: 'order-cod-5',
      paymentMethod: 'cashOnDelivery',
    });

    renderCheckoutRoute('/checkout?listingId=listing-5&quantity=1');

    await screen.findByText('Order summary');
    const cashOnDeliveryRadio = screen.getByDisplayValue('cashOnDelivery');

    if (!(cashOnDeliveryRadio as HTMLInputElement).checked) {
      await userEvent.click(cashOnDeliveryRadio);
    }

    expect(cashOnDeliveryRadio).toBeChecked();
    expect(screen.queryByDisplayValue('online')).not.toBeInTheDocument();

    await userEvent.type(screen.getByLabelText('First name'), 'Maria');
    await userEvent.type(screen.getByLabelText('Last name'), 'Ivanova');
    await userEvent.type(screen.getByLabelText('Email'), 'maria@example.com');
    await userEvent.type(screen.getByLabelText('Country'), 'Bulgaria');
    await userEvent.type(screen.getByLabelText('City'), 'Sofia');
    await userEvent.type(screen.getByLabelText('Address line'), '2 Vitosha Blvd');

    await userEvent.click(screen.getByRole('button', { name: 'Create order and continue' }));

    expect(checkoutService.createOrderAndStartCheckout).toHaveBeenCalledWith(
      expect.objectContaining({
        paymentMethod: 'cashOnDelivery',
      }),
    );
    expect(redirectTo).toHaveBeenCalledWith('/order/confirmation?orderId=order-cod-5&paymentMethod=cashOnDelivery');
  });
});
