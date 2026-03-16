import { useTranslation } from 'react-i18next';

import { Button, Card } from '@/components/ui';
import { MARKETPLACE_CONDITIONS, MARKETPLACE_GENRES } from '@/features/marketplace/types';
import type { ListingCondition, MarketplaceGenre } from '@/types/marketplace.types';
import { classNames } from '@/utils/classNames';

interface MarketplaceFiltersProps {
  className?: string;
  selectedGenres: MarketplaceGenre[];
  selectedCondition: ListingCondition | 'all';
  onToggleGenre: (genre: MarketplaceGenre) => void;
  onConditionChange: (condition: ListingCondition | 'all') => void;
  onClearFilters: () => void;
}

export function MarketplaceFilters({
  className,
  selectedGenres,
  selectedCondition,
  onToggleGenre,
  onConditionChange,
  onClearFilters,
}: MarketplaceFiltersProps) {
  const { t } = useTranslation();

  return (
    <Card className={classNames('marketplace-filters', className)}>
      <div className="marketplace-filters-block">
        <h3>{t('marketplace.genreLabel')}</h3>
        <div className="marketplace-filters-options">
          {MARKETPLACE_GENRES.map((genre) => (
            <label className="filter-checkbox" key={genre}>
              <input
                checked={selectedGenres.includes(genre)}
                onChange={() => {
                  onToggleGenre(genre);
                }}
                type="checkbox"
              />
              <span>{t(`taxonomy.genres.${genre}`)}</span>
            </label>
          ))}
        </div>
      </div>

      <div className="marketplace-filters-block">
        <h3>{t('marketplace.conditionLabel')}</h3>
        <div className="marketplace-filters-options">
          <label className="filter-radio">
            <input
              checked={selectedCondition === 'all'}
              name="condition"
              onChange={() => {
                onConditionChange('all');
              }}
              type="radio"
            />
            <span>{t('taxonomy.conditions.all')}</span>
          </label>

          {MARKETPLACE_CONDITIONS.map((condition) => (
            <label className="filter-radio" key={condition}>
              <input
                checked={selectedCondition === condition}
                name="condition"
                onChange={() => {
                  onConditionChange(condition);
                }}
                type="radio"
              />
              <span>{t(`taxonomy.conditions.${condition}`)}</span>
            </label>
          ))}
        </div>
      </div>

      <Button onClick={onClearFilters} variant="ghost">
        {t('common.actions.clearFilters')}
      </Button>
    </Card>
  );
}
