import type { FormEvent } from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, Navigate, useNavigate } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, Input } from '@/components/ui';
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
      <Card className="auth-card" elevated>
        <h1>{t('pages.register.title')}</h1>
        <p>{t('pages.register.subtitle')}</p>

        <form className="auth-form" onSubmit={handleSubmit}>
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

          <label className="ui-input-wrapper">
            <span className="ui-input-label">{t('pages.register.imageLabel')}</span>
            <input
              className="ui-input"
              onChange={(event) => {
                setFormState((previousState) => ({
                  ...previousState,
                  image: event.target.files?.[0] ?? null,
                }));
              }}
              type="file"
            />
          </label>

          {submitError ? <p className="auth-error">{submitError}</p> : null}

          <Button disabled={isSubmitting} fullWidth type="submit">
            {isSubmitting ? t('pages.register.submitting') : t('pages.register.submit')}
          </Button>
        </form>

        <p className="auth-switch-text">
          {t('pages.register.switchPrompt')}{' '}
          <Link className="auth-switch-link" to={ROUTES.login}>
            {t('pages.register.switchAction')}
          </Link>
        </p>
      </Card>
    </Container>
  );
}
