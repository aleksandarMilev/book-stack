export const ROUTES = {
  home: '/',
  marketplace: '/marketplace',
  listingDetails: '/marketplace/:listingId',
  books: '/books',
  login: '/login',
  register: '/register',
  profile: '/profile',
  myListings: '/my-listings',
  myListingCreate: '/my-listings/new',
  myListingEdit: '/my-listings/:listingId/edit',
  myOrders: '/my-orders',
  sellerSoldOrders: '/seller/sold-orders',
  paymentReturn: '/payment/return',
  adminDashboard: '/admin',
  adminBooksModeration: '/admin/books',
  adminListingsModeration: '/admin/listings',
} as const;

export const getListingDetailsRoute = (listingId: string): string => `${ROUTES.marketplace}/${listingId}`;
export const getMyListingEditRoute = (listingId: string): string => `${ROUTES.myListings}/${listingId}/edit`;
