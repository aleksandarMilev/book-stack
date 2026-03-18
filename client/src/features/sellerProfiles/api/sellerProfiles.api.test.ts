import type { AxiosError } from 'axios';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { httpClient } from '@/api/httpClient';
import { sellerProfilesApi } from '@/features/sellerProfiles/api/sellerProfiles.api';

vi.mock('@/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
    put: vi.fn(),
  },
}));

const createAxiosError = (status: number): AxiosError =>
  ({
    isAxiosError: true,
    response: {
      status,
    },
  } as AxiosError);

describe('sellerProfilesApi.getMine', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('returns profile data when API responds with profile payload', async () => {
    const profile = {
      userId: 'user-1',
      displayName: 'Book Corner',
      phoneNumber: '+359888111222',
      supportsOnlinePayment: true,
      supportsCashOnDelivery: true,
      isActive: true,
      createdOn: '2026-03-17T10:00:00Z',
      modifiedOn: null,
    };

    vi.mocked(httpClient.get).mockResolvedValue({
      status: 200,
      data: profile,
    });

    await expect(sellerProfilesApi.getMine()).resolves.toEqual(profile);
  });

  it('returns null when API returns 404 for missing seller profile', async () => {
    vi.mocked(httpClient.get).mockRejectedValue(createAxiosError(404));

    await expect(sellerProfilesApi.getMine()).resolves.toBeNull();
  });

  it('returns null when API responds with empty profile payload', async () => {
    vi.mocked(httpClient.get).mockResolvedValue({
      status: 200,
      data: null,
    });

    await expect(sellerProfilesApi.getMine()).resolves.toBeNull();
  });

  it('rethrows non-404 API failures', async () => {
    const error = createAxiosError(500);
    vi.mocked(httpClient.get).mockRejectedValue(error);

    await expect(sellerProfilesApi.getMine()).rejects.toBe(error);
  });
});
