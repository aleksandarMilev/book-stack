import { afterEach, describe, expect, it, vi } from 'vitest';

import { httpClient } from '@/api/httpClient';
import { identityApi } from '@/features/auth/api/identity.api';

vi.mock('@/api/httpClient', () => ({
  httpClient: {
    post: vi.fn(),
  },
}));

describe('identityApi.register', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('sends multipart form-data payload when image is not provided', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({
      data: { token: 'jwt-token' },
    });

    const response = await identityApi.register({
      username: 'alice',
      email: 'alice@example.com',
      password: 'Password123',
      firstName: 'Alice',
      lastName: 'Johnson',
      image: null,
    });

    const postCallArguments = vi.mocked(httpClient.post).mock.calls[0];
    expect(postCallArguments).toBeDefined();
    if (!postCallArguments) {
      throw new Error('Expected register request payload to be posted.');
    }

    const [url, payload, config] = postCallArguments;
    expect(url).toBe('/Identity/register/');
    expect(payload).toBeInstanceOf(FormData);
    expect(config).toEqual({
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    const formData = payload as FormData;
    expect(formData.get('username')).toBe('alice');
    expect(formData.get('email')).toBe('alice@example.com');
    expect(formData.get('password')).toBe('Password123');
    expect(formData.get('firstName')).toBe('Alice');
    expect(formData.get('lastName')).toBe('Johnson');
    expect(formData.get('image')).toBeNull();
    expect(response.token).toBe('jwt-token');
  });

  it('includes image file in multipart payload when provided', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({
      data: { token: 'jwt-token' },
    });

    const profileImage = new File(['avatar'], 'avatar.png', { type: 'image/png' });

    await identityApi.register({
      username: 'bob',
      email: 'bob@example.com',
      password: 'Password123',
      firstName: 'Bob',
      lastName: 'Miller',
      image: profileImage,
    });

    const postCallArguments = vi.mocked(httpClient.post).mock.calls[0];
    expect(postCallArguments).toBeDefined();
    if (!postCallArguments) {
      throw new Error('Expected register request payload to be posted.');
    }

    const [, payload] = postCallArguments;
    expect(payload).toBeInstanceOf(FormData);
    expect((payload as FormData).get('image')).toBe(profileImage);
  });
});
