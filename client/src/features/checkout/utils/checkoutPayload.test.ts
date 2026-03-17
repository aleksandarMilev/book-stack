import { describe, expect, it } from 'vitest';

import { mapCheckoutSubmissionToCreateOrderRequest } from '@/features/checkout/utils/checkoutPayload';

describe('mapCheckoutSubmissionToCreateOrderRequest', () => {
  it('maps checkout submission to backend order request shape', () => {
    const payload = mapCheckoutSubmissionToCreateOrderRequest({
      customerFirstName: '  John ',
      customerLastName: ' Doe  ',
      email: '  john@example.com  ',
      phoneNumber: '  +359888123456 ',
      country: ' Bulgaria ',
      city: ' Sofia ',
      addressLine: ' 1 Vitosha Blvd ',
      postalCode: ' 1000 ',
      items: [{ listingId: 'listing-1', quantity: 2 }],
    });

    expect(payload).toEqual({
      customerFirstName: 'John',
      customerLastName: 'Doe',
      email: 'john@example.com',
      phoneNumber: '+359888123456',
      country: 'Bulgaria',
      city: 'Sofia',
      addressLine: '1 Vitosha Blvd',
      postalCode: '1000',
      items: [{ listingId: 'listing-1', quantity: 2 }],
    });
  });

  it('omits optional fields when they are empty', () => {
    const payload = mapCheckoutSubmissionToCreateOrderRequest({
      customerFirstName: 'John',
      customerLastName: 'Doe',
      email: 'john@example.com',
      phoneNumber: ' ',
      country: 'Bulgaria',
      city: 'Sofia',
      addressLine: '1 Vitosha Blvd',
      postalCode: '',
      items: [{ listingId: 'listing-1', quantity: 1 }],
    });

    expect(payload.phoneNumber).toBeUndefined();
    expect(payload.postalCode).toBeUndefined();
  });
});
