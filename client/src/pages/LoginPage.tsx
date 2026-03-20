import type { FormEvent } from 'react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, Input } from '@/components/ui';
import { AuthRouteNotice, type AuthRouteNoticeReason } from '@/features/auth/components/AuthRouteNotice';
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

  const routeState = useMemo(() => location.state as { from?: string; reason?: string } | null, [location.state]);

  const redirectTo = useMemo(() => {
    const state = routeState;
    return state?.from ?? ROUTES.home;
  }, [routeState]);

  const redirectReason = routeState?.reason;
  const routeNoticeReason: AuthRouteNoticeReason | null =
    redirectReason === 'authRequired' || redirectReason === 'sessionExpired'
      ? redirectReason
      : null;

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
        <div className="auth-shell-head">
          <h1>{t('pages.login.title')}</h1>
          <p className="auth-shell-subtitle">{t('pages.login.subtitle')}</p>
        </div>

        <div className="auth-shell-body">
          {routeNoticeReason ? <AuthRouteNotice reason={routeNoticeReason} /> : null}

          <form aria-busy={isSubmitting} className="auth-form" data-submitting={isSubmitting ? 'true' : undefined} onSubmit={handleSubmit}>
            <div className="auth-form-fields">
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
            </div>

            <div className="auth-form-support">
              <label className="auth-remember">
                <input
                  className="auth-remember-checkbox"
                  checked={formState.rememberMe}
                  onChange={(event) => {
                    setFormState((previousState) => ({ ...previousState, rememberMe: event.target.checked }));
                  }}
                  type="checkbox"
                />
                <span>{t('pages.login.rememberMeLabel')}</span>
              </label>

              <p className="auth-inline-link">
                <Link className="auth-switch-link" to={ROUTES.forgotPassword}>
                  {t('pages.login.forgotPasswordAction')}
                </Link>
              </p>
            </div>

            {submitError ? <p className="auth-error auth-feedback">{submitError}</p> : null}

            <div className="auth-form-actions">
              <Button className="auth-submit-button" data-loading={isSubmitting ? 'true' : undefined} disabled={isSubmitting} fullWidth type="submit">
                {isSubmitting ? t('pages.login.submitting') : t('pages.login.submit')}
              </Button>
            </div>
          </form>
        </div>

        <div className="auth-shell-footer">
          <p className="auth-switch-text">
            {t('pages.login.switchPrompt')}{' '}
            <Link className="auth-switch-link" to={ROUTES.register}>
              {t('pages.login.switchAction')}
            </Link>
          </p>
        </div>
      </Card>
    </Container>
  );
}
