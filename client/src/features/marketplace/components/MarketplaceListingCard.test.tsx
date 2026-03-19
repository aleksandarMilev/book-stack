import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';

import { MarketplaceListingCard } from '@/features/marketplace/components/MarketplaceListingCard';
import i18n from '@/i18n';
import type { MarketplaceListing } from '@/types/marketplace.types';
import { toPriceDisplayValue } from '@/utils/priceDisplay';

const createListing = (overrides: Partial<MarketplaceListing> = {}): MarketplaceListing => ({
  id: 'listing-1',
  bookId: 'book-1',
  title: 'The Pragmatic Reader',
  author: 'Nadia Vale',
  genre: 'Science',
  publisher: 'North Press',
  publishedOn: '2024-01-01T00:00:00Z',
  isbn: '9781234567890',
  creatorId: 'seller-1',
  supportsOnlinePayment: true,
  supportsCashOnDelivery: true,
  condition: 'likeNew',
  quantity: 2,
  description: 'A practical guide for reading systems.',
  imageUrl: 'https://cdn.example.test/cover.png',
  isApproved: true,
  rejectionReason: null,
  createdOn: '2026-01-01T00:00:00Z',
  modifiedOn: null,
  price: toPriceDisplayValue(24.5, 'EUR'),
  ...overrides,
});

describe('MarketplaceListingCard', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en');
  });

  it('renders core listing details and CTA link', () => {
    const listing = createListing();

    const { container } = render(
      <MemoryRouter>
        <MarketplaceListingCard listing={listing} />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { level: 3, name: listing.title })).toBeInTheDocument();
    expect(screen.getByText(listing.author)).toBeInTheDocument();
    expect(screen.getByText(listing.genre)).toBeInTheDocument();
    expect(screen.getByText('Like new')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'View listing' })).toHaveAttribute(
      'href',
      `/marketplace/${listing.id}`,
    );
    expect(container.querySelector('.marketplace-listing-card--discovery')).not.toBeNull();
    expect(container.querySelector('.marketplace-listing-content')).not.toBeNull();
  });

  it('renders placeholder media when listing has no image', () => {
    const listing = createListing({ imageUrl: '' });
    const { container } = render(
      <MemoryRouter>
        <MarketplaceListingCard listing={listing} />
      </MemoryRouter>,
    );

    expect(container.querySelector('.marketplace-listing-image')).toBeNull();
    expect(container.querySelector('.marketplace-listing-image-placeholder')).not.toBeNull();
  });
});
