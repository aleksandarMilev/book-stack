import type { FormEvent } from 'react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Badge, Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import { SellerProfileRequiredState } from '@/features/sellerProfiles/components/SellerProfileRequiredState';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { ROUTES } from '@/routes/paths';
import type { MarketplaceListing, MarketplaceListingCondition } from '@/types/marketplace.types';

interface ListingEditFormState {
  price: string;
  quantity: string;
  condition: MarketplaceListingCondition;
  description: string;
  image: File | null;
  removeImage: boolean;
}

const DEFAULT_CURRENCY = 'EUR';

const CONDITION_OPTIONS: MarketplaceListingCondition[] = [
  'new',
  'likeNew',
  'veryGood',
  'good',
  'acceptable',
  'poor',
];

const parsePrice = (value: string): number => {
  const parsedValue = Number.parseFloat(value);
  if (Number.isNaN(parsedValue)) {
    return 0;
  }

  return parsedValue;
};

const parseQuantity = (value: string): number => {
  const parsedValue = Number.parseInt(value, 10);
  if (Number.isNaN(parsedValue)) {
    return 0;
  }

  return parsedValue;
};

const toEditFormState = (listing: MarketplaceListing): ListingEditFormState => ({
  price: listing.price.primary.amount.toString(),
  quantity: listing.quantity.toString(),
  condition: listing.condition,
  description: listing.description,
  image: null,
  removeImage: false,
});

const getModerationStatus = (listing: MarketplaceListing): 'approved' | 'pending' | 'rejected' => {
  if (listing.isApproved) {
    return 'approved';
  }

  return listing.rejectionReason ? 'rejected' : 'pending';
};

const getModerationBadgeVariant = (
  status: 'approved' | 'pending' | 'rejected',
): 'success' | 'warning' | 'danger' => {
  if (status === 'approved') {
    return 'success';
  }

  if (status === 'rejected') {
    return 'danger';
  }

  return 'warning';
};

