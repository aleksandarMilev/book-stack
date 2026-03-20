import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { authService } from '@/features/auth/services/auth.service';
import { RegisterPage } from '@/pages/RegisterPage';
import { useAuthStore } from '@/store/auth.store';

vi.mock('@/features/auth/services/auth.service', () => ({
  authService: {
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  },
}));

const renderRegisterPage = (initialState?: { reason?: string; from?: string }) =>
  render(
    <MemoryRouter initialEntries={[{ pathname: '/register', ...(initialState ? { state: initialState } : {}) }]}>
      <Routes>
        <Route element={<RegisterPage />} path="/register" />
      </Routes>
    </MemoryRouter>,
  );

describe('RegisterPage', () => {
  afterEach(() => {
    useAuthStore.setState({ session: null });
    vi.clearAllMocks();
  });

  it('maps form values to register payload and trims text fields', async () => {
    vi.mocked(authService.register).mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderRegisterPage();

    await user.type(screen.getByLabelText('Username'), '  alice  ');
    await user.type(screen.getByLabelText('Email'), '  alice@example.com  ');
    await user.type(screen.getByLabelText('First name'), '  Alice  ');
    await user.type(screen.getByLabelText('Last name'), '  Johnson  ');
    await user.type(screen.getByLabelText('Password'), 'Password123');
    await user.type(screen.getByLabelText('Confirm password'), 'Password123');

    await user.click(screen.getByRole('button', { name: 'Create account' }));

    await waitFor(() => {
      expect(authService.register).toHaveBeenCalledWith({
        username: 'alice',
        email: 'alice@example.com',
        password: 'Password123',
        firstName: 'Alice',
        lastName: 'Johnson',
        image: null,
      });
    });
  });

  it('shows route-context notice when register is opened with auth-required reason', async () => {
    renderRegisterPage({ from: '/profile', reason: 'authRequired' });

    expect(await screen.findByText('Sign in required')).toBeInTheDocument();
    expect(screen.getByText('Please sign in to continue.')).toBeInTheDocument();
  });
});
