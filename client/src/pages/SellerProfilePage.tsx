import type { FormEvent } from 'react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Badge, Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { sellerProfilesApi } from '@/features/sellerProfiles/api/sellerProfiles.api';
import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { ROUTES } from '@/routes/paths';

interface SellerProfileFormState {
  displayName: string;
  phoneNumber: string;
  supportsOnlinePayment: boolean;
  supportsCashOnDelivery: boolean;
}

const createInitialFormState = (): SellerProfileFormState => ({
  displayName: '',
  phoneNumber: '',
  supportsOnlinePayment: true,
  supportsCashOnDelivery: true,
});

const toFormState = (profile: {
  displayName: string;
  phoneNumber?: string | null;
  supportsOnlinePayment: boolean;
  supportsCashOnDelivery: boolean;
}): SellerProfileFormState => ({
  displayName: profile.displayName,
  phoneNumber: profile.phoneNumber ?? '',
  supportsOnlinePayment: profile.supportsOnlinePayment,
  supportsCashOnDelivery: profile.supportsCashOnDelivery,
});

export function SellerProfilePage() {
  const { t } = useTranslation();
  const location = useLocation();
  const profile = useSellerProfileStore((state) => state.profile);
  const loadState = useSellerProfileStore((state) => state.loadState);
  const loadMine = useSellerProfileStore((state) => state.loadMine);
  const setProfile = useSellerProfileStore((state) => state.setProfile);

  const [formState, setFormState] = useState<SellerProfileFormState>(createInitialFormState);
  const [fieldErrors, setFieldErrors] = useState<
    Partial<Record<'displayName' | 'paymentMethods', string>>
  >({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const routeState = useMemo(
    () => location.state as { from?: string; reason?: string } | null,
    [location.state],
  );
  const showSellerAccessNotice = routeState?.reason === 'sellerProfileRequired';

  useEffect(() => {
    void loadMine();
  }, [loadMine]);

  useEffect(() => {
    if (!profile) {
      setFormState(createInitialFormState());
      return;
    }

    setFormState(toFormState(profile));
  }, [profile]);

  const validateForm = (): boolean => {
    const nextErrors: Partial<Record<'displayName' | 'paymentMethods', string>> = {};
    const trimmedDisplayName = formState.displayName.trim();

    if (trimmedDisplayName.length < 2) {
      nextErrors.displayName = t('pages.sellerProfile.validation.displayNameMin');
    }

    if (!formState.supportsOnlinePayment && !formState.supportsCashOnDelivery) {
      nextErrors.paymentMethods = t('pages.sellerProfile.validation.atLeastOnePaymentMethod');
    }

    setFieldErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setSubmitError(null);
    setSubmitSuccess(null);

    if (!validateForm()) {
      return;
    }

    setIsSaving(true);

    try {
      const updatedProfile = await sellerProfilesApi.upsertMine({
        displayName: formState.displayName,
        phoneNumber: formState.phoneNumber,
        supportsOnlinePayment: formState.supportsOnlinePayment,
        supportsCashOnDelivery: formState.supportsCashOnDelivery,
      });

      setProfile(updatedProfile);
      setSubmitSuccess(t('pages.sellerProfile.saveSuccess'));
    } catch (error: unknown) {
      setSubmitError(getApiErrorMessage(error, t('pages.sellerProfile.saveError')));
    } finally {
      setIsSaving(false);
    }
  };

  const isLoading = (loadState === 'loading' || loadState === 'idle') && !profile;

  if (isLoading) {
    return (
      <Container className="profile-page seller-profile-page">
        <LoadingState
          description={t('pages.sellerProfile.loadingDescription')}
          title={t('pages.sellerProfile.loadingTitle')}
        />
      </Container>
    );
  }

  if (loadState === 'error' && !profile) {
    return (
      <Container className="profile-page seller-profile-page">
        <EmptyState
          action={<Button onClick={() => void loadMine(true)}>{t('common.actions.retry')}</Button>}
          description={t('pages.sellerProfile.loadError')}
          title={t('pages.sellerProfile.loadErrorTitle')}
        />
      </Container>
    );
  }

  return (
    <Container className="profile-page seller-profile-page">
      <header className="marketplace-header seller-profile-header">
        <h1>{t('pages.sellerProfile.title')}</h1>
        <p>{t('pages.sellerProfile.subtitle')}</p>
      </header>
      {showSellerAccessNotice ? (
        <Card className="route-access-card route-access-card--blocked route-access-card--seller" elevated>
          <div className="route-access-card-head">
            <Badge className="route-access-card-badge" variant="warning">
              {t('pages.routeAccess.sellerBlockedBadge')}
            </Badge>
            <h2>{t('pages.routeAccess.sellerBlockedTitle')}</h2>
            <p>{t('pages.routeAccess.sellerBlockedDescription')}</p>
          </div>
          <div className="route-access-card-note">
            <p>{t('pages.routeAccess.sellerBlockedHint')}</p>
          </div>
          <div className="route-access-card-actions">
            <Link to={ROUTES.marketplace}>
              <Button>{t('common.actions.browseMarketplace')}</Button>
            </Link>
            <a href="#seller-profile-form">
              <Button variant="secondary">{t('pages.routeAccess.sellerBlockedSecondaryAction')}</Button>
            </a>
          </div>
        </Card>
      ) : null}

      {!profile ? (
        <Card className="seller-profile-onboarding-card" elevated>
          <h2>{t('pages.sellerProfile.onboardingTitle')}</h2>
          <p>{t('pages.sellerProfile.onboardingDescription')}</p>
        </Card>
      ) : (
        <Card className="seller-profile-summary-card" elevated>
          <div className="seller-profile-summary-head">
            <h2>{profile.displayName}</h2>
            <Badge variant={profile.isActive ? 'success' : 'warning'}>
              {profile.isActive
                ? t('pages.sellerProfile.activeBadge')
                : t('pages.sellerProfile.inactiveBadge')}
            </Badge>
          </div>
          <p className="seller-profile-summary-hint">{t('pages.sellerProfile.moderationHint')}</p>
          {profile.isActive ? (
            <Link className="seller-profile-summary-action" to={ROUTES.myListings}>
              <Button size="sm" variant="secondary">
                {t('pages.sellerProfile.goToListings')}
              </Button>
            </Link>
          ) : null}
        </Card>
      )}

      <Card className="profile-edit-card seller-profile-form-card" elevated id="seller-profile-form">
        <div className="seller-profile-form-head">
          <h2>
            {profile ? t('pages.sellerProfile.editTitle') : t('pages.sellerProfile.createTitle')}
          </h2>
          <p className="ui-input-hint seller-profile-status-hint">
            {t('pages.sellerProfile.statusControlledByAdmins')}
          </p>
        </div>
        <form className="profile-form seller-profile-form" onSubmit={handleSubmit}>
          <div className="seller-profile-fields">
            <Input
              error={fieldErrors.displayName}
              label={t('pages.sellerProfile.displayNameLabel')}
              maxLength={150}
              onChange={(event) => {
                setFormState((previousState) => ({
                  ...previousState,
                  displayName: event.target.value,
                }));
              }}
              value={formState.displayName}
            />
            <Input
              label={t('pages.sellerProfile.phoneNumberLabel')}
              maxLength={30}
              onChange={(event) => {
                setFormState((previousState) => ({
                  ...previousState,
                  phoneNumber: event.target.value,
                }));
              }}
              value={formState.phoneNumber}
            />
          </div>

          <section className="seller-profile-payment-section">
            <div className="seller-profile-checkboxes">
              <label className="auth-remember seller-profile-checkbox">
                <input
                  checked={formState.supportsOnlinePayment}
                  onChange={(event) => {
                    setFormState((previousState) => ({
                      ...previousState,
                      supportsOnlinePayment: event.target.checked,
                    }));
                  }}
                  type="checkbox"
                />
                <span>{t('pages.sellerProfile.supportsOnlinePaymentLabel')}</span>
              </label>

              <label className="auth-remember seller-profile-checkbox">
                <input
                  checked={formState.supportsCashOnDelivery}
                  onChange={(event) => {
                    setFormState((previousState) => ({
                      ...previousState,
                      supportsCashOnDelivery: event.target.checked,
                    }));
                  }}
                  type="checkbox"
                />
                <span>{t('pages.sellerProfile.supportsCashOnDeliveryLabel')}</span>
              </label>
            </div>

            {fieldErrors.paymentMethods ? (
              <p className="auth-error seller-profile-feedback seller-profile-feedback--error">
                {fieldErrors.paymentMethods}
              </p>
            ) : null}
          </section>

          {submitError ? (
            <p className="auth-error seller-profile-feedback seller-profile-feedback--error">
              {submitError}
            </p>
          ) : null}
          {submitSuccess ? (
            <p className="profile-success seller-profile-feedback seller-profile-feedback--success">
              {submitSuccess}
            </p>
          ) : null}

          <div className="profile-actions seller-profile-actions">
            <Button disabled={isSaving} type="submit">
              {isSaving ? t('pages.sellerProfile.saving') : t('pages.sellerProfile.save')}
            </Button>
          </div>
        </form>
      </Card>
    </Container>
  );
}
