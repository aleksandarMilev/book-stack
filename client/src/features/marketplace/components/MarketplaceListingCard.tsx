import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { PriceDisplay } from '@/components/pricing/PriceDisplay';
import { Badge, Button, Card } from '@/components/ui';
import { getListingDetailsRoute } from '@/routes/paths';
import type { MarketplaceListing } from '@/types/marketplace.types';

interface MarketplaceListingCardProps {
  listing: MarketplaceListing;
}

export function MarketplaceListingCard({ listing }: MarketplaceListingCardProps) {
  const { t } = useTranslation();

  return (
    <Card className="marketplace-listing-card marketplace-listing-card--discovery" data-reveal elevated>
      <div className="marketplace-listing-media">
        {listing.imageUrl ? (
          <img
            alt={t('marketplace.listingImageAlt', { title: listing.title })}
            className="marketplace-listing-image"
            src={listing.imageUrl}
          />
        ) : (
          <div className="marketplace-listing-image-placeholder" />
        )}
      </div>

      <div className="marketplace-listing-top">
        <Badge className="marketplace-listing-condition" variant="success">
          {t(`taxonomy.conditions.${listing.condition}`)}
        </Badge>
        <PriceDisplay className="marketplace-listing-price" value={listing.price} />
      </div>

      <div className="marketplace-listing-content">
        <h3>{listing.title}</h3>
        <p className="marketplace-listing-author">{listing.author}</p>
        <p className="marketplace-listing-meta">{listing.genre}</p>
      </div>

      <Link to={getListingDetailsRoute(listing.id)}>
        <Button className="marketplace-listing-cta" fullWidth variant="secondary">
          {t('common.actions.viewListing')}
        </Button>
      </Link>
    </Card>
  );
}
