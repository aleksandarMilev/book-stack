import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';

import i18n from '@/i18n';

import { SellerProfileRequiredState } from './SellerProfileRequiredState';

describe('SellerProfileRequiredState', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en');
  });

  it('renders missing-profile state with guidance and actions', () => {
    render(
      <MemoryRouter>
        <SellerProfileRequiredState isInactive={false} />
      </MemoryRouter>,
    );

    expect(screen.getByText('Create your seller profile first')).toBeInTheDocument();
    expect(
      screen.getByText('You need an active seller profile before you can create or manage listings.'),
    ).toBeInTheDocument();
    expect(screen.getByText('Create seller profile')).toBeInTheDocument();

    expect(screen.getByRole('link', { name: 'Open seller profile' })).toHaveAttribute(
      'href',
      '/seller/profile',
    );
    expect(screen.getByRole('link', { name: 'Browse marketplace' })).toHaveAttribute(
      'href',
      '/marketplace',
    );
  });

  it('renders inactive-profile state with inactive status label', () => {
    render(
      <MemoryRouter>
        <SellerProfileRequiredState isInactive />
      </MemoryRouter>,
    );

    expect(screen.getByText('Your seller profile is inactive')).toBeInTheDocument();
    expect(
      screen.getByText('Reactivate your seller profile to continue creating or editing listings.'),
    ).toBeInTheDocument();
    expect(screen.getByText('Inactive')).toBeInTheDocument();
  });
});
