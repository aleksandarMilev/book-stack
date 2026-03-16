import type { FormEvent } from 'react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, Input } from '@/components/ui';
import { authService } from '@/features/auth/services/auth.service';
import { ROUTES } from '@/routes/paths';
import { useAuthCapabilities } from '@/store/auth.store';

interface LoginFormState {
  credentials: string;
  password: string;
  rememberMe: boolean;
}

export function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const capabilities = useAuthCapabilities();

  const [formState, setFormState] = useState<LoginFormState>({
    credentials: '',
    password: '',
    rememberMe: true,
  });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof LoginFormState, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const redirectTo = useMemo(() => {
    const state = location.state as { from?: string } | null;
    return state?.from ?? ROUTES.home;
  }, [location.state]);

  if (capabilities.isAuthenticated) {
    return <Navigate replace to={redirectTo} />;
  }

  const validateForm = (): boolean => {
    const nextFieldErrors: Partial<Record<keyof LoginFormState, string>> = {};

    if (!formState.credentials.trim()) {
      nextFieldErrors.credentials = t('pages.login.validation.credentialsRequired');
    }

    if (!formState.password) {
      nextFieldErrors.password = t('pages.login.validation.passwordRequired');
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
      await authService.login(formState.credentials.trim(), formState.password, formState.rememberMe);
      navigate(redirectTo, { replace: true });
    } catch (error: unknown) {
      setSubmitError(getApiErrorMessage(error, t('pages.login.errorGeneric')));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Container className="auth-page">
      <Card className="auth-card" elevated>
        <h1>{t('pages.login.title')}</h1>
        <p>{t('pages.login.subtitle')}</p>

        <form className="auth-form" onSubmit={handleSubmit}>
          <Input
            autoComplete="username"
            error={fieldErrors.credentials}
            label={t('pages.login.credentialsLabel')}
            name="credentials"
            onChange={(event) => {
              setFormState((previousState) => ({ ...previousState, credentials: event.target.value }));
            }}
            placeholder={t('pages.login.credentialsPlaceholder')}
            value={formState.credentials}
          />
          <Input
            autoComplete="current-password"
            error={fieldErrors.password}
            label={t('pages.login.passwordLabel')}
            name="password"
            onChange={(event) => {
              setFormState((previousState) => ({ ...previousState, password: event.target.value }));
            }}
            placeholder={t('pages.login.passwordPlaceholder')}
            type="password"
            value={formState.password}
          />

          <label className="auth-remember">
            <input
              checked={formState.rememberMe}
              onChange={(event) => {
                setFormState((previousState) => ({ ...previousState, rememberMe: event.target.checked }));
              }}
              type="checkbox"
            />
            <span>{t('pages.login.rememberMeLabel')}</span>
          </label>

          {submitError ? <p className="auth-error">{submitError}</p> : null}

          <Button disabled={isSubmitting} fullWidth type="submit">
            {isSubmitting ? t('pages.login.submitting') : t('pages.login.submit')}
          </Button>
        </form>

        <p className="auth-switch-text">
          {t('pages.login.switchPrompt')}{' '}
          <Link className="auth-switch-link" to={ROUTES.register}>
            {t('pages.login.switchAction')}
          </Link>
        </p>
      </Card>
    </Container>
  );
}
