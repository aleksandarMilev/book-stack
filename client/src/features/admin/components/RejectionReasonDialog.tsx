import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { Button, Card } from '@/components/ui';

interface RejectionReasonDialogProps {
  isOpen: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (reason: string) => Promise<void>;
}

const MIN_REASON_LENGTH = 3;

export function RejectionReasonDialog({ isOpen, isSubmitting, onClose, onSubmit }: RejectionReasonDialogProps) {
  const { t } = useTranslation();
  const [reason, setReason] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const reasonInputRef = useRef<HTMLTextAreaElement | null>(null);

  useEffect(() => {
    if (!isOpen) {
      setReason('');
      setErrorMessage(null);
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    reasonInputRef.current?.focus();

    const handleEscape = (event: KeyboardEvent): void => {
      if (event.key === 'Escape' && !isSubmitting) {
        onClose();
      }
    };

    window.addEventListener('keydown', handleEscape);
    return () => {
      window.removeEventListener('keydown', handleEscape);
    };
  }, [isOpen, isSubmitting, onClose]);

  if (!isOpen) {
    return null;
  }

  const handleSubmit = async (): Promise<void> => {
    const trimmedReason = reason.trim();
    if (trimmedReason.length < MIN_REASON_LENGTH) {
      setErrorMessage(t('pages.adminModeration.rejectDialog.validationReason'));
      return;
    }

    setErrorMessage(null);
    await onSubmit(trimmedReason);
  };

  return (
    <div
      className="admin-dialog-overlay"
      onClick={(event) => {
        if (event.target === event.currentTarget && !isSubmitting) {
          onClose();
        }
      }}
      role="presentation"
    >
      <Card aria-modal className="admin-dialog-card" role="dialog">
        <h2>{t('pages.adminModeration.rejectDialog.title')}</h2>
        <p className="admin-dialog-description">{t('pages.adminModeration.rejectDialog.description')}</p>

        <label className="ui-input-wrapper" htmlFor="moderation-reason">
          <span className="ui-input-label">{t('pages.adminModeration.rejectDialog.reasonLabel')}</span>
          <textarea
            className="ui-input admin-dialog-textarea"
            id="moderation-reason"
            maxLength={500}
            onChange={(event) => {
              setReason(event.target.value);
            }}
            placeholder={t('pages.adminModeration.rejectDialog.reasonPlaceholder')}
            ref={reasonInputRef}
            rows={4}
            value={reason}
          />
          {errorMessage ? <span className="ui-input-error">{errorMessage}</span> : null}
        </label>

        <div className="admin-dialog-actions">
          <Button onClick={onClose} variant="secondary">
            {t('common.actions.cancel')}
          </Button>
          <Button
            disabled={isSubmitting}
            onClick={() => {
              void handleSubmit();
            }}
            variant="primary"
          >
            {isSubmitting ? t('pages.adminModeration.rejectDialog.submitting') : t('common.actions.reject')}
          </Button>
        </div>
      </Card>
    </div>
  );
}
