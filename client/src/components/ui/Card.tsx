import type { HTMLAttributes } from 'react';

import { classNames } from '@/utils/classNames';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  elevated?: boolean;
}

export function Card({ className, elevated = false, ...props }: CardProps) {
  return <div className={classNames('ui-card', elevated && 'ui-card--elevated', className)} {...props} />;
}
