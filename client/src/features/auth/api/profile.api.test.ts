import { afterEach, describe, expect, it, vi } from 'vitest';

import { httpClient } from '@/api/httpClient';
import { profileApi } from '@/features/auth/api/profile.api';

vi.mock('@/api/httpClient', () => ({
  httpClient: {
    get: vi.fn(),
    put: vi.fn(),
  },
}));

describe('profileApi.updateMine', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('sends form-data payload when image is not provided', async () => {
    vi.mocked(httpClient.put).mockResolvedValue({ data: undefined });

    await profileApi.updateMine({
      firstName: 'Alice',
      lastName: 'Johnson',
      image: null,
      removeImage: true,
    });

    const putCallArguments = vi.mocked(httpClient.put).mock.calls[0];
    expect(putCallArguments).toBeDefined();
    if (!putCallArguments) {
      throw new Error('Expected profile update request payload to be posted.');
    }

    const [url, payload, config] = putCallArguments;
    expect(url).toBe('/Profiles');
    expect(payload).toBeInstanceOf(FormData);
    expect(config).toEqual({
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    const formData = payload as FormData;
    expect(formData.get('firstName')).toBe('Alice');
    expect(formData.get('lastName')).toBe('Johnson');
    expect(formData.get('removeImage')).toBe('true');
    expect(formData.get('image')).toBeNull();
  });

  it('includes image file when provided', async () => {
    vi.mocked(httpClient.put).mockResolvedValue({ data: undefined });
    const image = new File(['avatar'], 'avatar.png', { type: 'image/png' });

    await profileApi.updateMine({
      firstName: 'Bob',
      lastName: 'Miller',
      image,
      removeImage: false,
    });

    const putCallArguments = vi.mocked(httpClient.put).mock.calls[0];
    expect(putCallArguments).toBeDefined();
    if (!putCallArguments) {
      throw new Error('Expected profile update request payload to be posted.');
    }

    const [, payload] = putCallArguments;
    expect(payload).toBeInstanceOf(FormData);
    expect((payload as FormData).get('image')).toBe(image);
  });
});
