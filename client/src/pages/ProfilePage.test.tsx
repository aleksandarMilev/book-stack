import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { profileApi } from '@/features/auth/api/profile.api';
import { ProfilePage } from '@/pages/ProfilePage';
import { useAuthStore } from '@/store/auth.store';
import type { AuthSession } from '@/types/auth.types';

vi.mock('@/features/auth/api/profile.api', () => ({
  profileApi: {
    getMine: vi.fn(),
    updateMine: vi.fn(),
  },
}));

const createSession = (email: string, role: AuthSession['user']['role'] = 'buyer'): AuthSession => ({
  accessToken: 'mock-token',
  expiresAtUtc: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
  user: {
    id: 'user-1',
    role,
    email,
  },
});

const createProfileResponse = (imagePath: string) => ({
  id: 'user-1',
  firstName: 'Alice',
  lastName: 'Reader',
  imagePath,
  imageUrl: imagePath,
});

describe('ProfilePage', () => {
  afterEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({ session: null });
  });

  it('renders custom avatar image in profile summary', async () => {
    useAuthStore.setState({ session: createSession('alice@example.com') });
    vi.mocked(profileApi.getMine).mockResolvedValue(createProfileResponse('/images/profiles/custom-user.jpg'));

    render(<ProfilePage />);

    const avatarImage = await screen.findByTestId('profile-avatar-image');
    expect(avatarImage).toHaveAttribute('src', '/images/profiles/custom-user.jpg');
    expect(screen.getByTestId('profile-summary-email-value')).toHaveTextContent('alice@example.com');
    expect(screen.getByTestId('profile-summary-role-value')).toHaveTextContent('Buyer');
  });

  it('renders shared default avatar image when profile uses default path', async () => {
    useAuthStore.setState({ session: createSession('alice@example.com') });
    vi.mocked(profileApi.getMine).mockResolvedValue(createProfileResponse('/images/profiles/default.jpg'));

    render(<ProfilePage />);

    const avatarImage = await screen.findByTestId('profile-avatar-image');
    expect(avatarImage).toHaveAttribute('src', '/images/profiles/default.jpg');
  });

  it('keeps long email in dedicated summary value container', async () => {
    const longEmail =
      'very.long.account.email.address.for.layout-regression-checking@example-bookstack-domain.test';

    useAuthStore.setState({ session: createSession(longEmail, 'admin') });
    vi.mocked(profileApi.getMine).mockResolvedValue(createProfileResponse('/images/profiles/default.jpg'));

    render(<ProfilePage />);

    const emailValue = await screen.findByTestId('profile-summary-email-value');
    expect(emailValue).toHaveClass('profile-summary-value');
    expect(emailValue).toHaveTextContent(longEmail);
    expect(emailValue).toHaveAttribute('title', longEmail);
    expect(screen.getByTestId('profile-summary-role-value')).toHaveTextContent('Admin');
  });

  it('submits remove-image flow and refreshes to default avatar', async () => {
    useAuthStore.setState({ session: createSession('alice@example.com') });
    vi.mocked(profileApi.getMine)
      .mockResolvedValueOnce(createProfileResponse('/images/profiles/custom-user.jpg'))
      .mockResolvedValueOnce(createProfileResponse('/images/profiles/default.jpg'));
    vi.mocked(profileApi.updateMine).mockResolvedValue(undefined);

    render(<ProfilePage />);

    await screen.findByTestId('profile-avatar-image');
    await userEvent.click(screen.getByRole('checkbox', { name: 'Remove current profile image' }));
    await userEvent.click(screen.getByRole('button', { name: 'Save changes' }));

    await waitFor(() => {
      expect(profileApi.updateMine).toHaveBeenCalledWith({
        firstName: 'Alice',
        lastName: 'Reader',
        image: null,
        removeImage: true,
      });
    });

    await waitFor(() => {
      expect(screen.getByTestId('profile-avatar-image')).toHaveAttribute(
        'src',
        '/images/profiles/default.jpg',
      );
    });
  });
});
