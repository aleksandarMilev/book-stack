import { useTranslation } from 'react-i18next';

import { Badge, Card, Container } from '@/components/ui';

interface PlaceholderPageProps {
  titleKey: string;
  descriptionKey: string;
}

export function PlaceholderPage({ titleKey, descriptionKey }: PlaceholderPageProps) {
  const { t } = useTranslation();

  return (
    <Container className="placeholder-page">
      <Card className="placeholder-page-card" elevated>
        <Badge>{t('common.labels.comingSoon')}</Badge>
        <h1>{t(titleKey)}</h1>
        <p>{t(descriptionKey)}</p>
      </Card>
    </Container>
  );
}
