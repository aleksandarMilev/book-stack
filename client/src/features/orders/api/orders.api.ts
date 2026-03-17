import { httpClient } from '@/api/httpClient';
import type { PaginatedResponse } from '@/api/types/api.types';
import { resolveAssetUrl } from '@/api/utils/assetUrl';
import type {
  OrderItem,
  OrderStatus,
  PaymentMethod,
  PaymentStatus,
  SellerOrder,
  SettlementStatus,
  UserOrder,
} from '@/features/orders/types';
import type { MarketplaceListingCondition } from '@/types/marketplace.types';
import { toPriceDisplayValue } from '@/utils/priceDisplay';

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
  paymentMethod: number | string;
  status: number | string;
  paymentStatus: number | string;
  settlementStatus: number | string;
  platformFeePercent: number;
  platformFeeAmount: number;
  sellerNetAmount: number;
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
  paymentMethod: number | string;
  status: number | string;
  paymentStatus: number | string;
  settlementStatus: number | string;
  platformFeePercent: number;
  platformFeeAmount: number;
  sellerNetAmount: number;
  createdOn: string;
  items: OrderItemApiModel[];
}

interface OrderBackendFilterQuery {
  SearchTerm?: string | undefined;
  Email?: string | undefined;
  Status?: number | undefined;
  PaymentMethod?: number | undefined;
  PaymentStatus?: number | undefined;
  SettlementStatus?: number | undefined;
  PageIndex?: number | undefined;
  PageSize?: number | undefined;
}

export interface OrderFilterQuery {
  searchTerm?: string | undefined;
  email?: string | undefined;
  status?: OrderStatus | undefined;
  paymentMethod?: PaymentMethod | undefined;
  paymentStatus?: PaymentStatus | undefined;
  settlementStatus?: SettlementStatus | undefined;
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
  paymentMethod: PaymentMethod;
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

const toPaymentMethod = (paymentMethod: number | string): PaymentMethod => {
  if (typeof paymentMethod === 'number') {
    return paymentMethod === 1 ? 'cashOnDelivery' : 'online';
  }

  const normalizedPaymentMethod = paymentMethod.trim().toLowerCase();
  if (normalizedPaymentMethod === 'cashondelivery' || normalizedPaymentMethod === 'cash_on_delivery') {
    return 'cashOnDelivery';
  }

  return 'online';
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

    if (status === 5) {
      return 'pendingConfirmation';
    }

    if (status === 6) {
      return 'shipped';
    }

    if (status === 7) {
      return 'delivered';
    }

    return 'pendingPayment';
  }

  const normalizedStatus = status.trim().toLowerCase();
  if (normalizedStatus === 'pendingpayment') {
    return 'pendingPayment';
  }

  if (normalizedStatus === 'pendingconfirmation') {
    return 'pendingConfirmation';
  }

  if (normalizedStatus === 'confirmed') {
    return 'confirmed';
  }

  if (normalizedStatus === 'shipped') {
    return 'shipped';
  }

  if (normalizedStatus === 'delivered') {
    return 'delivered';
  }

  if (normalizedStatus === 'completed') {
    return 'completed';
  }

  if (normalizedStatus === 'cancelled' || normalizedStatus === 'canceled') {
    return 'cancelled';
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

    if (status === 4) {
      return 'notRequired';
    }

    if (status === 5) {
      return 'expired';
    }

    if (status === 6) {
      return 'cancelled';
    }

    return 'pending';
  }

  const normalizedStatus = status.trim().toLowerCase();
  if (normalizedStatus === 'pending' || normalizedStatus === 'unpaid') {
    return 'pending';
  }

  if (normalizedStatus === 'paid') {
    return 'paid';
  }

  if (normalizedStatus === 'failed') {
    return 'failed';
  }

  if (normalizedStatus === 'refunded') {
    return 'refunded';
  }

  if (normalizedStatus === 'notrequired') {
    return 'notRequired';
  }

  if (normalizedStatus === 'expired') {
    return 'expired';
  }

  if (normalizedStatus === 'cancelled' || normalizedStatus === 'canceled') {
    return 'cancelled';
  }

  return 'pending';
};

