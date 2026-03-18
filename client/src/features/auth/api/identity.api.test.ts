import { afterEach, describe, expect, it, vi } from 'vitest';

import { httpClient } from '@/api/httpClient';
import { identityApi } from '@/features/auth/api/identity.api';

vi.mock('@/api/httpClient', () => ({
  httpClient: {
    post: vi.fn(),
  },
}));

describe('identityApi', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('posts login payload to the identity login endpoint', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({
      data: { token: 'jwt-token' },
    });

    const response = await identityApi.login({
      credentials: 'alice@example.com',
      password: 'Password123',
      rememberMe: true,
    });

    expect(httpClient.post).toHaveBeenCalledWith('/Identity/login/', {
      credentials: 'alice@example.com',
      password: 'Password123',
      rememberMe: true,
    });
    expect(response.token).toBe('jwt-token');
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

  it('posts forgot-password payload to the expected endpoint', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({
      data: { message: 'If an account exists for that email, a password reset link has been sent.' },
    });

    const response = await identityApi.forgotPassword({
      email: 'alice@example.com',
    });

    expect(httpClient.post).toHaveBeenCalledWith('/Identity/forgot-password/', {
      email: 'alice@example.com',
    });
    expect(response.message).toBe('If an account exists for that email, a password reset link has been sent.');
  });

  it('posts reset-password payload to the expected endpoint', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({
      data: { message: 'Password successfully reset.' },
    });

    const response = await identityApi.resetPassword({
      email: 'alice@example.com',
      token: 'encoded-token',
      newPassword: 'Password123',
    });

    expect(httpClient.post).toHaveBeenCalledWith('/Identity/reset-password/', {
      email: 'alice@example.com',
      token: 'encoded-token',
      newPassword: 'Password123',
    });
    expect(response.message).toBe('Password successfully reset.');
  });
});
