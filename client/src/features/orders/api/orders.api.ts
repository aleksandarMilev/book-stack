import { httpClient } from '@/api/httpClient';
import type { PaginatedResponse } from '@/api/types/api.types';
import { resolveAssetUrl } from '@/api/utils/assetUrl';
import type { OrderItem, OrderStatus, PaymentStatus, SellerOrder, UserOrder } from '@/features/orders/types';
import type { MarketplaceListingCondition } from '@/types/marketplace.types';
import type { CurrencyCode, PriceDisplayValue } from '@/types/pricing.types';

interface OrderItemApiModel {
  id: string;
  listingId: string;
  bookId: string;
  sellerId?: string;
  bookTitle: string;
  bookAuthor: string;
  bookGenre: string;
  bookPublisher?: string | null;
  bookPublishedOn?: string | null;
  bookIsbn?: string | null;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  currency: string;
  condition: number | string;
  listingDescription: string;
  listingImagePath: string;
}

interface UserOrderApiModel {
  id: string;
  buyerId?: string | null;
  customerFirstName: string;
  customerLastName: string;
  email: string;
  phoneNumber?: string | null;
  country: string;
  city: string;
  addressLine: string;
  postalCode?: string | null;
  totalAmount: number;
  currency: string;
  status: number | string;
  paymentStatus: number | string;
  createdOn: string;
  items: OrderItemApiModel[];
}

interface SellerOrderApiModel {
  id: string;
  customerFirstName: string;
  customerLastName: string;
  email: string;
  phoneNumber?: string | null;
  country: string;
  city: string;
  addressLine: string;
  postalCode?: string | null;
  sellerTotalAmount: number;
  currency: string;
  status: number | string;
  paymentStatus: number | string;
  createdOn: string;
  items: OrderItemApiModel[];
}

interface OrderBackendFilterQuery {
  SearchTerm?: string | undefined;
  Email?: string | undefined;
  Status?: number | undefined;
  PaymentStatus?: number | undefined;
  PageIndex?: number | undefined;
  PageSize?: number | undefined;
}

export interface OrderFilterQuery {
  searchTerm?: string | undefined;
  email?: string | undefined;
  status?: OrderStatus | undefined;
  paymentStatus?: PaymentStatus | undefined;
  pageIndex?: number | undefined;
  pageSize?: number | undefined;
}

export interface CreateOrderItemRequest {
  listingId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  customerFirstName: string;
  customerLastName: string;
  email: string;
  phoneNumber?: string | undefined;
  country: string;
  city: string;
  addressLine: string;
  postalCode?: string | undefined;
  items: CreateOrderItemRequest[];
}

interface CreateOrderResultApiModel {
  orderId: string;
  paymentToken?: string | null;
}

export interface CreateOrderResult {
  orderId: string;
  paymentToken?: string;
}

const ORDERS_BASE_PATH = '/Orders';

const toCurrencyCode = (currency: string): CurrencyCode => {
  const normalizedCurrency = currency.trim().toUpperCase();
  if (normalizedCurrency === 'EUR') {
    return 'EUR';
  }

  return 'BGN';
};

const toPriceDisplayValue = (amount: number, currency: string): PriceDisplayValue => ({
  primary: {
    amount,
    currency: toCurrencyCode(currency),
  },
});

const toListingCondition = (condition: number | string): MarketplaceListingCondition => {
  if (typeof condition === 'string') {
    const normalizedCondition = condition.toLowerCase();
    if (
      normalizedCondition === 'new' ||
      normalizedCondition === 'likenew' ||
      normalizedCondition === 'verygood' ||
      normalizedCondition === 'good' ||
      normalizedCondition === 'acceptable' ||
      normalizedCondition === 'poor'
    ) {
      return normalizedCondition === 'likenew'
        ? 'likeNew'
        : normalizedCondition === 'verygood'
          ? 'veryGood'
          : (normalizedCondition as MarketplaceListingCondition);
    }
  }

  if (condition === 0) {
    return 'new';
  }

  if (condition === 1) {
    return 'likeNew';
  }

  if (condition === 2) {
    return 'veryGood';
  }

  if (condition === 3) {
    return 'good';
  }

  if (condition === 5) {
    return 'poor';
  }

  return 'acceptable';
};

const toOrderStatus = (status: number | string): OrderStatus => {
  if (typeof status === 'number') {
    if (status === 1) {
      return 'confirmed';
    }

    if (status === 2) {
      return 'cancelled';
    }

    if (status === 3) {
      return 'completed';
    }

    if (status === 4) {
      return 'expired';
    }

    return 'pendingPayment';
  }

  const normalizedStatus = status.trim().toLowerCase();
  if (normalizedStatus === 'pending' || normalizedStatus === 'pendingpayment') {
    return 'pendingPayment';
  }

  if (normalizedStatus === 'confirmed') {
    return 'confirmed';
  }

  if (normalizedStatus === 'cancelled' || normalizedStatus === 'canceled') {
    return 'cancelled';
  }

  if (normalizedStatus === 'completed') {
    return 'completed';
  }

  if (normalizedStatus === 'expired') {
    return 'expired';
  }

  return 'pendingPayment';
};

