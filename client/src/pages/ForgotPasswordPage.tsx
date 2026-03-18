import type { FormEvent } from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { getApiErrorMessage } from '@/api/utils/apiError';
import { Button, Card, Container, Input } from '@/components/ui';
import { authService } from '@/features/auth/services/auth.service';
import { ROUTES } from '@/routes/paths';

interface ForgotPasswordFormState {
  email: string;
}

export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const [formState, setFormState] = useState<ForgotPasswordFormState>({ email: '' });
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<keyof ForgotPasswordFormState, string>>>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

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
      <Card className="auth-card" elevated>
        <h1>{t('pages.forgotPassword.title')}</h1>
        <p>{t('pages.forgotPassword.subtitle')}</p>

        <form className="auth-form" onSubmit={handleSubmit}>
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

          {successMessage ? <p className="auth-info">{successMessage}</p> : null}
          {submitError ? <p className="auth-error">{submitError}</p> : null}

          <Button disabled={isSubmitting} fullWidth type="submit">
            {isSubmitting ? t('pages.forgotPassword.submitting') : t('pages.forgotPassword.submit')}
          </Button>
        </form>

        <p className="auth-switch-text">
          {t('pages.forgotPassword.backToLoginPrompt')}{' '}
          <Link className="auth-switch-link" to={ROUTES.login}>
            {t('pages.forgotPassword.backToLoginAction')}
          </Link>
        </p>
      </Card>
    </Container>
  );
}

