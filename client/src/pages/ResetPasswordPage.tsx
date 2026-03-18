import type { FormEvent } from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, Input } from '@/components/ui';
import { authService } from '@/features/auth/services/auth.service';
import { ROUTES } from '@/routes/paths';

interface ResetPasswordFormState {
  password: string;
  confirmPassword: string;
}

export function ResetPasswordPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const [formState, setFormState] = useState<ResetPasswordFormState>({
    password: '',
    confirmPassword: '',
  });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof ResetPasswordFormState, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isResetComplete, setIsResetComplete] = useState(false);

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
      <Card className="auth-card" elevated>
        <h1>{t('pages.resetPassword.title')}</h1>
        <p>{t('pages.resetPassword.subtitle')}</p>

        {!hasResetContext ? (
          <>
            <p className="auth-error">{t('pages.resetPassword.invalidLinkTitle')}</p>
            <p className="auth-switch-text">{t('pages.resetPassword.invalidLinkDescription')}</p>
            <p className="auth-switch-text">
              <Link className="auth-switch-link" to={ROUTES.forgotPassword}>
                {t('pages.resetPassword.requestNewLinkAction')}
              </Link>
            </p>
          </>
        ) : isResetComplete ? (
          <>
            <p className="auth-info">{t('pages.resetPassword.successMessage')}</p>
            <p className="auth-switch-text">
              {t('pages.resetPassword.backToLoginPrompt')}{' '}
              <Link className="auth-switch-link" to={ROUTES.login}>
                {t('pages.resetPassword.backToLoginAction')}
              </Link>
            </p>
          </>
        ) : (
          <form className="auth-form" onSubmit={handleSubmit}>
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

            {submitError ? <p className="auth-error">{submitError}</p> : null}

            <Button disabled={isSubmitting} fullWidth type="submit">
              {isSubmitting ? t('pages.resetPassword.submitting') : t('pages.resetPassword.submit')}
            </Button>
          </form>
        )}
      </Card>
    </Container>
  );
}
