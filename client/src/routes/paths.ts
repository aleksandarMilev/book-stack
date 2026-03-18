export const ROUTES = {
  home: '/',
  marketplace: '/marketplace',
  listingDetails: '/marketplace/:listingId',
  checkout: '/checkout',
  mockPaymentCheckout: '/payments/mock/checkout',
  login: '/login',
  register: '/register',
  forgotPassword: '/identity/forgot-password',
  resetPassword: '/identity/reset-password',
  profile: '/profile',
  sellerProfile: '/seller/profile',
  myListings: '/my-listings',
  myListingCreate: '/my-listings/new',
  myListingEdit: '/my-listings/:listingId/edit',
  myOrders: '/my-orders',
  sellerSoldOrders: '/seller/sold-orders',
  orderConfirmation: '/order/confirmation',
  paymentReturn: '/payment/return',
  adminDashboard: '/admin',
  adminBooksModeration: '/admin/books',
  adminListingsModeration: '/admin/listings',
} as const;

export const getListingDetailsRoute = (listingId: string): string =>
  `${ROUTES.marketplace}/${listingId}`;
export const getMyListingEditRoute = (listingId: string): string =>
  `${ROUTES.myListings}/${listingId}/edit`;

export const getCheckoutRoute = (listingId: string, quantity = 1): string => {
  const query = new URLSearchParams({
    listingId,
    quantity: String(quantity),
  });

  return `${ROUTES.checkout}?${query.toString()}`;
};

export const getOrderConfirmationRoute = (orderId: string, paymentMethod: 'cashOnDelivery' | 'online'): string => {
  const query = new URLSearchParams({
    orderId,
    paymentMethod,
  });

  return `${ROUTES.orderConfirmation}?${query.toString()}`;
};
