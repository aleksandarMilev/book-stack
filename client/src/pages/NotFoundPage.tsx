import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { Button, Card, Container } from '@/components/ui';
import { ROUTES } from '@/routes/paths';

export function NotFoundPage() {
  const { t } = useTranslation();

  return (
    <Container className="placeholder-page">
      <Card className="placeholder-page-card" elevated>
        <h1>{t('pages.notFound.title')}</h1>
        <p>{t('pages.notFound.description')}</p>
        <Link to={ROUTES.home}>
          <Button>{t('pages.notFound.action')}</Button>
        </Link>
      </Card>
    </Container>
  );
}
