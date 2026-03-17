import type { FormEvent } from 'react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

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
  isActive: boolean;
}

const createInitialFormState = (): SellerProfileFormState => ({
  displayName: '',
  phoneNumber: '',
  supportsOnlinePayment: true,
  supportsCashOnDelivery: true,
  isActive: true,
});

const toFormState = (profile: {
  displayName: string;
  phoneNumber?: string | null;
  supportsOnlinePayment: boolean;
  supportsCashOnDelivery: boolean;
  isActive: boolean;
}): SellerProfileFormState => ({
  displayName: profile.displayName,
  phoneNumber: profile.phoneNumber ?? '',
  supportsOnlinePayment: profile.supportsOnlinePayment,
  supportsCashOnDelivery: profile.supportsCashOnDelivery,
  isActive: profile.isActive,
});

export function SellerProfilePage() {
  const { t } = useTranslation();
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
        isActive: formState.isActive,
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
      <Container className="profile-page">
        <LoadingState
          description={t('pages.sellerProfile.loadingDescription')}
          title={t('pages.sellerProfile.loadingTitle')}
        />
      </Container>
    );
  }

  if (loadState === 'error' && !profile) {
    return (
      <Container className="profile-page">
        <EmptyState
          action={<Button onClick={() => void loadMine(true)}>{t('common.actions.retry')}</Button>}
          description={t('pages.sellerProfile.loadError')}
          title={t('pages.sellerProfile.loadErrorTitle')}
        />
      </Container>
    );
  }

  return (
    <Container className="profile-page">
      <header className="marketplace-header">
        <h1>{t('pages.sellerProfile.title')}</h1>
        <p>{t('pages.sellerProfile.subtitle')}</p>
      </header>

      {!profile ? (
        <Card elevated>
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
          <p>{t('pages.sellerProfile.moderationHint')}</p>
          {profile.isActive ? (
            <Link to={ROUTES.myListings}>
              <Button size="sm" variant="secondary">
                {t('pages.sellerProfile.goToListings')}
              </Button>
            </Link>
          ) : null}
        </Card>
      )}

      <Card className="profile-edit-card" elevated>
        <h2>
          {profile ? t('pages.sellerProfile.editTitle') : t('pages.sellerProfile.createTitle')}
        </h2>
        <form className="profile-form" onSubmit={handleSubmit}>
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

          <div className="seller-profile-checkboxes">
            <label className="auth-remember">
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

            <label className="auth-remember">
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

            {fieldErrors.paymentMethods ? (
              <p className="auth-error">{fieldErrors.paymentMethods}</p>
            ) : null}
          </div>

          <label className="auth-remember">
            <input
              checked={formState.isActive}
              onChange={(event) => {
                setFormState((previousState) => ({
                  ...previousState,
                  isActive: event.target.checked,
                }));
              }}
              type="checkbox"
            />
            <span>{t('pages.sellerProfile.isActiveLabel')}</span>
          </label>

          {submitError ? <p className="auth-error">{submitError}</p> : null}
          {submitSuccess ? <p className="profile-success">{submitSuccess}</p> : null}

          <div className="profile-actions">
            <Button disabled={isSaving} type="submit">
              {isSaving ? t('pages.sellerProfile.saving') : t('pages.sellerProfile.save')}
            </Button>
          </div>
        </form>
      </Card>
    </Container>
  );
}
