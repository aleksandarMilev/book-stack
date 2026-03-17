import type { BadgeVariant } from '@/components/ui/Badge';
import type { OrderStatus, PaymentMethod, PaymentStatus, SettlementStatus } from '@/features/orders/types';

export const ORDER_STATUS_FILTERS: Array<'all' | OrderStatus> = [
  'all',
  'pendingPayment',
  'pendingConfirmation',
  'confirmed',
  'shipped',
  'delivered',
  'completed',
  'cancelled',
  'expired',
];

export const PAYMENT_STATUS_FILTERS: Array<'all' | PaymentStatus> = [
  'all',
  'pending',
  'paid',
  'failed',
  'refunded',
  'notRequired',
  'expired',
  'cancelled',
];

export const getOrderStatusBadgeVariant = (status: OrderStatus): BadgeVariant => {
  if (status === 'confirmed' || status === 'completed') {
    return 'success';
  }

  if (status === 'cancelled' || status === 'expired') {
    return 'danger';
  }

  return 'warning';
};

export const getPaymentStatusBadgeVariant = (status: PaymentStatus): BadgeVariant => {
  if (status === 'paid' || status === 'notRequired') {
    return 'success';
  }

  if (status === 'refunded') {
    return 'accent';
  }

  if (status === 'failed' || status === 'expired' || status === 'cancelled') {
    return 'danger';
  }

  return 'warning';
};

export const getSettlementStatusBadgeVariant = (status: SettlementStatus): BadgeVariant => {
  if (status === 'settled') {
    return 'success';
  }

  if (status === 'waived') {
    return 'neutral';
  }

  if (status === 'disputed') {
    return 'danger';
  }

  return 'warning';
};

export const getPaymentMethodBadgeVariant = (method: PaymentMethod): BadgeVariant =>
  method === 'online' ? 'accent' : 'neutral';
