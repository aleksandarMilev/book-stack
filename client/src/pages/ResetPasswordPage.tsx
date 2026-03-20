import type { FormEvent } from 'react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation, useSearchParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, Input } from '@/components/ui';
import { AuthRouteNotice, type AuthRouteNoticeReason } from '@/features/auth/components/AuthRouteNotice';
import { AuthStateNotice } from '@/features/auth/components/AuthStateNotice';
import { authService } from '@/features/auth/services/auth.service';
import { ROUTES } from '@/routes/paths';

interface ResetPasswordFormState {
  password: string;
  confirmPassword: string;
}

export function ResetPasswordPage() {
  const { t } = useTranslation();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const [formState, setFormState] = useState<ResetPasswordFormState>({
    password: '',
    confirmPassword: '',
  });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof ResetPasswordFormState, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isResetComplete, setIsResetComplete] = useState(false);
  const routeState = useMemo(
    () => location.state as { reason?: string } | null,
    [location.state],
  );
  const routeNoticeReason: AuthRouteNoticeReason | null =
    routeState?.reason === 'authRequired' || routeState?.reason === 'sessionExpired'
      ? routeState.reason
      : null;

  const email = searchParams.get('email')?.trim() ?? '';
  const token = searchParams.get('token')?.trim() ?? '';
  const hasResetContext = email.length > 0 && token.length > 0;

  const validateForm = (): boolean => {
    const nextFieldErrors: Partial<Record<keyof ResetPasswordFormState, string>> = {};

    if (!formState.password) {
      nextFieldErrors.password = t('pages.resetPassword.validation.passwordRequired');
    } else if (formState.password.length < 6) {
      nextFieldErrors.password = t('pages.resetPassword.validation.passwordMinLength');
    }

    if (formState.confirmPassword !== formState.password) {
      nextFieldErrors.confirmPassword = t('pages.resetPassword.validation.passwordMismatch');
    }

    setFieldErrors(nextFieldErrors);
    return Object.keys(nextFieldErrors).length === 0;
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setSubmitError(null);

    if (!hasResetContext) {
      setSubmitError(t('pages.resetPassword.invalidLinkDescription'));
      return;
    }

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);

    try {
      await authService.resetPassword({
        email,
        token,
        newPassword: formState.password,
      });
      setIsResetComplete(true);
    } catch (error: unknown) {
      setSubmitError(getApiErrorMessage(error, t('pages.resetPassword.errorGeneric')));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Container className="auth-page">
      <Card className="auth-card auth-card--recovery" elevated>
        <div className="auth-shell-head">
          <h1>{t('pages.resetPassword.title')}</h1>
          <p className="auth-shell-subtitle">{t('pages.resetPassword.subtitle')}</p>
        </div>

        <div className="auth-shell-body">
          {routeNoticeReason ? <AuthRouteNotice reason={routeNoticeReason} /> : null}

          {!hasResetContext ? (
            <div className="auth-state-flow">
              <AuthStateNotice
                actions={
                  <Link className="auth-switch-link" to={ROUTES.forgotPassword}>
                    <Button variant="secondary">{t('pages.resetPassword.requestNewLinkAction')}</Button>
                  </Link>
                }
                badge={t('pages.resetPassword.invalidLinkBadge')}
                description={t('pages.resetPassword.invalidLinkDescription')}
                hint={t('pages.resetPassword.invalidLinkHint')}
                title={t('pages.resetPassword.invalidLinkTitle')}
                tone="warning"
              />
            </div>
          ) : isResetComplete ? (
            <div className="auth-state-flow">
              <AuthStateNotice
                actions={
                  <Link className="auth-switch-link" to={ROUTES.login}>
                    <Button variant="secondary">{t('pages.resetPassword.backToLoginAction')}</Button>
                  </Link>
                }
                badge={t('pages.resetPassword.successBadge')}
                description={t('pages.resetPassword.successMessage')}
                hint={t('pages.resetPassword.backToLoginPrompt')}
                title={t('pages.resetPassword.successTitle')}
                tone="success"
              />
            </div>
          ) : (
            <form aria-busy={isSubmitting} className="auth-form" data-submitting={isSubmitting ? 'true' : undefined} onSubmit={handleSubmit}>
              <div className="auth-form-fields">
                <Input disabled label={t('pages.resetPassword.emailLabel')} type="email" value={email} />
                <Input
                  autoComplete="new-password"
                  error={fieldErrors.password}
                  label={t('pages.resetPassword.passwordLabel')}
                  onChange={(event) => {
                    setFormState((previousState) => ({ ...previousState, password: event.target.value }));
                  }}
                  type="password"
                  value={formState.password}
                />
                <Input
                  autoComplete="new-password"
                  error={fieldErrors.confirmPassword}
                  label={t('pages.resetPassword.confirmPasswordLabel')}
                  onChange={(event) => {
                    setFormState((previousState) => ({ ...previousState, confirmPassword: event.target.value }));
                  }}
                  type="password"
                  value={formState.confirmPassword}
                />
              </div>

              {submitError ? (
                <AuthStateNotice
                  badge={t('pages.resetPassword.errorBadge')}
                  description={submitError}
                  hint={t('pages.resetPassword.errorHint')}
                  title={t('pages.resetPassword.errorTitle')}
                  tone="danger"
                />
              ) : null}

              <div className="auth-form-actions">
                <Button className="auth-submit-button" data-loading={isSubmitting ? 'true' : undefined} disabled={isSubmitting} fullWidth type="submit">
                  {isSubmitting ? t('pages.resetPassword.submitting') : t('pages.resetPassword.submit')}
                </Button>
              </div>
            </form>
          )}
        </div>

        {hasResetContext && !isResetComplete ? (
          <div className="auth-shell-footer">
            <p className="auth-switch-text">
              {t('pages.resetPassword.backToLoginPrompt')}{' '}
              <Link className="auth-switch-link" to={ROUTES.login}>
                {t('pages.resetPassword.backToLoginAction')}
              </Link>
            </p>
          </div>
        ) : null}
      </Card>
    </Container>
  );
}
