import type { FormEvent } from 'react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { profileApi } from '@/features/auth/api/profile.api';
import { checkoutService } from '@/features/checkout/services/checkout.service';
import { mapCheckoutSubmissionToCreateOrderRequest } from '@/features/checkout/utils/checkoutPayload';
import { multiplyPriceDisplayValue } from '@/features/checkout/utils/priceMath';
import { listingsApi } from '@/features/marketplace/api/listings.api';
import type { PaymentMethod } from '@/features/orders/types';
import { getOrderConfirmationRoute, ROUTES } from '@/routes/paths';
import { useAuthCapabilities, useAuthStore } from '@/store/auth.store';
import type { MarketplaceListing } from '@/types/marketplace.types';
import { redirectTo } from '@/utils/navigation';

interface CheckoutFormState {
  customerFirstName: string;
  customerLastName: string;
  email: string;
  phoneNumber: string;
  country: string;
  city: string;
  addressLine: string;
  postalCode: string;
}

interface SupportedPaymentMethods {
  online: boolean;
  cashOnDelivery: boolean;
  hasValidData: boolean;
}

const parseQuantity = (value: string | null): number => {
  const parsedValue = Number.parseInt(value ?? '', 10);
  if (Number.isNaN(parsedValue) || parsedValue < 1) {
    return 1;
  }

  return parsedValue;
};

const parseDisplayName = (displayName: string | undefined): Pick<CheckoutFormState, 'customerFirstName' | 'customerLastName'> => {
  if (!displayName) {
    return {
      customerFirstName: '',
      customerLastName: '',
    };
  }

  const parts = displayName.trim().split(/\s+/);
  if (parts.length === 0) {
    return {
      customerFirstName: '',
      customerLastName: '',
    };
  }

  if (parts.length === 1) {
    return {
      customerFirstName: parts[0] ?? '',
      customerLastName: '',
    };
  }

  return {
    customerFirstName: parts[0] ?? '',
    customerLastName: parts.slice(1).join(' '),
  };
};

const resolveSupportedPaymentMethods = (listing: MarketplaceListing | null): SupportedPaymentMethods => {
  if (!listing) {
    return {
      online: false,
      cashOnDelivery: false,
      hasValidData: false,
    };
  }

  const hasExplicitSupportData =
    typeof listing.supportsOnlinePayment === 'boolean' &&
    typeof listing.supportsCashOnDelivery === 'boolean';

  if (!hasExplicitSupportData) {
    return {
      online: false,
      cashOnDelivery: false,
      hasValidData: false,
    };
  }

  return {
    online: Boolean(listing.supportsOnlinePayment),
    cashOnDelivery: Boolean(listing.supportsCashOnDelivery),
    hasValidData: true,
  };
};

