import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';

import i18n from '@/i18n';
import { HomePage } from '@/pages/HomePage';

const renderHomePage = (initialState?: { from?: string; reason?: string }) =>
  render(
    <MemoryRouter initialEntries={[{ pathname: '/', ...(initialState ? { state: initialState } : {}) }]}>
      <HomePage />
    </MemoryRouter>,
  );

describe('HomePage', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en');
  });

  it('keeps homepage flow sections in expected order through conversion block', () => {
    const { container } = renderHomePage();
    const sections = Array.from(container.querySelectorAll('.home-page > section')).map((section) =>
      section.className,
    );

    expect(sections).toHaveLength(5);
    expect(sections[0]).toContain('home-hero');
    expect(sections[1]).toContain('home-value');
    expect(sections[2]).toContain('home-genres');
    expect(sections[3]).toContain('home-how');
    expect(sections[4]).toContain('home-conversion');
  });

  it('renders conversion CTA actions and trust items', () => {
    renderHomePage();

    const browseLinks = screen.getAllByRole('link', { name: 'Browse marketplace' });
    expect(browseLinks.some((link) => link.getAttribute('href') === '/marketplace')).toBe(true);

    const registerLinks = screen.getAllByRole('link', { name: 'Register' });
    expect(registerLinks.some((link) => link.getAttribute('href') === '/register')).toBe(true);

    const conversionTrustItems = document.querySelectorAll('.home-conversion-trust-item');
    expect(conversionTrustItems).toHaveLength(3);
  });

  it('shows admin access notice when redirected from an admin route', () => {
    renderHomePage({ from: '/admin', reason: 'adminAccessRequired' });

    expect(screen.getByText('Administrator access required')).toBeInTheDocument();
    expect(
      screen.getByText('This section is available only to administrator accounts.'),
    ).toBeInTheDocument();

    const marketplaceLinks = screen.getAllByRole('link', { name: 'Browse marketplace' });
    expect(marketplaceLinks.some((link) => link.getAttribute('href') === '/marketplace')).toBe(
      true,
    );
    expect(screen.getByRole('link', { name: 'Profile' })).toHaveAttribute('href', '/profile');
  });
});
