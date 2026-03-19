import { useTranslation } from 'react-i18next';

import { Button, Card } from '@/components/ui';
import { Input } from '@/components/ui/Input';
import { MARKETPLACE_CONDITIONS } from '@/features/marketplace/types';
import type { MarketplaceListingCondition } from '@/types/marketplace.types';
import { classNames } from '@/utils/classNames';

interface MarketplaceFiltersProps {
  className?: string;
  title: string;
  author: string;
  genre: string;
  priceFrom: string;
  priceTo: string;
  selectedCondition: MarketplaceListingCondition | 'all';
  onTitleChange: (value: string) => void;
  onAuthorChange: (value: string) => void;
  onGenreChange: (value: string) => void;
  onPriceFromChange: (value: string) => void;
  onPriceToChange: (value: string) => void;
  onConditionChange: (condition: MarketplaceListingCondition | 'all') => void;
  onClearFilters: () => void;
}

export function MarketplaceFilters({
  className,
  title,
  author,
  genre,
  priceFrom,
  priceTo,
  selectedCondition,
  onTitleChange,
  onAuthorChange,
  onGenreChange,
  onPriceFromChange,
  onPriceToChange,
  onConditionChange,
  onClearFilters,
}: MarketplaceFiltersProps) {
  const { t } = useTranslation();

  return (
    <Card className={classNames('marketplace-filters', className)}>
      <div className="marketplace-filters-block marketplace-filters-block--fields">
        <h3>{t('marketplace.filtersTitle')}</h3>
        <Input
          label={t('marketplace.titleLabel')}
          onChange={(event) => {
            onTitleChange(event.target.value);
          }}
          value={title}
        />
        <Input
          label={t('marketplace.authorLabel')}
          onChange={(event) => {
            onAuthorChange(event.target.value);
          }}
          value={author}
        />
        <Input
          label={t('marketplace.genreLabel')}
          onChange={(event) => {
            onGenreChange(event.target.value);
          }}
          value={genre}
        />
        <div className="marketplace-price-range">
          <Input
            inputMode="decimal"
            label={t('marketplace.priceFromLabel')}
            min={0}
            onChange={(event) => {
              onPriceFromChange(event.target.value);
            }}
            step="0.01"
            type="number"
            value={priceFrom}
          />
          <Input
            inputMode="decimal"
            label={t('marketplace.priceToLabel')}
            min={0}
            onChange={(event) => {
              onPriceToChange(event.target.value);
            }}
            step="0.01"
            type="number"
            value={priceTo}
          />
        </div>
      </div>

      <div className="marketplace-filters-block marketplace-filters-block--conditions">
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

      <Button className="marketplace-filters-clear" onClick={onClearFilters} variant="ghost">
        {t('common.actions.clearFilters')}
      </Button>
    </Card>
  );
}
