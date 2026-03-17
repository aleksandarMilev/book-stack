import { useTranslation } from 'react-i18next';

import { Badge } from '@/components/ui';
import type { ModerationStatus } from '@/features/admin/types/admin.types';
import { MODERATION_STATUS_BADGE_VARIANT } from '@/features/admin/utils/moderationStatus';

interface ModerationStatusBadgeProps {
  status: ModerationStatus;
}

export function ModerationStatusBadge({ status }: ModerationStatusBadgeProps) {
  const { t } = useTranslation();

  return (
    <Badge variant={MODERATION_STATUS_BADGE_VARIANT[status]}>{t(`pages.adminModeration.status.${status}`)}</Badge>
  );
}
