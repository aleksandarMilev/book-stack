import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Badge, Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { ordersApi } from '@/features/orders/api/orders.api';
import type { OrderStatus, PaymentStatus, UserOrder } from '@/features/orders/types';
import {
  getOrderStatusBadgeVariant,
  getPaymentMethodBadgeVariant,
  getPaymentStatusBadgeVariant,
  getSettlementStatusBadgeVariant,
  ORDER_STATUS_FILTERS,
  PAYMENT_STATUS_FILTERS,
} from '@/features/orders/ui/statusPresentation';
import { useLanguage } from '@/hooks/useLanguage';
import { usePaginationScrollReset } from '@/hooks/usePaginationScrollReset';
import { formatDateTime } from '@/utils/formatters';

type OrderStatusFilter = 'all' | OrderStatus;
type PaymentStatusFilter = 'all' | PaymentStatus;

const PAGE_SIZE_OPTIONS = [10, 20, 30] as const;

const formatOrderId = (orderId: string): string => {
  if (orderId.length <= 8) {
    return orderId;
  }

  return `${orderId.slice(0, 8)}...`;
};

export function MyOrdersPage() {
  const { t } = useTranslation();
  const { language } = useLanguage();
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<OrderStatusFilter>('all');
  const [paymentStatusFilter, setPaymentStatusFilter] = useState<PaymentStatusFilter>('all');
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [orders, setOrders] = useState<UserOrder[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [expandedOrderId, setExpandedOrderId] = useState<string | null>(null);
  const [orderDetails, setOrderDetails] = useState<Record<string, UserOrder>>({});
  const [detailsError, setDetailsError] = useState<Record<string, string>>({});
  const [detailsLoading, setDetailsLoading] = useState<Record<string, boolean>>({});
  const [reloadCounter, setReloadCounter] = useState(0);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalItems / Math.max(pageSize, 1))), [pageSize, totalItems]);
  const resultsSectionRef = usePaginationScrollReset<HTMLDivElement>(pageIndex);

  useEffect(() => {
    let isActive = true;

    const loadOrders = async (): Promise<void> => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const response = await ordersApi.getMyOrders({
          searchTerm: searchTerm || undefined,
          status: statusFilter === 'all' ? undefined : statusFilter,
          paymentStatus: paymentStatusFilter === 'all' ? undefined : paymentStatusFilter,
          pageIndex,
          pageSize,
        });

        if (!isActive) {
          return;
        }

        setOrders(response.items);
        setTotalItems(response.totalItems);
      } catch (error: unknown) {
        if (!isActive) {
          return;
        }

        setOrders([]);
        setTotalItems(0);
        setErrorMessage(getApiErrorMessage(error, t('pages.myOrders.errorDescription')));
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    void loadOrders();

    return () => {
      isActive = false;
    };
  }, [pageIndex, pageSize, paymentStatusFilter, reloadCounter, searchTerm, statusFilter, t]);

  useEffect(() => {
    if (pageIndex <= totalPages) {
      return;
    }

    setPageIndex(totalPages);
  }, [pageIndex, totalPages]);

  const handleToggleDetails = async (orderId: string): Promise<void> => {
    if (expandedOrderId === orderId) {
      setExpandedOrderId(null);
      return;
    }

    setExpandedOrderId(orderId);
    if (orderDetails[orderId] || detailsLoading[orderId]) {
      return;
    }

    setDetailsLoading((previousState) => ({ ...previousState, [orderId]: true }));
    setDetailsError((previousState) => {
      const nextState = { ...previousState };
      delete nextState[orderId];
      return nextState;
    });

    try {
      const response = await ordersApi.getMyOrderDetails(orderId);
      setOrderDetails((previousState) => ({ ...previousState, [orderId]: response }));
    } catch (error: unknown) {
      setDetailsError((previousState) => ({
        ...previousState,
        [orderId]: getApiErrorMessage(error, t('pages.myOrders.detailsError')),
      }));
    } finally {
      setDetailsLoading((previousState) => ({ ...previousState, [orderId]: false }));
    }
  };

  const hasOrders = !isLoading && !errorMessage && orders.length > 0;

  return (
    <Container className="account-page my-orders-page">
      <header className="marketplace-header my-orders-header">
        <h1>{t('pages.myOrders.title')}</h1>
        <p>{t('pages.myOrders.subtitle')}</p>
      </header>

      <section className="marketplace-toolbar account-toolbar account-toolbar--orders my-orders-toolbar">
        <Input
          label={t('pages.myOrders.searchLabel')}
          onChange={(event) => {
            setPageIndex(1);
            setSearchTerm(event.target.value);
          }}
          placeholder={t('pages.myOrders.searchPlaceholder')}
          value={searchTerm}
        />
        <label className="marketplace-sort-label" htmlFor="my-orders-status">
          <span>{t('pages.myOrders.statusFilterLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="my-orders-status"
            onChange={(event) => {
              setPageIndex(1);
              setStatusFilter(event.target.value as OrderStatusFilter);
            }}
            value={statusFilter}
          >
            {ORDER_STATUS_FILTERS.map((status) => (
              <option key={status} value={status}>
                {t(`pages.myOrders.filters.status.${status}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="marketplace-sort-label" htmlFor="my-orders-payment-status">
          <span>{t('pages.myOrders.paymentFilterLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="my-orders-payment-status"
            onChange={(event) => {
              setPageIndex(1);
              setPaymentStatusFilter(event.target.value as PaymentStatusFilter);
            }}
            value={paymentStatusFilter}
          >
            {PAYMENT_STATUS_FILTERS.map((status) => (
              <option key={status} value={status}>
                {t(`pages.myOrders.filters.payment.${status}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="marketplace-sort-label" htmlFor="my-orders-page-size">
          <span>{t('marketplace.pageSizeLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="my-orders-page-size"
            onChange={(event) => {
              setPageIndex(1);
              setPageSize(Number.parseInt(event.target.value, 10));
            }}
            value={pageSize}
          >
            {PAGE_SIZE_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {t('marketplace.pageSizeOption', { count: option })}
              </option>
            ))}
          </select>
        </label>
      </section>

      <div className="marketplace-results my-orders-results" ref={resultsSectionRef}>
        <p className="marketplace-results-count my-orders-results-count">
          {t('pages.myOrders.resultsCount', { count: totalItems })}
        </p>

        {isLoading ? (
          <LoadingState
            description={t('pages.myOrders.loadingDescription')}
            title={t('pages.myOrders.loadingTitle')}
          />
        ) : null}

        {!isLoading && errorMessage ? (
          <EmptyState
            action={
              <Button
                onClick={() => {
                  setReloadCounter((previousCounter) => previousCounter + 1);
                }}
              >
                {t('common.actions.retry')}
              </Button>
            }
            description={errorMessage}
            title={t('pages.myOrders.errorTitle')}
          />
        ) : null}

        {!isLoading && !errorMessage && orders.length === 0 ? (
          <EmptyState
            description={t('pages.myOrders.emptyDescription')}
            title={t('pages.myOrders.emptyTitle')}
          />
        ) : null}

        {hasOrders ? (
          <div className="order-card-list my-orders-list">
            {orders.map((order) => {
              const isExpanded = expandedOrderId === order.id;
              const detail = orderDetails[order.id] ?? order;

              return (
                <Card className="order-card my-orders-entry" key={order.id}>
                  <div className="order-card-head my-orders-entry-head">
                    <div className="my-orders-entry-summary">
                      <p className="order-card-id my-orders-entry-id">
                        {t('pages.myOrders.orderIdLabel')}: {formatOrderId(order.id)}
                      </p>
                      <p className="order-card-date my-orders-entry-date">
                        {formatDateTime({ value: order.createdOn, language })}
                      </p>
                    </div>
                    <div className="my-orders-entry-total">
                      <PriceDisplay value={order.total} />
                    </div>
                  </div>

                  <div className="order-card-statuses my-orders-entry-statuses">
                    <Badge className="my-orders-status-badge" variant={getPaymentMethodBadgeVariant(order.paymentMethod)}>
                      {t(`taxonomy.paymentMethod.${order.paymentMethod}`)}
                    </Badge>
                    <Badge className="my-orders-status-badge" variant={getOrderStatusBadgeVariant(order.status)}>
                      {t(`taxonomy.orderStatus.${order.status}`)}
                    </Badge>
                    <Badge className="my-orders-status-badge" variant={getPaymentStatusBadgeVariant(order.paymentStatus)}>
                      {t(`taxonomy.paymentStatus.${order.paymentStatus}`)}
                    </Badge>
                    <Badge
                      className="my-orders-status-badge"
                      variant={getSettlementStatusBadgeVariant(order.settlementStatus)}
                    >
                      {t(`taxonomy.settlementStatus.${order.settlementStatus}`)}
                    </Badge>
                  </div>
                  <p className="checkout-summary-meta my-orders-entry-description">
                    {t(`pages.myOrders.statusDescriptions.${order.status}`)}
                  </p>

                  <ul className="order-items-summary my-orders-entry-items">
                    {order.items.slice(0, 2).map((item) => (
                      <li key={item.id}>
                        {item.bookTitle} x{item.quantity}
                      </li>
                    ))}
                    {order.items.length > 2 ? (
                      <li>{t('pages.myOrders.moreItems', { count: order.items.length - 2 })}</li>
                    ) : null}
                  </ul>

                  {detailsError[order.id] ? <p className="auth-error">{detailsError[order.id]}</p> : null}

                  <div className="my-orders-entry-actions">
                    <Button
                      className="my-orders-entry-action"
                      onClick={() => {
                        void handleToggleDetails(order.id);
                      }}
                      variant="secondary"
                    >
                      {isExpanded
                        ? t('pages.myOrders.hideDetails')
                        : detailsLoading[order.id]
                          ? t('pages.myOrders.loadingDetails')
                          : t('pages.myOrders.showDetails')}
                    </Button>
                  </div>

                  {isExpanded ? (
                    <div className="order-details-panel my-orders-details-panel">
                      <h3>{t('pages.myOrders.detailsTitle')}</h3>
                      <p>
                        {detail.customerFirstName} {detail.customerLastName}
                      </p>
                      <p>{detail.email}</p>
                      <p>
                        {detail.addressLine}, {detail.city}, {detail.country}
                        {detail.postalCode ? `, ${detail.postalCode}` : ''}
                      </p>
                      <div className="order-details-financials my-orders-details-financials">
                        <div className="checkout-summary-price-line">
                          <span>{t('pages.myOrders.financial.totalLabel')}</span>
                          <PriceDisplay value={detail.total} />
                        </div>
                        <div className="checkout-summary-price-line">
                          <span>
                            {t('pages.myOrders.financial.platformFeeLabel', {
                              percent: detail.platformFeePercent.toFixed(2),
                            })}
                          </span>
                          <PriceDisplay value={detail.platformFeeAmount} />
                        </div>
                        <div className="checkout-summary-price-line">
                          <span>{t('pages.myOrders.financial.sellerNetLabel')}</span>
                          <PriceDisplay value={detail.sellerNetAmount} />
                        </div>
                      </div>
                      <div className="order-details-items my-orders-details-items">
                        {detail.items.map((item) => (
                          <div className="order-details-item my-orders-details-item" key={item.id}>
                            {item.listingImageUrl ? (
                              <img
                                alt={t('marketplace.listingImageAlt', { title: item.bookTitle })}
                                className="order-details-item-image"
                                src={item.listingImageUrl}
                              />
                            ) : (
                              <div className="order-details-item-image-placeholder" />
                            )}
                            <div className="my-orders-details-item-content">
                              <p className="order-details-item-title">{item.bookTitle}</p>
                              <p className="order-details-item-meta">
                                {item.bookAuthor} · {t(`taxonomy.conditions.${item.condition}`)}
                              </p>
                              <p className="order-details-item-meta">
                                {t('pages.myOrders.itemQuantity', { count: item.quantity })}
                              </p>
                              <PriceDisplay value={item.totalPrice} />
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  ) : null}
                </Card>
              );
            })}
          </div>
        ) : null}

        {hasOrders ? (
          <div className="marketplace-pagination">
            <Button
              disabled={pageIndex <= 1}
              onClick={() => {
                setPageIndex((previousPageIndex) => Math.max(previousPageIndex - 1, 1));
              }}
              variant="secondary"
            >
              {t('common.actions.previous')}
            </Button>
            <p>{t('marketplace.pageIndicator', { page: pageIndex, totalPages })}</p>
            <Button
              disabled={pageIndex >= totalPages}
              onClick={() => {
                setPageIndex((previousPageIndex) => Math.min(previousPageIndex + 1, totalPages));
              }}
              variant="secondary"
            >
              {t('common.actions.next')}
            </Button>
          </div>
        ) : null}
      </div>
    </Container>
  );
}
