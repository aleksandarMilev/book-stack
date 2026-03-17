import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { Button, EmptyState } from '@/components/ui';
import { ROUTES } from '@/routes/paths';

interface SellerProfileRequiredStateProps {
  isInactive: boolean;
}

export function SellerProfileRequiredState({ isInactive }: SellerProfileRequiredStateProps) {
  const { t } = useTranslation();

  return (
    <EmptyState
      action={
        <Link to={ROUTES.sellerProfile}>
          <Button>{t('pages.myListings.openSellerProfileCta')}</Button>
        </Link>
      }
      description={
        isInactive
          ? t('pages.myListings.inactiveSellerProfileDescription')
          : t('pages.myListings.noSellerProfileDescription')
      }
      title={
        isInactive
          ? t('pages.myListings.inactiveSellerProfileTitle')
          : t('pages.myListings.noSellerProfileTitle')
      }
    />
  );
}
