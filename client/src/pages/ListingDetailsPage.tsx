import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';

import { Button, Card, Container } from '@/components/ui';
import { ROUTES } from '@/routes/paths';

export function ListingDetailsPage() {
  const { t } = useTranslation();
  const { listingId } = useParams<{ listingId: string }>();

  return (
    <Container className="placeholder-page">
      <Card className="placeholder-page-card" elevated>
        <h1>{t('pages.listingDetails.title')}</h1>
        <p>{t('pages.listingDetails.description')}</p>
        <p className="placeholder-listing-id">{t('pages.listingDetails.listingId', { id: listingId ?? '-' })}</p>
        <Link to={ROUTES.marketplace}>
          <Button>{t('common.actions.browseMarketplace')}</Button>
        </Link>
      </Card>
    </Container>
  );
}
