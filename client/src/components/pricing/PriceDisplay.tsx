import { useMemo } from 'react';

import { useLanguage } from '@/hooks/useLanguage';
import type { PriceDisplayValue } from '@/types/pricing.types';
import { classNames } from '@/utils/classNames';
import { formatMoney } from '@/utils/formatters';

interface PriceDisplayProps {
  value: PriceDisplayValue;
  className?: string;
}

export function PriceDisplay({ value, className }: PriceDisplayProps) {
  const { language } = useLanguage();

  const primaryValue = useMemo(
    () => formatMoney({ amount: value.primary.amount, currency: value.primary.currency, language }),
    [language, value.primary.amount, value.primary.currency],
  );

  const secondaryValue = useMemo(() => {
    if (!value.secondary) {
      return null;
    }

    return formatMoney({ amount: value.secondary.amount, currency: value.secondary.currency, language });
  }, [language, value.secondary]);

  return (
    <div className={classNames('price-display', className)}>
      <span className="price-display-primary">{primaryValue}</span>
      {secondaryValue ? <span className="price-display-secondary">{secondaryValue}</span> : null}
    </div>
  );
}