const toPaymentStatus = (status: number | string): PaymentStatus => {
  if (typeof status === 'number') {
    if (status === 1) {
      return 'paid';
    }

    if (status === 2) {
      return 'failed';
    }

    if (status === 3) {
      return 'refunded';
    }

    return 'unpaid';
  }

  const normalizedStatus = status.trim().toLowerCase();
  if (normalizedStatus === 'paid') {
    return 'paid';
  }

  if (normalizedStatus === 'failed') {
    return 'failed';
  }

  if (normalizedStatus === 'refunded') {
    return 'refunded';
  }

  return 'unpaid';
};

const ORDER_STATUS_TO_BACKEND: Record<OrderStatus, number> = {
  pendingPayment: 0,
  confirmed: 1,
  cancelled: 2,
  completed: 3,
  expired: 4,
};

const PAYMENT_STATUS_TO_BACKEND: Record<PaymentStatus, number> = {
  unpaid: 0,
  paid: 1,
  failed: 2,
  refunded: 3,
};

const removeEmptyQueryValues = <T extends object>(query: T): T => {
  const entries = Object.entries(query as Record<string, unknown>).filter(
    ([, value]) => value !== undefined && value !== null && value !== '',
  );

  return Object.fromEntries(entries) as T;
};

const mapOrderItem = (item: OrderItemApiModel): OrderItem => ({
  id: item.id,
  listingId: item.listingId,
  bookId: item.bookId,
  ...(item.sellerId ? { sellerId: item.sellerId } : {}),
  bookTitle: item.bookTitle,
  bookAuthor: item.bookAuthor,
  bookGenre: item.bookGenre,
  bookPublisher: item.bookPublisher ?? null,
  bookPublishedOn: item.bookPublishedOn ?? null,
  bookIsbn: item.bookIsbn ?? null,
  unitPrice: toPriceDisplayValue(item.unitPrice, item.currency),
  quantity: item.quantity,
  totalPrice: toPriceDisplayValue(item.totalPrice, item.currency),
  condition: toListingCondition(item.condition),
  listingDescription: item.listingDescription,
  listingImageUrl: resolveAssetUrl(item.listingImagePath),
});

const mapUserOrder = (order: UserOrderApiModel): UserOrder => ({
  id: order.id,
  buyerId: order.buyerId ?? null,
  customerFirstName: order.customerFirstName,
  customerLastName: order.customerLastName,
  email: order.email,
  phoneNumber: order.phoneNumber ?? null,
  country: order.country,
  city: order.city,
  addressLine: order.addressLine,
  postalCode: order.postalCode ?? null,
  total: toPriceDisplayValue(order.totalAmount, order.currency),
  status: toOrderStatus(order.status),
  paymentStatus: toPaymentStatus(order.paymentStatus),
  createdOn: order.createdOn,
  items: order.items.map(mapOrderItem),
});

const mapSellerOrder = (order: SellerOrderApiModel): SellerOrder => ({
  id: order.id,
  customerFirstName: order.customerFirstName,
  customerLastName: order.customerLastName,
  email: order.email,
  phoneNumber: order.phoneNumber ?? null,
  country: order.country,
  city: order.city,
  addressLine: order.addressLine,
  postalCode: order.postalCode ?? null,
  sellerTotal: toPriceDisplayValue(order.sellerTotalAmount, order.currency),
  status: toOrderStatus(order.status),
  paymentStatus: toPaymentStatus(order.paymentStatus),
  createdOn: order.createdOn,
  items: order.items.map(mapOrderItem),
});

const toBackendFilterQuery = (query: OrderFilterQuery): OrderBackendFilterQuery => ({
  SearchTerm: query.searchTerm,
  Email: query.email,
  Status: query.status ? ORDER_STATUS_TO_BACKEND[query.status] : undefined,
  PaymentStatus: query.paymentStatus ? PAYMENT_STATUS_TO_BACKEND[query.paymentStatus] : undefined,
  PageIndex: query.pageIndex,
  PageSize: query.pageSize,
});

export const ordersApi = {
  async createOrder(payload: CreateOrderRequest): Promise<CreateOrderResult> {
    const response = await httpClient.post<CreateOrderResultApiModel>(ORDERS_BASE_PATH, payload);

    return {
      orderId: response.data.orderId,
      ...(response.data.paymentToken ? { paymentToken: response.data.paymentToken } : {}),
    };
  },

  async getMyOrders(query: OrderFilterQuery): Promise<PaginatedResponse<UserOrder>> {
    const response = await httpClient.get<PaginatedResponse<UserOrderApiModel>>(`${ORDERS_BASE_PATH}/mine/`, {
      params: removeEmptyQueryValues(toBackendFilterQuery(query)),
    });

    return {
      ...response.data,
      items: response.data.items.map(mapUserOrder),
    };
  },

  async getMyOrderDetails(id: string): Promise<UserOrder> {
    const response = await httpClient.get<UserOrderApiModel>(`${ORDERS_BASE_PATH}/${id}/`);

    return mapUserOrder(response.data);
  },

  async getSoldOrders(query: OrderFilterQuery): Promise<PaginatedResponse<SellerOrder>> {
    const response = await httpClient.get<PaginatedResponse<SellerOrderApiModel>>(`${ORDERS_BASE_PATH}/sold/`, {
      params: removeEmptyQueryValues(toBackendFilterQuery(query)),
    });

    return {
      ...response.data,
      items: response.data.items.map(mapSellerOrder),
    };
  },

  async getSoldOrderDetails(id: string): Promise<SellerOrder> {
    const response = await httpClient.get<SellerOrderApiModel>(`${ORDERS_BASE_PATH}/sold/${id}/`);

    return mapSellerOrder(response.data);
  },
};
