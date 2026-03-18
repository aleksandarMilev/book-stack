import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';

import { AppFooter } from '@/components/navigation/AppFooter';
import i18n from '@/i18n';

describe('AppFooter', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en');
  });

  it('renders public navigation without canonical books browse link', () => {
    render(
      <MemoryRouter>
        <AppFooter />
      </MemoryRouter>,
    );

    expect(screen.getByRole('link', { name: 'Home' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Marketplace' })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: 'Books' })).not.toBeInTheDocument();
  });
});
