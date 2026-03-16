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
    <Card className="marketplace-listing-card" elevated>
      <div className="marketplace-listing-top">
        <Badge variant="success">{t(`taxonomy.conditions.${listing.condition}`)}</Badge>
        <PriceDisplay className="marketplace-listing-price" value={listing.price} />
      </div>

      <h3>{t(listing.titleKey)}</h3>
      <p className="marketplace-listing-author">{t(listing.authorKey)}</p>
      <p className="marketplace-listing-meta">
        {t(`taxonomy.genres.${listing.genre}`)} | {t(listing.cityKey)}
      </p>

      <Link to={getListingDetailsRoute(listing.id)}>
        <Button fullWidth variant="secondary">
          {t('common.actions.viewListing')}
        </Button>
      </Link>
    </Card>
  );
}
