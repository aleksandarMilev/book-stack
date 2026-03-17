import { describe, expect, it } from 'vitest';

import { deriveModerationStatus } from '@/features/admin/utils/moderationStatus';

describe('deriveModerationStatus', () => {
  it('returns approved when item is approved', () => {
    expect(deriveModerationStatus({ isApproved: true, rejectionReason: null })).toBe('approved');
  });

  it('returns pending when item is not approved without rejection reason', () => {
    expect(deriveModerationStatus({ isApproved: false, rejectionReason: null })).toBe('pending');
  });

  it('returns rejected when item is not approved and has rejection reason', () => {
    expect(deriveModerationStatus({ isApproved: false, rejectionReason: 'Missing details' })).toBe('rejected');
  });
});
