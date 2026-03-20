import { useTranslation } from 'react-i18next';

import { Badge } from '@/components/ui';

export type AuthRouteNoticeReason = 'authRequired' | 'sessionExpired';

interface AuthRouteNoticeProps {
  reason: AuthRouteNoticeReason;
}

export function AuthRouteNotice({ reason }: AuthRouteNoticeProps) {
  const { t } = useTranslation();

  return (
    <div className={`auth-route-notice auth-route-notice--${reason}`} role="status">
      <div className="auth-route-notice-head">
        <Badge className="auth-route-notice-badge" variant={reason === 'sessionExpired' ? 'warning' : 'accent'}>
          {t('pages.routeAccess.authNoticeBadge')}
        </Badge>
        <p className="auth-route-notice-title">
          {reason === 'sessionExpired'
            ? t('pages.routeAccess.sessionExpiredTitle')
            : t('pages.routeAccess.authRequiredTitle')}
        </p>
        <p className="auth-route-notice-description">{t(`pages.login.info.${reason}`)}</p>
      </div>
      <p className="auth-route-notice-hint">{t('pages.routeAccess.authNoticeHint')}</p>
    </div>
  );
}
