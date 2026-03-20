import type { FormEvent } from 'react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, Input } from '@/components/ui';
import { AuthRouteNotice, type AuthRouteNoticeReason } from '@/features/auth/components/AuthRouteNotice';
import { AuthStateNotice } from '@/features/auth/components/AuthStateNotice';
import { authService } from '@/features/auth/services/auth.service';
import { ROUTES } from '@/routes/paths';

interface ForgotPasswordFormState {
  email: string;
}

export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const location = useLocation();
  const [formState, setFormState] = useState<ForgotPasswordFormState>({ email: '' });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof ForgotPasswordFormState, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const routeState = useMemo(
    () => location.state as { reason?: string } | null,
    [location.state],
  );
  const routeNoticeReason: AuthRouteNoticeReason | null =
    routeState?.reason === 'authRequired' || routeState?.reason === 'sessionExpired'
      ? routeState.reason
      : null;

  const validateForm = (): boolean => {
    const nextFieldErrors: Partial<Record<keyof ForgotPasswordFormState, string>> = {};

    if (!formState.email.trim()) {
      nextFieldErrors.email = t('pages.forgotPassword.validation.emailRequired');
    } else if (!/\S+@\S+\.\S+/.test(formState.email.trim())) {
      nextFieldErrors.email = t('pages.forgotPassword.validation.emailInvalid');
    }

    setFieldErrors(nextFieldErrors);

    return Object.keys(nextFieldErrors).length === 0;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setSubmitError(null);
    setSuccessMessage(null);

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);

    try {
      await authService.forgotPassword(formState.email.trim());
      setSuccessMessage(t('pages.forgotPassword.successMessage'));
    } catch (error: unknown) {
      setSubmitError(getApiErrorMessage(error, t('pages.forgotPassword.errorGeneric')));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Container className="auth-page">
      <Card className="auth-card auth-card--recovery" elevated>
        <div className="auth-shell-head">
          <h1>{t('pages.forgotPassword.title')}</h1>
          <p className="auth-shell-subtitle">{t('pages.forgotPassword.subtitle')}</p>
        </div>

        <div className="auth-shell-body">
          {routeNoticeReason ? <AuthRouteNotice reason={routeNoticeReason} /> : null}

          <form aria-busy={isSubmitting} className="auth-form" data-submitting={isSubmitting ? 'true' : undefined} onSubmit={handleSubmit}>
            <div className="auth-form-fields">
              <Input
                autoComplete="email"
                error={fieldErrors.email}
                label={t('pages.forgotPassword.emailLabel')}
                onChange={(event) => {
                  setFormState({ email: event.target.value });
                }}
                placeholder={t('pages.forgotPassword.emailPlaceholder')}
                type="email"
                value={formState.email}
              />
            </div>

            {successMessage ? (
              <AuthStateNotice
                badge={t('pages.forgotPassword.successBadge')}
                description={successMessage}
                hint={t('pages.forgotPassword.successHint')}
                title={t('pages.forgotPassword.successTitle')}
                tone="success"
              />
            ) : null}
            {submitError ? (
              <AuthStateNotice
                badge={t('pages.forgotPassword.errorBadge')}
                description={submitError}
                hint={t('pages.forgotPassword.errorHint')}
                title={t('pages.forgotPassword.errorTitle')}
                tone="danger"
              />
            ) : null}

            <div className="auth-form-actions">
              <Button className="auth-submit-button" data-loading={isSubmitting ? 'true' : undefined} disabled={isSubmitting} fullWidth type="submit">
                {isSubmitting ? t('pages.forgotPassword.submitting') : t('pages.forgotPassword.submit')}
              </Button>
            </div>
          </form>
        </div>

        <div className="auth-shell-footer">
          <p className="auth-switch-text">
            {t('pages.forgotPassword.backToLoginPrompt')}{' '}
            <Link className="auth-switch-link" to={ROUTES.login}>
              {t('pages.forgotPassword.backToLoginAction')}
            </Link>
          </p>
        </div>
      </Card>
    </Container>
  );
}