const toSettlementStatus = (status: number | string): SettlementStatus => {
  if (typeof status === 'number') {
    if (status === 1) {
      return 'settled';
    }

    if (status === 2) {
      return 'waived';
    }

    if (status === 3) {
      return 'disputed';
    }

    return 'pending';
  }

  const normalizedStatus = status.trim().toLowerCase();
  if (
    normalizedStatus === 'pending' ||
    normalizedStatus === 'settled' ||
    normalizedStatus === 'waived' ||
    normalizedStatus === 'disputed'
  ) {
    return normalizedStatus;
  }

  return 'pending';
};

const ORDER_STATUS_TO_BACKEND: Record<OrderStatus, number> = {
  pendingPayment: 0,
  confirmed: 1,
  cancelled: 2,
  completed: 3,
  expired: 4,
  pendingConfirmation: 5,
  shipped: 6,
  delivered: 7,
};

const PAYMENT_METHOD_TO_BACKEND: Record<PaymentMethod, number> = {
  online: 0,
  cashOnDelivery: 1,
};

const PAYMENT_STATUS_TO_BACKEND: Record<PaymentStatus, number> = {
  pending: 0,
  paid: 1,
  failed: 2,
  refunded: 3,
  notRequired: 4,
  expired: 5,
  cancelled: 6,
};

const SETTLEMENT_STATUS_TO_BACKEND: Record<SettlementStatus, number> = {
  pending: 0,
  settled: 1,
  waived: 2,
  disputed: 3,
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
  paymentMethod: toPaymentMethod(order.paymentMethod),
  status: toOrderStatus(order.status),
  paymentStatus: toPaymentStatus(order.paymentStatus),
  settlementStatus: toSettlementStatus(order.settlementStatus),
  platformFeePercent: order.platformFeePercent,
  platformFeeAmount: toPriceDisplayValue(order.platformFeeAmount, order.currency),
  sellerNetAmount: toPriceDisplayValue(order.sellerNetAmount, order.currency),
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
  paymentMethod: toPaymentMethod(order.paymentMethod),
  status: toOrderStatus(order.status),
  paymentStatus: toPaymentStatus(order.paymentStatus),
  settlementStatus: toSettlementStatus(order.settlementStatus),
  platformFeePercent: order.platformFeePercent,
  platformFeeAmount: toPriceDisplayValue(order.platformFeeAmount, order.currency),
  sellerNetAmount: toPriceDisplayValue(order.sellerNetAmount, order.currency),
  createdOn: order.createdOn,
  items: order.items.map(mapOrderItem),
});

const toBackendFilterQuery = (query: OrderFilterQuery): OrderBackendFilterQuery => ({
  SearchTerm: query.searchTerm,
  Email: query.email,
  Status: query.status ? ORDER_STATUS_TO_BACKEND[query.status] : undefined,
  PaymentMethod: query.paymentMethod ? PAYMENT_METHOD_TO_BACKEND[query.paymentMethod] : undefined,
  PaymentStatus: query.paymentStatus ? PAYMENT_STATUS_TO_BACKEND[query.paymentStatus] : undefined,
  SettlementStatus: query.settlementStatus ? SETTLEMENT_STATUS_TO_BACKEND[query.settlementStatus] : undefined,
  PageIndex: query.pageIndex,
  PageSize: query.pageSize,
});

const toBackendPaymentMethod = (paymentMethod: PaymentMethod): number => PAYMENT_METHOD_TO_BACKEND[paymentMethod];

export const ordersApi = {
  async createOrder(payload: CreateOrderRequest): Promise<CreateOrderResult> {
    const response = await httpClient.post<CreateOrderResultApiModel>(ORDERS_BASE_PATH, {
      ...payload,
      paymentMethod: toBackendPaymentMethod(payload.paymentMethod),
    });

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

  async confirmSoldOrder(id: string): Promise<void> {
    await httpClient.put(`${ORDERS_BASE_PATH}/sold/${id}/confirm/`);
  },

  async shipSoldOrder(id: string): Promise<void> {
    await httpClient.put(`${ORDERS_BASE_PATH}/sold/${id}/ship/`);
  },

  async deliverSoldOrder(id: string): Promise<void> {
    await httpClient.put(`${ORDERS_BASE_PATH}/sold/${id}/deliver/`);
  },
};
