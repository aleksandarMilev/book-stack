import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { Badge, Button, Card, Container, Section } from '@/components/ui';
import { ROUTES } from '@/routes/paths';

const genreKeys = ['fiction', 'nonfiction', 'children', 'science', 'poetry'] as const;

export function HomePage() {
  const { t } = useTranslation();

  return (
    <div className="home-page">
      <section className="home-hero">
        <Container className="home-hero-grid">
          <div className="home-hero-content" data-reveal>
            <Badge variant="accent">{t('home.hero.eyebrow')}</Badge>
            <h1>{t('home.hero.title')}</h1>
            <p>{t('home.hero.description')}</p>

            <div className="home-hero-actions">
              <Link to={ROUTES.marketplace}>
                <Button size="lg">{t('home.hero.primaryCta')}</Button>
              </Link>
              <a href="#home-how-it-works">
                <Button size="lg" variant="secondary">
                  {t('home.hero.secondaryCta')}
                </Button>
              </a>
            </div>
          </div>

          <Card className="home-hero-stats" data-reveal elevated>
            <p>{t('home.hero.statOne')}</p>
            <p>{t('home.hero.statTwo')}</p>
            <p>{t('home.hero.statThree')}</p>
          </Card>
        </Container>
      </section>

      <Section
        className="home-value"
        description={t('home.value.description')}
        title={t('home.value.title')}
      >
        <div className="home-value-grid">
          <Card data-reveal>
            <h3>{t('home.value.buyersTitle')}</h3>
            <p>{t('home.value.buyersDescription')}</p>
          </Card>
          <Card data-reveal>
            <h3>{t('home.value.sellersTitle')}</h3>
            <p>{t('home.value.sellersDescription')}</p>
          </Card>
        </div>
      </Section>

      <Section
        className="home-genres"
        description={t('home.genres.description')}
        title={t('home.genres.title')}
      >
        <div className="home-genres-grid">
          {genreKeys.map((genreKey) => (
            <Card className="home-genre-card" data-reveal key={genreKey}>
              <h3>{t(`taxonomy.genres.${genreKey}`)}</h3>
              <p>{t(`home.genres.${genreKey}Description`)}</p>
            </Card>
          ))}
        </div>
      </Section>

      <Section
        className="home-how"
        description={t('home.how.description')}
        title={t('home.how.title')}
      >
        <ol className="home-how-grid" id="home-how-it-works">
          <li data-reveal>
            <Card>
              <h3>{t('home.how.stepOneTitle')}</h3>
              <p>{t('home.how.stepOneDescription')}</p>
            </Card>
          </li>
          <li data-reveal>
            <Card>
              <h3>{t('home.how.stepTwoTitle')}</h3>
              <p>{t('home.how.stepTwoDescription')}</p>
            </Card>
          </li>
          <li data-reveal>
            <Card>
              <h3>{t('home.how.stepThreeTitle')}</h3>
              <p>{t('home.how.stepThreeDescription')}</p>
            </Card>
          </li>
        </ol>
      </Section>
    </div>
  );
}
