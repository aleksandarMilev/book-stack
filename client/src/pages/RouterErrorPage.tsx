import { useTranslation } from 'react-i18next';
import { isRouteErrorResponse, Link, useRouteError } from 'react-router-dom';

import { AppFooter } from '@/components/navigation/AppFooter';
import { AppHeader } from '@/components/navigation/AppHeader';
import { Button, Card, Container } from '@/components/ui';
import { ROUTES } from '@/routes/paths';

export function RouterErrorPage() {
  const { t } = useTranslation();
  const routeError = useRouteError();

  const statusCode = isRouteErrorResponse(routeError) ? routeError.status : null;

  return (
    <div className="app-shell">
      <AppHeader />
      <main className="app-main">
        <Container className="placeholder-page">
          <Card className="placeholder-page-card" elevated>
            {statusCode ? <p className="placeholder-error-status">{statusCode}</p> : null}
            <h1>{t('pages.notFound.title')}</h1>
            <p>{t('pages.notFound.description')}</p>
            <Link to={ROUTES.home}>
              <Button>{t('pages.notFound.action')}</Button>
            </Link>
          </Card>
        </Container>
      </main>
      <AppFooter />
    </div>
  );
}
