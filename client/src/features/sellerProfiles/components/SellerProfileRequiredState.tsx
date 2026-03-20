import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { Badge, Button, Card } from '@/components/ui';
import { ROUTES } from '@/routes/paths';

interface SellerProfileRequiredStateProps {
  isInactive: boolean;
}

export function SellerProfileRequiredState({ isInactive }: SellerProfileRequiredStateProps) {
  const { t } = useTranslation();
  const title = isInactive
    ? t('pages.myListings.inactiveSellerProfileTitle')
    : t('pages.myListings.noSellerProfileTitle');
  const description = isInactive
    ? t('pages.myListings.inactiveSellerProfileDescription')
    : t('pages.myListings.noSellerProfileDescription');

  return (
    <Card className="seller-profile-required-card" elevated>
      <div className="seller-profile-required-head">
        <Badge className="seller-profile-required-badge" variant={isInactive ? 'warning' : 'accent'}>
          {isInactive ? t('pages.sellerProfile.inactiveBadge') : t('pages.sellerProfile.createTitle')}
        </Badge>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>

      <div className="seller-profile-required-note">
        <p>{t('pages.sellerProfile.statusControlledByAdmins')}</p>
      </div>

      <div className="seller-profile-required-actions">
        <Link to={ROUTES.sellerProfile}>
          <Button>{t('pages.myListings.openSellerProfileCta')}</Button>
        </Link>
        <Link to={ROUTES.marketplace}>
          <Button variant="secondary">{t('common.actions.browseMarketplace')}</Button>
        </Link>
      </div>
    </Card>
  );
}
