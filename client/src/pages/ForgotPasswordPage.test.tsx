import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { authService } from '@/features/auth/services/auth.service';
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage';

vi.mock('@/features/auth/services/auth.service', () => ({
  authService: {
    forgotPassword: vi.fn(),
  },
}));

const renderForgotPasswordPage = () =>
  render(
    <MemoryRouter initialEntries={['/identity/forgot-password']}>
      <Routes>
        <Route element={<ForgotPasswordPage />} path="/identity/forgot-password" />
      </Routes>
    </MemoryRouter>,
  );

describe('ForgotPasswordPage', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('submits trimmed email and shows success state', async () => {
    vi.mocked(authService.forgotPassword).mockResolvedValue(
      'If an account exists for that email, a password reset link has been sent.',
    );
    const user = userEvent.setup();

    renderForgotPasswordPage();

    await user.type(screen.getByLabelText('Email'), '  user@example.com  ');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    await waitFor(() => {
      expect(authService.forgotPassword).toHaveBeenCalledWith('user@example.com');
    });
    expect(
      await screen.findByText('If an account exists for this email, a password reset link has been sent.'),
    ).toBeInTheDocument();
  });

  it('shows submit error when request fails', async () => {
    vi.mocked(authService.forgotPassword).mockRejectedValue(new Error('request failed'));
    const user = userEvent.setup();

    renderForgotPasswordPage();

    await user.type(screen.getByLabelText('Email'), 'user@example.com');
    await user.click(screen.getByRole('button', { name: 'Send reset link' }));

    expect(await screen.findByText('Could not request a password reset. Please try again.')).toBeInTheDocument();
  });
});
