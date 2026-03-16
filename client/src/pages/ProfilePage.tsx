import type { FormEvent } from 'react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, EmptyState, Input, LoadingState } from '@/components/ui';
import { profileApi, type ProfileResponse } from '@/features/auth/api/profile.api';
import { useAuthStore } from '@/store/auth.store';

interface ProfileFormState {
  firstName: string;
  lastName: string;
  image: File | null;
  removeImage: boolean;
}

const getInitials = (firstName: string, lastName: string): string => {
  const firstInitial = firstName.trim().charAt(0);
  const lastInitial = lastName.trim().charAt(0);
  return `${firstInitial}${lastInitial}`.toUpperCase() || 'BS';
};

export function ProfilePage() {
  const { t } = useTranslation();
  const session = useAuthStore((state) => state.session);
  const setSession = useAuthStore((state) => state.setSession);

  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [formState, setFormState] = useState<ProfileFormState>({
    firstName: '',
    lastName: '',
    image: null,
    removeImage: false,
  });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<'firstName' | 'lastName', string>>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitSuccess, setSubmitSuccess] = useState<string | null>(null);
  const [reloadCounter, setReloadCounter] = useState(0);

  const profileDisplayName = useMemo(
    () => `${profile?.firstName ?? ''} ${profile?.lastName ?? ''}`.trim() || session?.user.displayName || '-',
    [profile?.firstName, profile?.lastName, session?.user.displayName],
  );

  useEffect(() => {
    let isActive = true;

    const loadProfile = async (): Promise<void> => {
      setIsLoading(true);
      setLoadError(null);

      try {
        const response = await profileApi.getMine();
        if (!isActive) {
          return;
        }

        setProfile(response);
        setFormState({
          firstName: response.firstName,
          lastName: response.lastName,
          image: null,
          removeImage: false,
        });
      } catch (error: unknown) {
        if (!isActive) {
          return;
        }

        setLoadError(getApiErrorMessage(error, t('pages.profile.loadError')));
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    void loadProfile();

    return () => {
      isActive = false;
    };
  }, [reloadCounter, t]);

  const validateForm = (): boolean => {
    const nextFieldErrors: Partial<Record<'firstName' | 'lastName', string>> = {};

    if (!formState.firstName.trim()) {
      nextFieldErrors.firstName = t('pages.profile.validation.firstNameRequired');
    }

    if (!formState.lastName.trim()) {
      nextFieldErrors.lastName = t('pages.profile.validation.lastNameRequired');
    }

    setFieldErrors(nextFieldErrors);
    return Object.keys(nextFieldErrors).length === 0;
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
      await profileApi.updateMine({
        firstName: formState.firstName.trim(),
        lastName: formState.lastName.trim(),
        image: formState.image,
        removeImage: formState.removeImage,
      });

      const updatedProfile = await profileApi.getMine();
      setProfile(updatedProfile);
      setFormState({
        firstName: updatedProfile.firstName,
        lastName: updatedProfile.lastName,
        image: null,
        removeImage: false,
      });

      if (session) {
        setSession({
          ...session,
          user: {
            ...session.user,
            displayName: `${updatedProfile.firstName} ${updatedProfile.lastName}`.trim(),
          },
        });
      }

      setSubmitSuccess(t('pages.profile.updateSuccess'));
    } catch (error: unknown) {
      setSubmitError(getApiErrorMessage(error, t('pages.profile.updateError')));
    } finally {
      setIsSaving(false);
    }
  };

  const handleReset = (): void => {
    if (!profile) {
      return;
    }

    setSubmitError(null);
    setSubmitSuccess(null);
    setFieldErrors({});
    setFormState({
      firstName: profile.firstName,
      lastName: profile.lastName,
      image: null,
      removeImage: false,
    });
  };

  if (isLoading) {
    return (
      <Container className="profile-page">
        <LoadingState description={t('pages.profile.loadingDescription')} title={t('pages.profile.loadingTitle')} />
      </Container>
    );
  }

  if (loadError || !profile) {
    return (
      <Container className="profile-page">
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
          description={loadError ?? t('pages.profile.emptyDescription')}
          title={t('pages.profile.emptyTitle')}
        />
      </Container>
    );
  }

  return (
    <Container className="profile-page">
      <header className="marketplace-header">
        <h1>{t('pages.profile.title')}</h1>
        <p>{t('pages.profile.subtitle')}</p>
      </header>

      <section className="profile-layout">
        <Card className="profile-summary-card" elevated>
          {profile.imageUrl && !formState.removeImage ? (
            <img alt={t('pages.profile.imageAlt', { name: profileDisplayName })} className="profile-avatar" src={profile.imageUrl} />
          ) : (
            <div className="profile-avatar profile-avatar--fallback">{getInitials(profile.firstName, profile.lastName)}</div>
          )}
          <div className="profile-summary-content">
            <h2>{profileDisplayName}</h2>
            <dl className="profile-summary-meta">
              <dt>{t('pages.profile.emailLabel')}</dt>
              <dd>{session?.user.email ?? '-'}</dd>
              <dt>{t('pages.profile.roleLabel')}</dt>
              <dd>{t(`pages.profile.roles.${session?.user.role ?? 'buyer'}`)}</dd>
            </dl>
          </div>
        </Card>

        <Card className="profile-edit-card" elevated>
          <h2>{t('pages.profile.editTitle')}</h2>
          <form className="profile-form" onSubmit={handleSubmit}>
            <Input
              autoComplete="given-name"
              error={fieldErrors.firstName}
              label={t('pages.profile.firstNameLabel')}
              onChange={(event) => {
                setFormState((previousState) => ({ ...previousState, firstName: event.target.value }));
              }}
              value={formState.firstName}
            />
            <Input
              autoComplete="family-name"
              error={fieldErrors.lastName}
              label={t('pages.profile.lastNameLabel')}
              onChange={(event) => {
                setFormState((previousState) => ({ ...previousState, lastName: event.target.value }));
              }}
              value={formState.lastName}
            />

            <label className="ui-input-wrapper">
              <span className="ui-input-label">{t('pages.profile.imageUploadLabel')}</span>
              <input
                className="ui-input"
                onChange={(event) => {
                  const nextImage = event.target.files?.[0] ?? null;
                  setFormState((previousState) => ({
                    ...previousState,
                    image: nextImage,
                    removeImage: nextImage ? false : previousState.removeImage,
                  }));
                }}
                type="file"
              />
              <span className="ui-input-hint">{t('pages.profile.imageUploadHint')}</span>
            </label>

            <label className="auth-remember">
              <input
                checked={formState.removeImage}
                onChange={(event) => {
                  setFormState((previousState) => ({
                    ...previousState,
                    removeImage: event.target.checked,
                    image: event.target.checked ? null : previousState.image,
                  }));
                }}
                type="checkbox"
              />
              <span>{t('pages.profile.removeImageLabel')}</span>
            </label>

            {submitError ? <p className="auth-error">{submitError}</p> : null}
            {submitSuccess ? <p className="profile-success">{submitSuccess}</p> : null}

            <div className="profile-actions">
              <Button disabled={isSaving} type="submit">
                {isSaving ? t('pages.profile.saving') : t('pages.profile.save')}
              </Button>
              <Button disabled={isSaving} onClick={handleReset} type="button" variant="secondary">
                {t('common.actions.reset')}
              </Button>
            </div>
          </form>
        </Card>
      </section>
    </Container>
  );
}
