import type { BadgeVariant } from '@/components/ui/Badge';
import type { ModerationStatus } from '@/features/admin/types/admin.types';

interface ModerationSubject {
  isApproved: boolean;
  rejectionReason?: string | null;
}

export const MODERATION_STATUS_BADGE_VARIANT: Record<ModerationStatus, BadgeVariant> = {
  approved: 'success',
  pending: 'warning',
  rejected: 'danger',
};

export const deriveModerationStatus = (subject: ModerationSubject): ModerationStatus => {
  if (subject.isApproved) {
    return 'approved';
  }

  return subject.rejectionReason ? 'rejected' : 'pending';
};
