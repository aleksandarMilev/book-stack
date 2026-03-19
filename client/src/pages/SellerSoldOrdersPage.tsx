import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Badge, Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { ordersApi } from '@/features/orders/api/orders.api';
import type { OrderStatus, PaymentStatus, SellerOrder } from '@/features/orders/types';
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

type SellerOrderAction = 'confirm' | 'ship' | 'deliver';

const getAvailableSellerAction = (status: OrderStatus): SellerOrderAction | null => {
  if (status === 'pendingConfirmation') {
    return 'confirm';
  }

  if (status === 'confirmed') {
    return 'ship';
  }

  if (status === 'shipped') {
    return 'deliver';
  }

  return null;
};

export function SellerSoldOrdersPage() {
  const { t } = useTranslation();
  const { language } = useLanguage();
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<OrderStatusFilter>('all');
  const [paymentStatusFilter, setPaymentStatusFilter] = useState<PaymentStatusFilter>('all');
  const [pageIndex, setPageIndex] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [orders, setOrders] = useState<SellerOrder[]>([]);
  const [totalItems, setTotalItems] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [expandedOrderId, setExpandedOrderId] = useState<string | null>(null);
  const [orderDetails, setOrderDetails] = useState<Record<string, SellerOrder>>({});
  const [detailsError, setDetailsError] = useState<Record<string, string>>({});
  const [detailsLoading, setDetailsLoading] = useState<Record<string, boolean>>({});
  const [actionLoading, setActionLoading] = useState<Record<string, boolean>>({});
  const [actionError, setActionError] = useState<Record<string, string>>({});
  const [reloadCounter, setReloadCounter] = useState(0);

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalItems / Math.max(pageSize, 1))), [pageSize, totalItems]);
  const resultsSectionRef = usePaginationScrollReset<HTMLDivElement>(pageIndex);

  useEffect(() => {
    let isActive = true;

    const loadSoldOrders = async (): Promise<void> => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const response = await ordersApi.getSoldOrders({
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
        setErrorMessage(getApiErrorMessage(error, t('pages.sellerSoldOrders.errorDescription')));
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    void loadSoldOrders();

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
      const response = await ordersApi.getSoldOrderDetails(orderId);
      setOrderDetails((previousState) => ({ ...previousState, [orderId]: response }));
    } catch (error: unknown) {
      setDetailsError((previousState) => ({
        ...previousState,
        [orderId]: getApiErrorMessage(error, t('pages.sellerSoldOrders.detailsError')),
      }));
    } finally {
      setDetailsLoading((previousState) => ({ ...previousState, [orderId]: false }));
    }
  };

  const handleSellerAction = async (orderId: string, action: SellerOrderAction): Promise<void> => {
    setActionLoading((previousState) => ({ ...previousState, [orderId]: true }));
    setActionError((previousState) => {
      const nextState = { ...previousState };
      delete nextState[orderId];
      return nextState;
    });

    try {
      if (action === 'confirm') {
        await ordersApi.confirmSoldOrder(orderId);
      } else if (action === 'ship') {
        await ordersApi.shipSoldOrder(orderId);
      } else {
        await ordersApi.deliverSoldOrder(orderId);
      }

      setOrderDetails((previousState) => {
        const nextState = { ...previousState };
        delete nextState[orderId];
        return nextState;
      });
      setReloadCounter((previousCounter) => previousCounter + 1);
    } catch (error: unknown) {
      setActionError((previousState) => ({
        ...previousState,
        [orderId]: getApiErrorMessage(error, t('pages.sellerSoldOrders.actionError')),
      }));
    } finally {
      setActionLoading((previousState) => ({ ...previousState, [orderId]: false }));
    }
  };

  const hasOrders = !isLoading && !errorMessage && orders.length > 0;

  return (
    <Container className="account-page">
      <header className="marketplace-header">
        <h1>{t('pages.sellerSoldOrders.title')}</h1>
        <p>{t('pages.sellerSoldOrders.subtitle')}</p>
      </header>

      <section className="marketplace-toolbar account-toolbar account-toolbar--orders">
        <Input
          label={t('pages.sellerSoldOrders.searchLabel')}
          onChange={(event) => {
            setPageIndex(1);
            setSearchTerm(event.target.value);
          }}
          placeholder={t('pages.sellerSoldOrders.searchPlaceholder')}
          value={searchTerm}
        />
        <label className="marketplace-sort-label" htmlFor="sold-orders-status">
          <span>{t('pages.sellerSoldOrders.statusFilterLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="sold-orders-status"
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
        <label className="marketplace-sort-label" htmlFor="sold-orders-payment-status">
          <span>{t('pages.sellerSoldOrders.paymentFilterLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="sold-orders-payment-status"
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
        <label className="marketplace-sort-label" htmlFor="sold-orders-page-size">
          <span>{t('marketplace.pageSizeLabel')}</span>
          <select
            className="marketplace-sort-select"
            id="sold-orders-page-size"
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

      <div className="marketplace-results" ref={resultsSectionRef}>
        <p className="marketplace-results-count">{t('pages.sellerSoldOrders.resultsCount', { count: totalItems })}</p>

        {isLoading ? (
          <LoadingState
            description={t('pages.sellerSoldOrders.loadingDescription')}
            title={t('pages.sellerSoldOrders.loadingTitle')}
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
            title={t('pages.sellerSoldOrders.errorTitle')}
          />
        ) : null}

        {!isLoading && !errorMessage && orders.length === 0 ? (
          <EmptyState
            description={t('pages.sellerSoldOrders.emptyDescription')}
            title={t('pages.sellerSoldOrders.emptyTitle')}
          />
        ) : null}

        {hasOrders ? (
          <div className="order-card-list">
            {orders.map((order) => {
              const isExpanded = expandedOrderId === order.id;
              const detail = orderDetails[order.id] ?? order;
              const availableAction = getAvailableSellerAction(order.status);
              const isActionBlockedByPayment =
                order.paymentMethod === 'online' &&
                order.paymentStatus !== 'paid' &&
                availableAction !== null;
              const canRunAction = availableAction !== null && !isActionBlockedByPayment;

              return (
                <Card className="order-card" key={order.id}>
                  <div className="order-card-head">
                    <div>
                      <p className="order-card-id">
                        {t('pages.myOrders.orderIdLabel')}: {formatOrderId(order.id)}
                      </p>
                      <p className="order-card-date">
                        {formatDateTime({ value: order.createdOn, language })}
                      </p>
                    </div>
                    <PriceDisplay value={order.sellerTotal} />
                  </div>

                  <div className="order-card-statuses">
                    <Badge variant={getPaymentMethodBadgeVariant(order.paymentMethod)}>
                      {t(`taxonomy.paymentMethod.${order.paymentMethod}`)}
                    </Badge>
                    <Badge variant={getOrderStatusBadgeVariant(order.status)}>
                      {t(`taxonomy.orderStatus.${order.status}`)}
                    </Badge>
                    <Badge variant={getPaymentStatusBadgeVariant(order.paymentStatus)}>
                      {t(`taxonomy.paymentStatus.${order.paymentStatus}`)}
                    </Badge>
                    <Badge variant={getSettlementStatusBadgeVariant(order.settlementStatus)}>
                      {t(`taxonomy.settlementStatus.${order.settlementStatus}`)}
                    </Badge>
                  </div>
                  <p className="checkout-summary-meta">{t(`pages.sellerSoldOrders.statusDescriptions.${order.status}`)}</p>

                  <ul className="order-items-summary">
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
                  {actionError[order.id] ? <p className="auth-error">{actionError[order.id]}</p> : null}

                  {availableAction ? (
                    <Button
                      disabled={Boolean(actionLoading[order.id]) || !canRunAction}
                      onClick={() => {
                        if (canRunAction) {
                          void handleSellerAction(order.id, availableAction);
                        }
                      }}
                    >
                      {actionLoading[order.id]
                        ? t('pages.sellerSoldOrders.actionInProgress')
                        : t(`pages.sellerSoldOrders.actions.${availableAction}`)}
                    </Button>
                  ) : null}
                  {isActionBlockedByPayment ? (
                    <p className="checkout-summary-meta">{t('pages.sellerSoldOrders.actionBlockedByPayment')}</p>
                  ) : null}

                  <Button
                    onClick={() => {
                      void handleToggleDetails(order.id);
                    }}
                    variant="secondary"
                  >
                    {isExpanded
                      ? t('pages.sellerSoldOrders.hideDetails')
                      : detailsLoading[order.id]
                        ? t('pages.sellerSoldOrders.loadingDetails')
                        : t('pages.sellerSoldOrders.showDetails')}
                  </Button>

                  {isExpanded ? (
                    <div className="order-details-panel">
                      <h3>{t('pages.sellerSoldOrders.detailsTitle')}</h3>
                      <p>
                        {detail.customerFirstName} {detail.customerLastName}
                      </p>
                      <p>{detail.email}</p>
                      <p>
                        {detail.addressLine}, {detail.city}, {detail.country}
                        {detail.postalCode ? `, ${detail.postalCode}` : ''}
                      </p>
                      <div className="order-details-financials">
                        <div className="checkout-summary-price-line">
                          <span>{t('pages.sellerSoldOrders.financial.sellerTotalLabel')}</span>
                          <PriceDisplay value={detail.sellerTotal} />
                        </div>
                        <div className="checkout-summary-price-line">
                          <span>
                            {t('pages.sellerSoldOrders.financial.platformFeeLabel', {
                              percent: detail.platformFeePercent.toFixed(2),
                            })}
                          </span>
                          <PriceDisplay value={detail.platformFeeAmount} />
                        </div>
                        <div className="checkout-summary-price-line">
                          <span>{t('pages.sellerSoldOrders.financial.sellerNetLabel')}</span>
                          <PriceDisplay value={detail.sellerNetAmount} />
                        </div>
                      </div>
                      <div className="order-details-items">
                        {detail.items.map((item) => (
                          <div className="order-details-item" key={item.id}>
                            {item.listingImageUrl ? (
                              <img
                                alt={t('marketplace.listingImageAlt', { title: item.bookTitle })}
                                className="order-details-item-image"
                                src={item.listingImageUrl}
                              />
                            ) : (
                              <div className="order-details-item-image-placeholder" />
                            )}
                            <div>
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