export function CheckoutPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const capabilities = useAuthCapabilities();
  const session = useAuthStore((state) => state.session);

  const listingId = searchParams.get('listingId') ?? '';
  const initialQuantity = parseQuantity(searchParams.get('quantity'));

  const [listing, setListing] = useState<MarketplaceListing | null>(null);
  const [quantity, setQuantity] = useState(initialQuantity);
  const [isLoadingListing, setIsLoadingListing] = useState(true);
  const [listingError, setListingError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const displayNameParts = useMemo(() => parseDisplayName(session?.user.displayName), [session?.user.displayName]);

  const [formState, setFormState] = useState<CheckoutFormState>({
    customerFirstName: displayNameParts.customerFirstName,
    customerLastName: displayNameParts.customerLastName,
    email: session?.user.email ?? '',
    phoneNumber: '',
    country: '',
    city: '',
    addressLine: '',
    postalCode: '',
  });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof CheckoutFormState, string>>>({});
  const [selectedPaymentMethod, setSelectedPaymentMethod] = useState<PaymentMethod | null>(null);
  const [paymentMethodError, setPaymentMethodError] = useState<string | null>(null);

  const supportedPaymentMethods = useMemo(() => resolveSupportedPaymentMethods(listing), [listing]);
  const availablePaymentMethods = useMemo<PaymentMethod[]>(() => {
    const methods: PaymentMethod[] = [];
    if (supportedPaymentMethods.online) {
      methods.push('online');
    }

    if (supportedPaymentMethods.cashOnDelivery) {
      methods.push('cashOnDelivery');
    }

    return methods;
  }, [supportedPaymentMethods.cashOnDelivery, supportedPaymentMethods.online]);

  useEffect(() => {
    if (!listingId) {
      setIsLoadingListing(false);
      setListingError(t('pages.checkout.invalidSelectionDescription'));
      return;
    }

    let isActive = true;

    const loadListing = async (): Promise<void> => {
      setIsLoadingListing(true);
      setListingError(null);

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

        setListing(null);
        setListingError(getApiErrorMessage(error, t('pages.checkout.loadListingError')));
      } finally {
        if (isActive) {
          setIsLoadingListing(false);
        }
      }
    };

    void loadListing();

    return () => {
      isActive = false;
    };
  }, [listingId, t]);

  useEffect(() => {
    if (!listing || !capabilities.isAuthenticated) {
      return;
    }

    let isActive = true;

    const prefillFromProfile = async (): Promise<void> => {
      try {
        const profile = await profileApi.getMine();
        if (!isActive) {
          return;
        }

        setFormState((previousState) => ({
          ...previousState,
          customerFirstName: previousState.customerFirstName || profile.firstName,
          customerLastName: previousState.customerLastName || profile.lastName,
        }));
      } catch {
        // Profile prefill is best effort and should not block checkout.
      }
    };

    void prefillFromProfile();

    return () => {
      isActive = false;
    };
  }, [capabilities.isAuthenticated, listing]);

  useEffect(() => {
    if (!listing) {
      return;
    }

    setQuantity((previousQuantity) => Math.min(Math.max(previousQuantity, 1), Math.max(listing.quantity, 1)));
  }, [listing]);

  useEffect(() => {
    if (availablePaymentMethods.length === 0) {
      setSelectedPaymentMethod(null);
      return;
    }

    if (availablePaymentMethods.length === 1) {
      setSelectedPaymentMethod(availablePaymentMethods[0] ?? null);
      return;
    }

    setSelectedPaymentMethod((currentValue) => {
      if (currentValue && availablePaymentMethods.includes(currentValue)) {
        return currentValue;
      }

      return availablePaymentMethods[0] ?? null;
    });
  }, [availablePaymentMethods]);

  const unitPrice = listing?.price;
  const totalPrice = useMemo(() => {
    if (!unitPrice) {
      return null;
    }

    return multiplyPriceDisplayValue(unitPrice, quantity);
  }, [quantity, unitPrice]);

  const canPurchase = Boolean(listing && listing.isApproved && listing.quantity > 0);
  const hasSupportedPaymentMethods = availablePaymentMethods.length > 0;
  const hasInvalidPaymentSupportData = Boolean(listing) && !supportedPaymentMethods.hasValidData;
  const isInvalidState = !isLoadingListing && (!listing || !canPurchase);

  const validateForm = (): boolean => {
    const nextFieldErrors: Partial<Record<keyof CheckoutFormState, string>> = {};

    if (!formState.customerFirstName.trim()) {
      nextFieldErrors.customerFirstName = t('pages.checkout.validation.firstNameRequired');
    }

    if (!formState.customerLastName.trim()) {
      nextFieldErrors.customerLastName = t('pages.checkout.validation.lastNameRequired');
    }

    if (!formState.email.trim()) {
      nextFieldErrors.email = t('pages.checkout.validation.emailRequired');
    } else if (!/\S+@\S+\.\S+/.test(formState.email.trim())) {
      nextFieldErrors.email = t('pages.checkout.validation.emailInvalid');
    }

    if (!formState.country.trim()) {
      nextFieldErrors.country = t('pages.checkout.validation.countryRequired');
    }

    if (!formState.city.trim()) {
      nextFieldErrors.city = t('pages.checkout.validation.cityRequired');
    }

    if (!formState.addressLine.trim()) {
      nextFieldErrors.addressLine = t('pages.checkout.validation.addressLineRequired');
    }

    setFieldErrors(nextFieldErrors);
    return Object.keys(nextFieldErrors).length === 0;
  };

  const handleCheckoutSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setSubmitError(null);
    setPaymentMethodError(null);

    if (!listing || !canPurchase || !validateForm()) {
      return;
    }

    if (!selectedPaymentMethod) {
      setPaymentMethodError(t('pages.checkout.validation.paymentMethodRequired'));
      return;
    }

    if (!hasSupportedPaymentMethods) {
      setPaymentMethodError(t('pages.checkout.validation.noSupportedPaymentMethod'));
      return;
    }

    setIsSubmitting(true);

    try {
      const orderPayload = mapCheckoutSubmissionToCreateOrderRequest({
        ...formState,
        paymentMethod: selectedPaymentMethod,
        items: [{ listingId: listing.id, quantity }],
      });

      const checkoutResult = await checkoutService.createOrderAndStartCheckout(orderPayload);

      if (checkoutResult.paymentMethod === 'online') {
        redirectTo(checkoutResult.checkoutUrl);
        return;
      }

      redirectTo(getOrderConfirmationRoute(checkoutResult.orderId, 'cashOnDelivery'));
    } catch (error: unknown) {
      setSubmitError(getApiErrorMessage(error, t('pages.checkout.submitError')));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoadingListing) {
    return (
      <Container className="checkout-page">
        <LoadingState
          description={t('pages.checkout.loadingDescription')}
          title={t('pages.checkout.loadingTitle')}
        />
      </Container>
    );
  }

  if (isInvalidState) {
    return (
      <Container className="checkout-page">
        <EmptyState
          action={
            <Link to={ROUTES.marketplace}>
              <Button>{t('common.actions.backToMarketplace')}</Button>
            </Link>
          }
          description={listingError ?? t('pages.checkout.invalidSelectionDescription')}
          title={t('pages.checkout.invalidSelectionTitle')}
        />
      </Container>
    );
  }

  return (
    <Container className="checkout-page">
      <header className="marketplace-header">
        <h1>{t('pages.checkout.title')}</h1>
        <p>{t('pages.checkout.subtitle')}</p>
      </header>

      {!capabilities.isAuthenticated ? (
        <Card className="checkout-auth-hint">
          <p>{t('pages.checkout.guestNotice')}</p>
          <Link to={ROUTES.login}>
            <Button size="sm" variant="secondary">
              {t('pages.checkout.loginCta')}
            </Button>
          </Link>
        </Card>
      ) : null}

      <div className="checkout-layout">
        <Card className="checkout-form-card" elevated>
          <h2>{t('pages.checkout.contactSectionTitle')}</h2>
          <form className="checkout-form" onSubmit={handleCheckoutSubmit}>
            <div className="checkout-form-grid">
              <Input
                autoComplete="given-name"
                error={fieldErrors.customerFirstName}
                label={t('pages.checkout.firstNameLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, customerFirstName: event.target.value }));
                }}
                value={formState.customerFirstName}
              />
              <Input
                autoComplete="family-name"
                error={fieldErrors.customerLastName}
                label={t('pages.checkout.lastNameLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, customerLastName: event.target.value }));
                }}
                value={formState.customerLastName}
              />
              <Input
                autoComplete="email"
                error={fieldErrors.email}
                label={t('pages.checkout.emailLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, email: event.target.value }));
                }}
                type="email"
                value={formState.email}
              />
              <Input
                autoComplete="tel"
                label={t('pages.checkout.phoneLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, phoneNumber: event.target.value }));
                }}
                value={formState.phoneNumber}
              />
              <Input
                autoComplete="country-name"
                error={fieldErrors.country}
                label={t('pages.checkout.countryLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, country: event.target.value }));
                }}
                value={formState.country}
              />
              <Input
                autoComplete="address-level2"
                error={fieldErrors.city}
                label={t('pages.checkout.cityLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, city: event.target.value }));
                }}
                value={formState.city}
              />
              <Input
                autoComplete="address-line1"
                className="checkout-address-input"
                error={fieldErrors.addressLine}
                label={t('pages.checkout.addressLineLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, addressLine: event.target.value }));
                }}
                value={formState.addressLine}
              />
              <Input
                autoComplete="postal-code"
                label={t('pages.checkout.postalCodeLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, postalCode: event.target.value }));
                }}
                value={formState.postalCode}
              />
            </div>

            <div className="checkout-payment-methods">
              <h3>{t('pages.checkout.paymentMethodTitle')}</h3>
              <p className="checkout-summary-meta">{t('pages.checkout.paymentMethodSubtitle')}</p>

              {hasInvalidPaymentSupportData ? (
                <p className="auth-error">{t('pages.checkout.paymentMethodConfigurationError')}</p>
              ) : null}

              {!hasSupportedPaymentMethods ? (
                <p className="auth-error">{t('pages.checkout.noSupportedPaymentMethodsMessage')}</p>
              ) : (
                <div className="checkout-payment-options">
                  {availablePaymentMethods.map((paymentMethod) => (
                    <label className="checkout-payment-option" key={paymentMethod}>
                      <input
                        checked={selectedPaymentMethod === paymentMethod}
                        name="paymentMethod"
                        onChange={() => {
                          setSelectedPaymentMethod(paymentMethod);
                          setPaymentMethodError(null);
                        }}
                        type="radio"
                        value={paymentMethod}
                      />
                      <span>
                        <strong>{t(`pages.checkout.paymentMethods.${paymentMethod}.title`)}</strong>
                        <small>{t(`pages.checkout.paymentMethods.${paymentMethod}.description`)}</small>
                      </span>
                    </label>
                  ))}
                </div>
              )}
            </div>

            {submitError ? <p className="auth-error">{submitError}</p> : null}
            {paymentMethodError ? <p className="auth-error">{paymentMethodError}</p> : null}

            <Button disabled={isSubmitting || !hasSupportedPaymentMethods} type="submit">
              {isSubmitting ? t('pages.checkout.submitting') : t('pages.checkout.submit')}
            </Button>
          </form>
        </Card>

        <Card className="checkout-summary-card" elevated>
          <h2>{t('pages.checkout.summaryTitle')}</h2>
          {listing ? (
            <div className="checkout-summary-item">
              <p className="checkout-summary-title">{listing.title}</p>
              <p className="checkout-summary-meta">{listing.author}</p>
              <p className="checkout-summary-meta">{listing.genre}</p>

              <div className="checkout-quantity-control">
                <span>{t('pages.checkout.quantityLabel')}</span>
                <div className="checkout-quantity-actions">
                  <Button
                    disabled={quantity <= 1}
                    onClick={() => {
                      setQuantity((previousQuantity) => Math.max(previousQuantity - 1, 1));
                    }}
                    size="sm"
                    variant="ghost"
                  >
                    -
                  </Button>
                  <Input
                    className="checkout-quantity-input"
                    min={1}
                    onChange={(event) => {
                      const nextQuantity = Number.parseInt(event.target.value, 10);
                      if (Number.isNaN(nextQuantity)) {
                        setQuantity(1);
                        return;
                      }

                      setQuantity(Math.min(Math.max(nextQuantity, 1), listing.quantity));
                    }}
                    type="number"
                    value={String(quantity)}
                  />
                  <Button
                    disabled={quantity >= listing.quantity}
                    onClick={() => {
                      setQuantity((previousQuantity) => Math.min(previousQuantity + 1, listing.quantity));
                    }}
                    size="sm"
                    variant="ghost"
                  >
                    +
                  </Button>
                </div>
                <p className="checkout-summary-meta">
                  {t('pages.checkout.availableQuantity', { count: listing.quantity })}
                </p>
              </div>

              {selectedPaymentMethod ? (
                <div className="checkout-payment-summary">
                  <p className="checkout-summary-meta">{t('pages.checkout.paymentMethodSummaryLabel')}</p>
                  <p className="checkout-summary-title">
                    {t(`pages.checkout.paymentMethods.${selectedPaymentMethod}.title`)}
                  </p>
                  <p className="checkout-summary-meta">
                    {t(`pages.checkout.paymentMethods.${selectedPaymentMethod}.description`)}
                  </p>
                </div>
              ) : null}

              <div className="checkout-summary-price-line">
                <span>{t('pages.checkout.unitPriceLabel')}</span>
                <PriceDisplay value={listing.price} />
              </div>
              {totalPrice ? (
                <div className="checkout-summary-price-line checkout-summary-price-line--total">
                  <span>{t('pages.checkout.totalPriceLabel')}</span>
                  <PriceDisplay value={totalPrice} />
                </div>
              ) : null}
            </div>
          ) : null}
        </Card>
      </div>
    </Container>
  );
}
