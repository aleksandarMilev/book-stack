import type { InputHTMLAttributes } from 'react';

import { classNames } from '@/utils/classNames';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  hint?: string;
  error?: string;
}

export function Input({ className, label, hint, error, id, ...props }: InputProps) {
  const fieldId = id ?? props.name;
  const hintId = hint && fieldId ? `${fieldId}-hint` : undefined;
  const errorId = error && fieldId ? `${fieldId}-error` : undefined;

  return (
    <label className="ui-input-wrapper" htmlFor={fieldId}>
      {label ? <span className="ui-input-label">{label}</span> : null}
      <input
        aria-describedby={[hintId, errorId].filter(Boolean).join(' ') || undefined}
        className={classNames('ui-input', error && 'ui-input--error', className)}
        id={fieldId}
        {...props}
      />
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
    </label>
  );
}
