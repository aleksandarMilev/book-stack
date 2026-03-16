import type { HTMLAttributes } from 'react';

import { classNames } from '@/utils/classNames';

type BadgeVariant = 'neutral' | 'accent' | 'success';

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
}

export function Badge({ className, variant = 'neutral', ...props }: BadgeProps) {
  return <span className={classNames('ui-badge', `ui-badge--${variant}`, className)} {...props} />;
}
