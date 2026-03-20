import type { FormEvent } from 'react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, FileUploadField, Input } from '@/components/ui';
import { AuthRouteNotice, type AuthRouteNoticeReason } from '@/features/auth/components/AuthRouteNotice';
import { authService } from '@/features/auth/services/auth.service';
import { ROUTES } from '@/routes/paths';
import { useAuthCapabilities } from '@/store/auth.store';

interface RegisterFormState {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  image: File | null;
}

export function RegisterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const capabilities = useAuthCapabilities();

  const [formState, setFormState] = useState<RegisterFormState>({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    image: null,
  });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof RegisterFormState, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const routeState = useMemo(() => location.state as { reason?: string } | null, [location.state]);
  const routeNoticeReason: AuthRouteNoticeReason | null =
    routeState?.reason === 'authRequired' || routeState?.reason === 'sessionExpired'
      ? routeState.reason
      : null;

  if (capabilities.isAuthenticated) {
    return <Navigate replace to={ROUTES.home} />;
  }

  const validateForm = (): boolean => {
    const nextFieldErrors: Partial<Record<keyof RegisterFormState, string>> = {};

    if (!formState.username.trim()) {
      nextFieldErrors.username = t('pages.register.validation.usernameRequired');
    }

    if (!formState.email.trim()) {
      nextFieldErrors.email = t('pages.register.validation.emailRequired');
    }

    if (formState.email && !/\S+@\S+\.\S+/.test(formState.email)) {
      nextFieldErrors.email = t('pages.register.validation.emailInvalid');
    }

    if (!formState.password) {
      nextFieldErrors.password = t('pages.register.validation.passwordRequired');
    }

    if (formState.password.length > 0 && formState.password.length < 6) {
      nextFieldErrors.password = t('pages.register.validation.passwordMinLength');
    }

    if (!formState.firstName.trim()) {
      nextFieldErrors.firstName = t('pages.register.validation.firstNameRequired');
    }

    if (!formState.lastName.trim()) {
      nextFieldErrors.lastName = t('pages.register.validation.lastNameRequired');
    }

    if (formState.confirmPassword !== formState.password) {
      nextFieldErrors.confirmPassword = t('pages.register.validation.passwordMismatch');
    }

    setFieldErrors(nextFieldErrors);
    return Object.keys(nextFieldErrors).length === 0;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setSubmitError(null);

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);

    try {
      await authService.register({
        username: formState.username.trim(),
        email: formState.email.trim(),
        password: formState.password,
        firstName: formState.firstName.trim(),
        lastName: formState.lastName.trim(),
        image: formState.image,
      });

      navigate(ROUTES.home, { replace: true });
    } catch (error: unknown) {
      setSubmitError(getApiErrorMessage(error, t('pages.register.errorGeneric')));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Container className="auth-page">
      <Card className="auth-card auth-card--register" elevated>
        <div className="auth-shell-head">
          <h1>{t('pages.register.title')}</h1>
          <p className="auth-shell-subtitle">{t('pages.register.subtitle')}</p>
        </div>

        <div className="auth-shell-body">
          {routeNoticeReason ? <AuthRouteNotice reason={routeNoticeReason} /> : null}

          <form aria-busy={isSubmitting} className="auth-form" data-submitting={isSubmitting ? 'true' : undefined} onSubmit={handleSubmit}>
            <div className="auth-form-fields">
              <Input
                autoComplete="username"
                error={fieldErrors.username}
                label={t('pages.register.usernameLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, username: event.target.value }));
                }}
                value={formState.username}
              />
              <Input
                autoComplete="email"
                error={fieldErrors.email}
                label={t('pages.register.emailLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, email: event.target.value }));
                }}
                type="email"
                value={formState.email}
              />
              <Input
                autoComplete="given-name"
                error={fieldErrors.firstName}
                label={t('pages.register.firstNameLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, firstName: event.target.value }));
                }}
                value={formState.firstName}
              />
              <Input
                autoComplete="family-name"
                error={fieldErrors.lastName}
                label={t('pages.register.lastNameLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, lastName: event.target.value }));
                }}
                value={formState.lastName}
              />
              <Input
                autoComplete="new-password"
                error={fieldErrors.password}
                label={t('pages.register.passwordLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, password: event.target.value }));
                }}
                type="password"
                value={formState.password}
              />
              <Input
                autoComplete="new-password"
                error={fieldErrors.confirmPassword}
                label={t('pages.register.confirmPasswordLabel')}
                onChange={(event) => {
                  setFormState((previousState) => ({ ...previousState, confirmPassword: event.target.value }));
                }}
                type="password"
                value={formState.confirmPassword}
              />

              <FileUploadField
                accept="image/*"
                file={formState.image}
                label={t('pages.register.imageLabel')}
                onFileChange={(nextImage) => {
                  setFormState((previousState) => ({
                    ...previousState,
                    image: nextImage,
                  }));
                }}
                showImagePreview
              />
            </div>

            {submitError ? <p className="auth-error auth-feedback">{submitError}</p> : null}

            <div className="auth-form-actions">
              <Button className="auth-submit-button" data-loading={isSubmitting ? 'true' : undefined} disabled={isSubmitting} fullWidth type="submit">
                {isSubmitting ? t('pages.register.submitting') : t('pages.register.submit')}
              </Button>
            </div>
          </form>
        </div>

        <div className="auth-shell-footer">
          <p className="auth-switch-text">
            {t('pages.register.switchPrompt')}{' '}
            <Link className="auth-switch-link" to={ROUTES.login}>
              {t('pages.register.switchAction')}
            </Link>
          </p>
        </div>
      </Card>
    </Container>
  );
}