export function MyListingEditPage() {
  const { t } = useTranslation();
  const { listingId } = useParams<{ listingId: string }>();
  const sellerProfile = useSellerProfileStore((state) => state.profile);
  const sellerProfileLoadState = useSellerProfileStore((state) => state.loadState);
  const loadSellerProfile = useSellerProfileStore((state) => state.loadMine);

  const [listing, setListing] = useState<MarketplaceListing | null>(null);
  const [formState, setFormState] = useState<ListingEditFormState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    void loadSellerProfile();
  }, [loadSellerProfile]);

  useEffect(() => {
    if (!listingId) {
      setLoadError(t('pages.myListingEdit.invalidListing'));
      setIsLoading(false);
      return;
    }

    let isActive = true;

    const loadListing = async (): Promise<void> => {
      setIsLoading(true);
      setLoadError(null);

      try {
        const response = await listingsApi.getListingById(listingId);
        if (!isActive) {
          return;
        }

        setListing(response);
        setFormState(toEditFormState(response));
      } catch (error: unknown) {
        if (!isActive) {
          return;
        }

        setListing(null);
        setFormState(null);
        setLoadError(getApiErrorMessage(error, t('pages.myListingEdit.loadError')));
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

  const moderationStatus = useMemo(
    () => (listing ? getModerationStatus(listing) : 'pending'),
    [listing],
  );

  const hasActiveSellerProfile = Boolean(sellerProfile?.isActive);
  const isCheckingSellerProfile =
    (sellerProfileLoadState === 'loading' || sellerProfileLoadState === 'idle') && !sellerProfile;

  const validateForm = (): string | null => {
    if (!formState) {
      return t('pages.myListingEdit.invalidListing');
    }

    if (parsePrice(formState.price) < 0.01) {
      return t('pages.myListingEdit.validation.priceRequired');
    }

    if (parseQuantity(formState.quantity) < 1) {
      return t('pages.myListingEdit.validation.quantityRequired');
    }

    if (formState.description.trim().length < 10) {
      return t('pages.myListingEdit.validation.descriptionMinLength');
    }

    return null;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!listing || !formState) {
      setSubmitError(t('pages.myListingEdit.invalidListing'));
      return;
    }

    const validationError = validateForm();
    if (validationError) {
      setSubmitError(validationError);
      return;
    }

    setIsSubmitting(true);

    try {
      await listingsApi.editListing(listing.id, {
        bookId: listing.bookId,
        price: parsePrice(formState.price),
        currency: DEFAULT_CURRENCY,
        condition: formState.condition,
        quantity: parseQuantity(formState.quantity),
        description: formState.description,
        image: formState.image,
        removeImage: formState.removeImage,
      });

      const refreshedListing = await listingsApi.getListingById(listing.id);
      setListing(refreshedListing);
      setFormState(toEditFormState(refreshedListing));
      setSubmitSuccess(t('pages.myListingEdit.submitSuccess'));
    } catch (error: unknown) {
      setSubmitError(getApiErrorMessage(error, t('pages.myListingEdit.submitError')));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isCheckingSellerProfile || isLoading) {
    return (
      <Container className="account-page">
        <LoadingState
          description={t('pages.myListingEdit.loadingDescription')}
          title={t('pages.myListingEdit.loadingTitle')}
        />
      </Container>
    );
  }

  if (!hasActiveSellerProfile) {
    return (
      <Container className="account-page">
        <header className="marketplace-header">
          <h1>{t('pages.myListingEdit.title')}</h1>
          <p>{t('pages.myListingEdit.subtitle')}</p>
        </header>
        <SellerProfileRequiredState isInactive={Boolean(sellerProfile)} />
      </Container>
    );
  }

  if (loadError || !listing || !formState) {
    return (
      <Container className="account-page">
        <EmptyState
          action={
            <Link to={ROUTES.myListings}>
              <Button>{t('pages.myListingEdit.backToMyListings')}</Button>
            </Link>
          }
          description={loadError ?? t('pages.myListingEdit.loadError')}
          title={t('pages.myListingEdit.loadErrorTitle')}
        />
      </Container>
    );
  }

  return (
    <Container className="account-page">
      <header className="marketplace-header">
        <h1>{t('pages.myListingEdit.title')}</h1>
        <p>{t('pages.myListingEdit.subtitle')}</p>
      </header>

      <Card className="seller-listing-edit-summary" elevated>
        <div className="seller-listing-edit-summary-head">
          <h2>{listing.title}</h2>
          <Badge variant={getModerationBadgeVariant(moderationStatus)}>
            {t(`pages.myListings.status.${moderationStatus}`)}
          </Badge>
        </div>
        <p>
          {listing.author} • {listing.genre}
        </p>
        {listing.rejectionReason ? (
          <p className="account-listing-rejection">
            {t('pages.myListings.rejectionReasonLabel')}: {listing.rejectionReason}
          </p>
        ) : null}
        <p className="ui-input-hint">{t('pages.myListingEdit.reapprovalNotice')}</p>
      </Card>

      <Card className="seller-listing-form-card" elevated>
        <form className="seller-listing-form" onSubmit={handleSubmit}>
          <div className="seller-listing-book-create-grid">
            <Input
              label={t('pages.myListingEdit.priceLabel')}
              min={0.01}
              onChange={(event) => {
                setFormState((previousState) =>
                  previousState
                    ? {
                        ...previousState,
                        price: event.target.value,
                      }
                    : previousState,
                );
              }}
              step={0.01}
              type="number"
              value={formState.price}
            />
            <Input
              label={t('pages.myListingEdit.quantityLabel')}
              min={1}
              onChange={(event) => {
                setFormState((previousState) =>
                  previousState
                    ? {
                        ...previousState,
                        quantity: event.target.value,
                      }
                    : previousState,
                );
              }}
              type="number"
              value={formState.quantity}
            />
            <label className="ui-input-wrapper">
              <span className="ui-input-label">{t('pages.myListingEdit.conditionLabel')}</span>
              <select
                className="ui-input"
                onChange={(event) => {
                  setFormState((previousState) =>
                    previousState
                      ? {
                          ...previousState,
                          condition: event.target.value as MarketplaceListingCondition,
                        }
                      : previousState,
                  );
                }}
                value={formState.condition}
              >
                {CONDITION_OPTIONS.map((condition) => (
                  <option key={condition} value={condition}>
                    {t(`taxonomy.conditions.${condition}`)}
                  </option>
                ))}
              </select>
            </label>
            <label className="ui-input-wrapper seller-listing-grid-span-full">
              <span className="ui-input-label">{t('pages.myListingEdit.descriptionLabel')}</span>
              <textarea
                className="ui-input seller-listing-textarea"
                onChange={(event) => {
                  setFormState((previousState) =>
                    previousState
                      ? {
                          ...previousState,
                          description: event.target.value,
                        }
                      : previousState,
                  );
                }}
                value={formState.description}
              />
            </label>
            <label className="ui-input-wrapper seller-listing-grid-span-full">
              <span className="ui-input-label">{t('pages.myListingEdit.imageLabel')}</span>
              <input
                className="ui-input"
                onChange={(event) => {
                  const nextImage = event.target.files?.[0] ?? null;
                  setFormState((previousState) =>
                    previousState
                      ? {
                          ...previousState,
                          image: nextImage,
                          removeImage: nextImage ? false : previousState.removeImage,
                        }
                      : previousState,
                  );
                }}
                type="file"
              />
              <span className="ui-input-hint">{t('pages.myListingEdit.imageHint')}</span>
            </label>

            <label className="auth-remember seller-listing-grid-span-full">
              <input
                checked={formState.removeImage}
                onChange={(event) => {
                  setFormState((previousState) =>
                    previousState
                      ? {
                          ...previousState,
                          removeImage: event.target.checked,
                          image: event.target.checked ? null : previousState.image,
                        }
                      : previousState,
                  );
                }}
                type="checkbox"
              />
              <span>{t('pages.myListingEdit.removeImageLabel')}</span>
            </label>
          </div>

          {submitError ? <p className="auth-error">{submitError}</p> : null}
          {submitSuccess ? <p className="profile-success">{submitSuccess}</p> : null}

          <div className="profile-actions">
            <Button disabled={isSubmitting} type="submit">
              {isSubmitting ? t('pages.myListingEdit.submitting') : t('pages.myListingEdit.submit')}
            </Button>
            <Link to={ROUTES.myListings}>
              <Button variant="secondary">{t('pages.myListingEdit.backToMyListings')}</Button>
            </Link>
          </div>
        </form>
      </Card>
    </Container>
  );
}
