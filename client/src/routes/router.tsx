import { createBrowserRouter } from 'react-router-dom';

import { isMockPaymentUiEnabled } from '@/features/payments/config/paymentProvider';
import { AppShell } from '@/layouts/AppShell';
import { AdminBooksModerationPage } from '@/pages/admin/AdminBooksModerationPage';
import { AdminDashboardPage } from '@/pages/admin/AdminDashboardPage';
import { AdminListingsModerationPage } from '@/pages/admin/AdminListingsModerationPage';
import { BooksPage } from '@/pages/BooksPage';
import { CheckoutPage } from '@/pages/CheckoutPage';
import { HomePage } from '@/pages/HomePage';
import { ListingDetailsPage } from '@/pages/ListingDetailsPage';
import { LoginPage } from '@/pages/LoginPage';
import { MarketplacePage } from '@/pages/MarketplacePage';
import { MockPaymentCheckoutPage } from '@/pages/MockPaymentCheckoutPage';
import { MyListingCreatePage } from '@/pages/MyListingCreatePage';
import { MyListingEditPage } from '@/pages/MyListingEditPage';
import { MyListingsPage } from '@/pages/MyListingsPage';
import { MyOrdersPage } from '@/pages/MyOrdersPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { OrderConfirmationPage } from '@/pages/OrderConfirmationPage';
import { PaymentReturnPage } from '@/pages/PaymentReturnPage';
import { ProfilePage } from '@/pages/ProfilePage';
import { RegisterPage } from '@/pages/RegisterPage';
import { RouterErrorPage } from '@/pages/RouterErrorPage';
import { SellerProfilePage } from '@/pages/SellerProfilePage';
import { SellerSoldOrdersPage } from '@/pages/SellerSoldOrdersPage';
import { GuardedRouteOutlet } from '@/routes/guards/GuardedRouteOutlet';
import { ROUTES } from '@/routes/paths';

export const appRouter = createBrowserRouter([
  {
    path: ROUTES.home,
    element: <AppShell />,
    errorElement: <RouterErrorPage />,
    children: [
      { index: true, element: <HomePage /> },
      { path: ROUTES.marketplace, element: <MarketplacePage /> },
      { path: ROUTES.listingDetails, element: <ListingDetailsPage /> },
      { path: ROUTES.books, element: <BooksPage /> },
      { path: ROUTES.checkout, element: <CheckoutPage /> },
      { path: ROUTES.orderConfirmation, element: <OrderConfirmationPage /> },
      ...(isMockPaymentUiEnabled
        ? [{ path: ROUTES.mockPaymentCheckout, element: <MockPaymentCheckoutPage /> }]
        : []),
      { path: ROUTES.login, element: <LoginPage /> },
      { path: ROUTES.register, element: <RegisterPage /> },
      { path: ROUTES.paymentReturn, element: <PaymentReturnPage /> },
      {
        element: <GuardedRouteOutlet level="authenticated" />,
        children: [
          { path: ROUTES.profile, element: <ProfilePage /> },
          { path: ROUTES.sellerProfile, element: <SellerProfilePage /> },
          { path: ROUTES.myOrders, element: <MyOrdersPage /> },
        ],
      },
      {
        element: <GuardedRouteOutlet level="seller" />,
        children: [
          { path: ROUTES.myListings, element: <MyListingsPage /> },
          { path: ROUTES.myListingCreate, element: <MyListingCreatePage /> },
          { path: ROUTES.myListingEdit, element: <MyListingEditPage /> },
          { path: ROUTES.sellerSoldOrders, element: <SellerSoldOrdersPage /> },
        ],
      },
      {
        element: <GuardedRouteOutlet level="admin" />,
        children: [
          { path: ROUTES.adminDashboard, element: <AdminDashboardPage /> },
          { path: ROUTES.adminBooksModeration, element: <AdminBooksModerationPage /> },
          { path: ROUTES.adminListingsModeration, element: <AdminListingsModerationPage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
