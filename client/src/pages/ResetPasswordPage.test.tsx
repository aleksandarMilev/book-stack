import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { authService } from '@/features/auth/services/auth.service';
import { ResetPasswordPage } from '@/pages/ResetPasswordPage';

vi.mock('@/features/auth/services/auth.service', () => ({
  authService: {
    resetPassword: vi.fn(),
  },
}));

const renderResetPasswordPage = (initialEntry: string) =>
  render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route element={<ResetPasswordPage />} path="/identity/reset-password" />
      </Routes>
    </MemoryRouter>,
  );

describe('ResetPasswordPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('reads email and token from URL and submits reset payload', async () => {
    vi.mocked(authService.resetPassword).mockResolvedValue('Password successfully reset.');
    const user = userEvent.setup();

    renderResetPasswordPage('/identity/reset-password?email=user%40example.com&token=encoded-token');

    expect(screen.getByDisplayValue('user@example.com')).toBeDisabled();

    await user.type(screen.getByLabelText('New password'), 'Password123');
    await user.type(screen.getByLabelText('Confirm new password'), 'Password123');
    await user.click(screen.getByRole('button', { name: 'Reset password' }));

    await waitFor(() => {
      expect(authService.resetPassword).toHaveBeenCalledWith({
        email: 'user@example.com',
        token: 'encoded-token',
        newPassword: 'Password123',
      });
    });
    expect(await screen.findByText('Password reset complete')).toBeInTheDocument();
    expect(
      await screen.findByText('Password reset successful. You can now sign in with your new password.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Go to login' })).toBeInTheDocument();
  });

  it('shows reset error when API request fails', async () => {
    vi.mocked(authService.resetPassword).mockRejectedValue(new Error('invalid token'));
    const user = userEvent.setup();

    renderResetPasswordPage('/identity/reset-password?email=user%40example.com&token=expired-token');

    await user.type(screen.getByLabelText('New password'), 'Password123');
    await user.type(screen.getByLabelText('Confirm new password'), 'Password123');
    await user.click(screen.getByRole('button', { name: 'Reset password' }));

    expect(await screen.findByText('Could not reset password')).toBeInTheDocument();
    expect(
      await screen.findByText('Could not reset your password. Please request a new reset link and try again.'),
    ).toBeInTheDocument();
  });

  it('shows invalid-link state when email or token is missing', async () => {
    renderResetPasswordPage('/identity/reset-password');

    expect(await screen.findByText('Recovery link')).toBeInTheDocument();
    expect(await screen.findByText('This password reset link is invalid.')).toBeInTheDocument();
    expect(
      screen.getByText('Please request a new password reset link and try again.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Request a new reset link' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Request a new reset link' })).toHaveAttribute('href', '/identity/forgot-password');
    expect(screen.queryByRole('button', { name: 'Reset password' })).not.toBeInTheDocument();
  });

  it('shows route-context notice when opened with session-expired reason', async () => {
    render(
      <MemoryRouter
        initialEntries={[
          {
            pathname: '/identity/reset-password',
            search: '?email=user%40example.com&token=encoded-token',
            state: { reason: 'sessionExpired' },
          },
        ]}
      >
        <Routes>
          <Route element={<ResetPasswordPage />} path="/identity/reset-password" />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText('Session expired')).toBeInTheDocument();
    expect(screen.getByText('Your session has expired. Please sign in again.')).toBeInTheDocument();
  });
});
