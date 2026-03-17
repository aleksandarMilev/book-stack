import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';

import { Badge, Button, Card, Container } from '@/components/ui';
import { ROUTES } from '@/routes/paths';
import { useAuthCapabilities } from '@/store/auth.store';

type ConfirmationPaymentMethod = 'cashOnDelivery' | 'online' | null;

const parsePaymentMethod = (value: string | null): ConfirmationPaymentMethod => {
  if (!value) {
    return null;
  }

  const normalizedValue = value.trim().toLowerCase();
  if (normalizedValue === 'cashondelivery') {
    return 'cashOnDelivery';
  }

  if (normalizedValue === 'online') {
    return 'online';
  }

  return null;
};

export function OrderConfirmationPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const capabilities = useAuthCapabilities();

  const orderId = searchParams.get('orderId');
  const paymentMethod = parsePaymentMethod(searchParams.get('paymentMethod'));
  const paymentMethodKey = paymentMethod ?? 'cashOnDelivery';

  return (
    <Container className="payment-return-page">
      <Card className="payment-return-card" elevated>
        <Badge variant="success">{t('pages.orderConfirmation.badge')}</Badge>
        <h1>{t('pages.orderConfirmation.title')}</h1>
        <p>{t(`pages.orderConfirmation.descriptions.${paymentMethodKey}`)}</p>

        {orderId ? (
          <p className="payment-return-order-id">
            {t('pages.orderConfirmation.orderIdLabel')}: {orderId}
          </p>
        ) : (
          <p className="payment-return-note">{t('pages.orderConfirmation.missingOrderReference')}</p>
        )}

        <div className="payment-return-actions">
          {capabilities.isAuthenticated ? (
            <Link to={ROUTES.myOrders}>
              <Button>{t('pages.orderConfirmation.myOrdersAction')}</Button>
            </Link>
          ) : null}

          <Link to={ROUTES.marketplace}>
            <Button variant="secondary">{t('pages.orderConfirmation.marketplaceAction')}</Button>
          </Link>
        </div>
      </Card>
    </Container>
  );
}
