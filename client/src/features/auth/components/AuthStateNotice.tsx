import type { ReactNode } from 'react';

import { Badge } from '@/components/ui';
import { classNames } from '@/utils/classNames';

type AuthStateNoticeTone = 'info' | 'success' | 'warning' | 'danger';

interface AuthStateNoticeProps {
  tone: AuthStateNoticeTone;
  badge: string;
  title: string;
  description?: string;
  hint?: string;
  actions?: ReactNode;
  className?: string;
  role?: 'status' | 'alert';
}

const toneToBadgeVariant = (tone: AuthStateNoticeTone): 'accent' | 'success' | 'warning' | 'danger' => {
  if (tone === 'success') {
    return 'success';
  }

  if (tone === 'warning') {
    return 'warning';
  }

  if (tone === 'danger') {
    return 'danger';
  }

  return 'accent';
};

export function AuthStateNotice({
  tone,
  badge,
  title,
  description,
  hint,
  actions,
  className,
  role = tone === 'danger' ? 'alert' : 'status',
}: AuthStateNoticeProps) {
  return (
    <div className={classNames('auth-state-notice', `auth-state-notice--${tone}`, className)} role={role}>
      <div className="auth-state-notice-head">
        <Badge className="auth-state-notice-badge" variant={toneToBadgeVariant(tone)}>
          {badge}
        </Badge>
        <p className="auth-state-notice-title">{title}</p>
        {description ? <p className="auth-state-notice-description">{description}</p> : null}
      </div>
      {hint ? <p className="auth-state-notice-hint">{hint}</p> : null}
      {actions ? <div className="auth-state-notice-actions">{actions}</div> : null}
    </div>
  );
}
