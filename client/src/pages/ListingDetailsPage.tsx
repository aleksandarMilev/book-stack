import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Badge, Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import { getCheckoutRoute, ROUTES } from '@/routes/paths';
import type { MarketplaceListing } from '@/types/marketplace.types';

export function ListingDetailsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { listingId } = useParams<{ listingId: string }>();
  const [listing, setListing] = useState<MarketplaceListing | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [selectedQuantity, setSelectedQuantity] = useState(1);
  const [reloadCounter, setReloadCounter] = useState(0);

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
        setSelectedQuantity((previousQuantity) => Math.min(previousQuantity, Math.max(response.quantity, 1)));
      } catch (error: unknown) {
        if (!isActive) {
          return;
        }

        setListing(null);
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
  }, [listingId, reloadCounter, t]);

  if (isLoading) {
    return (
      <Container className="listing-details-page">
        <LoadingState description={t('pages.listingDetails.loadingDescription')} title={t('pages.listingDetails.loadingTitle')} />
      </Container>
    );
  }

  if (errorMessage || !listing) {
    return (
      <Container className="listing-details-page">
        <EmptyState
          action={
            <div className="listing-details-error-actions">
              <Button
                onClick={() => {
                  setReloadCounter((previousCounter) => previousCounter + 1);
                }}
                variant="secondary"
              >
                {t('common.actions.retry')}
              </Button>
              <Link to={ROUTES.marketplace}>
                <Button>{t('common.actions.browseMarketplace')}</Button>
              </Link>
            </div>
          }
          description={errorMessage ?? t('pages.listingDetails.notFound')}
          title={t('pages.listingDetails.errorTitle')}
        />
      </Container>
    );
  }

  const canPurchase = listing.isApproved && listing.quantity > 0;
  const supportedPaymentMethods = [
    listing.supportsOnlinePayment ? t('taxonomy.paymentMethod.online') : null,
    listing.supportsCashOnDelivery ? t('taxonomy.paymentMethod.cashOnDelivery') : null,
  ].filter((method): method is string => Boolean(method));

  const handleQuantityChange = (value: string): void => {
    const parsedValue = Number.parseInt(value, 10);
    if (Number.isNaN(parsedValue)) {
      setSelectedQuantity(1);
      return;
    }

    setSelectedQuantity(Math.min(Math.max(parsedValue, 1), listing.quantity));
  };

  const handleBuyNow = (): void => {
    if (!canPurchase) {
      return;
    }

    navigate(getCheckoutRoute(listing.id, selectedQuantity));
  };

  return (
    <Container className="listing-details-page">
      <article className="listing-details-layout">
        <Card className="listing-details-media" elevated>
          <div className="listing-details-media-frame">
            {listing.imageUrl ? (
              <img
                alt={t('pages.listingDetails.imageAlt', { title: listing.title })}
                className="listing-details-image"
                src={listing.imageUrl}
              />
            ) : (
              <div className="listing-details-image-placeholder" />
            )}
          </div>
        </Card>

        <div className="listing-details-main">
          <Card className="listing-details-content" elevated>
            <div className="listing-details-heading">
              <div className="listing-details-meta-head">
                <p className="listing-details-genre">{listing.genre}</p>
                <Badge className="listing-details-condition-badge" variant="success">
                  {t(`taxonomy.conditions.${listing.condition}`)}
                </Badge>
              </div>
              <h1>{listing.title}</h1>
              <p className="listing-details-author">{listing.author}</p>
            </div>

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
              <dt>{t('pages.listingDetails.quantityLabel')}</dt>
              <dd>{listing.quantity}</dd>
            </dl>

            {supportedPaymentMethods.length > 0 ? (
              <div className="listing-details-payment-methods" aria-label={t('pages.listingDetails.purchaseTitle')}>
                {supportedPaymentMethods.map((method) => (
                  <Badge className="listing-details-payment-badge" key={method} variant="neutral">
                    {method}
                  </Badge>
                ))}
              </div>
            ) : null}

            <p className="placeholder-listing-id">{t('pages.listingDetails.listingId', { id: listing.id })}</p>
          </Card>

          <Card className="listing-details-purchase-card" elevated>
            <p className="listing-details-purchase-kicker">{t('common.labels.trustedMarketplace')}</p>
            <PriceDisplay className="listing-details-price listing-details-purchase-price" value={listing.price} />

            <section className="listing-details-purchase">
              <h2>{t('pages.listingDetails.purchaseTitle')}</h2>
              <div className="listing-details-purchase-row">
                <Input
                  className="listing-details-quantity-input"
                  label={t('pages.listingDetails.purchaseQuantityLabel')}
                  max={listing.quantity}
                  min={1}
                  onChange={(event) => {
                    handleQuantityChange(event.target.value);
                  }}
                  type="number"
                  value={String(selectedQuantity)}
                />
                <p className="listing-details-stock">
                  {canPurchase
                    ? t('pages.listingDetails.inStock', { count: listing.quantity })
                    : t('pages.listingDetails.outOfStock')}
                </p>
              </div>
              <Button disabled={!canPurchase} onClick={handleBuyNow}>
                {t('pages.listingDetails.buyNow')}
              </Button>
            </section>

            <div className="listing-details-secondary-actions">
              <Link to={ROUTES.marketplace}>
                <Button fullWidth variant="secondary">
                  {t('common.actions.backToMarketplace')}
                </Button>
              </Link>
            </div>
          </Card>
        </div>
      </article>

      <Card className="listing-details-description-card">
        <section className="listing-details-description">
          <h2>{t('pages.listingDetails.descriptionTitle')}</h2>
          <p>{listing.description}</p>
        </section>
      </Card>
    </Container>
  );
}
