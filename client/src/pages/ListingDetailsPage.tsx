import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Button, Card, Container } from '@/components/ui';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import { ROUTES } from '@/routes/paths';
import type { MarketplaceListing } from '@/types/marketplace.types';

export function ListingDetailsPage() {
  const { t } = useTranslation();
  const { listingId } = useParams<{ listingId: string }>();
  const [listing, setListing] = useState<MarketplaceListing | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!listingId) {
      setErrorMessage(t('pages.listingDetails.notFound'));
      setIsLoading(false);
      return;
    }

    let isActive = true;

    const loadListing = async (): Promise<void> => {
      setIsLoading(true);
      setErrorMessage(null);

      try {
        const response = await listingsApi.getListingById(listingId);

        if (!isActive) {
          return;
        }

        setListing(response);
      } catch (error: unknown) {
        if (!isActive) {
          return;
        }

        setErrorMessage(getApiErrorMessage(error, t('pages.listingDetails.error')));
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    void loadListing();

    return () => {
      isActive = false;
    };
  }, [listingId, t]);

  if (isLoading) {
    return (
      <Container className="placeholder-page">
        <Card className="placeholder-page-card" elevated>
          <p>{t('pages.listingDetails.loading')}</p>
        </Card>
      </Container>
    );
  }

  if (errorMessage || !listing) {
    return (
      <Container className="placeholder-page">
        <Card className="placeholder-page-card" elevated>
          <h1>{t('pages.listingDetails.title')}</h1>
          <p>{errorMessage ?? t('pages.listingDetails.notFound')}</p>
          <Link to={ROUTES.marketplace}>
            <Button>{t('common.actions.browseMarketplace')}</Button>
          </Link>
        </Card>
      </Container>
    );
  }

  return (
    <Container className="listing-details-page">
      <article className="listing-details-layout">
        <Card className="listing-details-media" elevated>
          {listing.imageUrl ? (
            <img
              alt={t('pages.listingDetails.imageAlt', { title: listing.title })}
              className="listing-details-image"
              src={listing.imageUrl}
            />
          ) : (
            <div className="listing-details-image-placeholder" />
          )}
        </Card>

        <Card className="listing-details-content" elevated>
          <p className="listing-details-genre">{listing.genre}</p>
          <h1>{listing.title}</h1>
          <p className="listing-details-author">{listing.author}</p>
          <PriceDisplay className="listing-details-price" value={listing.price} />

          <dl className="listing-details-metadata">
            {listing.publisher ? (
              <>
                <dt>{t('pages.listingDetails.publisherLabel')}</dt>
                <dd>{listing.publisher}</dd>
              </>
            ) : null}
            {listing.publishedOn ? (
              <>
                <dt>{t('pages.listingDetails.publishedOnLabel')}</dt>
                <dd>{listing.publishedOn}</dd>
              </>
            ) : null}
            {listing.isbn ? (
              <>
                <dt>{t('pages.listingDetails.isbnLabel')}</dt>
                <dd>{listing.isbn}</dd>
              </>
            ) : null}
            <dt>{t('pages.listingDetails.conditionLabel')}</dt>
            <dd>{t(`taxonomy.conditions.${listing.condition}`)}</dd>
            <dt>{t('pages.listingDetails.quantityLabel')}</dt>
            <dd>{listing.quantity}</dd>
          </dl>

          <section className="listing-details-description">
            <h2>{t('pages.listingDetails.descriptionTitle')}</h2>
            <p>{listing.description}</p>
          </section>

          <p className="placeholder-listing-id">{t('pages.listingDetails.listingId', { id: listing.id })}</p>
        </Card>
      </article>

      <Card className="listing-details-actions">
        <Link to={ROUTES.marketplace}>
          <Button variant="secondary">{t('common.actions.backToMarketplace')}</Button>
        </Link>
      </Card>
    </Container>
  );
}
