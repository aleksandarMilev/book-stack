import type { ChangeEvent, InputHTMLAttributes } from 'react';
import { useEffect, useId, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { classNames } from '@/utils/classNames';

interface FileUploadFieldProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'value' | 'onChange'> {
  label?: string;
  hint?: string;
  error?: string;
  file: File | null;
  onFileChange: (file: File | null) => void;
  triggerLabel?: string;
  emptyLabel?: string;
  clearLabel?: string;
  showImagePreview?: boolean;
  previewAlt?: string;
  previewUrl?: string | null;
}

export function FileUploadField({
  className,
  label,
  hint,
  error,
  id,
  name,
  file,
  onFileChange,
  disabled = false,
  triggerLabel,
  emptyLabel,
  clearLabel,
  showImagePreview = false,
  previewAlt,
  previewUrl = null,
  ...props
}: FileUploadFieldProps) {
  const { t } = useTranslation();
  const generatedId = useId();
  const fieldId = id ?? name ?? generatedId;
  const hintId = hint ? `${fieldId}-hint` : undefined;
  const errorId = error ? `${fieldId}-error` : undefined;
  const inputRef = useRef<HTMLInputElement>(null);
  const [objectPreviewUrl, setObjectPreviewUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!showImagePreview || !file || !file.type.startsWith('image/')) {
      setObjectPreviewUrl(null);
      return;
    }

    const nextObjectUrl = URL.createObjectURL(file);
    setObjectPreviewUrl(nextObjectUrl);

    return () => {
      URL.revokeObjectURL(nextObjectUrl);
    };
  }, [file, showImagePreview]);

  const handleInputChange = (event: ChangeEvent<HTMLInputElement>): void => {
    onFileChange(event.target.files?.[0] ?? null);
  };

  const handleOpenFilePicker = (): void => {
    if (disabled) {
      return;
    }

    inputRef.current?.click();
  };

  const handleClearFile = (): void => {
    if (disabled) {
      return;
    }

    if (inputRef.current) {
      inputRef.current.value = '';
    }

    onFileChange(null);
  };

  const resolvedTriggerLabel = triggerLabel ?? t('common.fileUpload.chooseFile');
  const resolvedEmptyLabel = emptyLabel ?? t('common.fileUpload.noFileSelected');
  const resolvedClearLabel = clearLabel ?? t('common.fileUpload.clearSelectedFile');
  const resolvedPreviewAlt = previewAlt ?? t('common.fileUpload.previewAlt');
  const previewSource = showImagePreview ? objectPreviewUrl ?? previewUrl : null;

  return (
    <div className={classNames('ui-input-wrapper', className)}>
      {label ? (
        <label className="ui-input-label" htmlFor={fieldId}>
          {label}
        </label>
      ) : null}

      <div className={classNames('ui-file-upload', error && 'ui-file-upload--error', disabled && 'ui-file-upload--disabled')}>
        <input
          {...props}
          accept={props.accept}
          aria-describedby={[hintId, errorId].filter(Boolean).join(' ') || undefined}
          aria-invalid={Boolean(error)}
          className="ui-file-upload-input"
          disabled={disabled}
          id={fieldId}
          name={name}
          onChange={handleInputChange}
          ref={inputRef}
          type="file"
        />

        <div className="ui-file-upload-row">
          <button
            className="ui-button ui-button--secondary ui-button--sm ui-file-upload-trigger"
            disabled={disabled}
            onClick={handleOpenFilePicker}
            type="button"
          >
            {resolvedTriggerLabel}
          </button>

          <span
            className={classNames('ui-file-upload-filename', !file && 'ui-file-upload-filename--empty')}
            title={file?.name ?? resolvedEmptyLabel}
          >
            {file?.name ?? resolvedEmptyLabel}
          </span>

          {file ? (
            <button
              aria-label={resolvedClearLabel}
              className="ui-file-upload-clear"
              disabled={disabled}
              onClick={handleClearFile}
              title={resolvedClearLabel}
              type="button"
            >
              <span aria-hidden>X</span>
            </button>
          ) : null}
        </div>

        {previewSource ? (
          <div className="ui-file-upload-preview">
            <img alt={resolvedPreviewAlt} src={previewSource} />
          </div>
        ) : null}
      </div>

      {hint ? (
        <span className="ui-input-hint" id={hintId}>
          {hint}
        </span>
      ) : null}
      {error ? (
        <span className="ui-input-error" id={errorId}>
          {error}
        </span>
      ) : null}
    </div>
  );
}

