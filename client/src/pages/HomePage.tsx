import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import { Badge, Button, Card, Container, Section } from '@/components/ui';
import { ROUTES } from '@/routes/paths';

const genreKeys = ['fiction', 'nonfiction', 'children', 'science', 'poetry'] as const;
const heroStatKeys = ['statOne', 'statTwo', 'statThree'] as const;
const valueCardKeys = ['buyers', 'sellers'] as const;
const howStepKeys = ['stepOne', 'stepTwo', 'stepThree'] as const;
const conversionTrustKeys = ['statOne', 'statTwo', 'statThree'] as const;

export function HomePage() {
  const { t } = useTranslation();

  return (
    <div className="home-page">
      <section className="home-hero">
        <Container className="home-hero-grid">
          <div className="home-hero-content" data-reveal>
            <div className="home-hero-kicker">
              <Badge variant="accent">{t('home.hero.eyebrow')}</Badge>
              <span className="home-hero-trust-pill">{t('common.labels.trustedMarketplace')}</span>
            </div>
            <h1>{t('home.hero.title')}</h1>
            <p>{t('home.hero.description')}</p>

            <div className="home-hero-actions">
              <Link to={ROUTES.marketplace}>
                <Button className="home-hero-primary-cta" size="lg">
                  {t('home.hero.primaryCta')}
                </Button>
              </Link>
              <a href="#home-how-it-works">
                <Button className="home-hero-secondary-cta" size="lg" variant="secondary">
                  {t('home.hero.secondaryCta')}
                </Button>
              </a>
            </div>
          </div>

          <Card className="home-hero-stats" data-reveal elevated>
            <p className="home-hero-stats-title">{t('common.labels.premiumSelection')}</p>
            <ul className="home-hero-stats-list">
              {heroStatKeys.map((statKey, index) => (
                <li className="home-hero-stats-item" key={statKey}>
                  <span className="home-hero-stats-index">{String(index + 1).padStart(2, '0')}</span>
                  <p>{t(`home.hero.${statKey}`)}</p>
                </li>
              ))}
            </ul>
          </Card>
        </Container>
      </section>

      <Section
        className="home-value home-editorial-section home-editorial-section--value"
        contentClassName="home-value-content home-editorial-surface"
        description={t('home.value.description')}
        title={t('home.value.title')}
      >
        <div className="home-value-grid home-editorial-grid">
          {valueCardKeys.map((valueCardKey, index) => (
            <Card className="home-value-card home-editorial-card" data-reveal key={valueCardKey}>
              <div className="home-value-card-head home-editorial-card-head">
                <span aria-hidden className="home-value-card-index home-editorial-card-index">
                  {String(index + 1).padStart(2, '0')}
                </span>
                <h3>{t(`home.value.${valueCardKey}Title`)}</h3>
              </div>
              <p>{t(`home.value.${valueCardKey}Description`)}</p>
            </Card>
          ))}
        </div>
      </Section>

      <Section
        className="home-genres home-editorial-section home-editorial-section--genres"
        contentClassName="home-genres-content home-editorial-surface"
        description={t('home.genres.description')}
        title={t('home.genres.title')}
      >
        <div className="home-genres-grid home-editorial-grid">
          {genreKeys.map((genreKey, index) => (
            <Card className="home-genre-card home-editorial-card" data-reveal key={genreKey}>
              <div className="home-genre-card-head home-editorial-card-head">
                <span aria-hidden className="home-genre-card-index home-editorial-card-index">
                  {String(index + 1).padStart(2, '0')}
                </span>
                <h3>{t(`taxonomy.genres.${genreKey}`)}</h3>
              </div>
              <p>{t(`home.genres.${genreKey}Description`)}</p>
            </Card>
          ))}
        </div>
      </Section>

      <Section
        className="home-how home-editorial-section home-editorial-section--how"
        contentClassName="home-how-content home-editorial-surface"
        description={t('home.how.description')}
        title={t('home.how.title')}
      >
        <ol className="home-how-grid home-editorial-grid" id="home-how-it-works">
          {howStepKeys.map((stepKey, index) => (
            <li className="home-how-step" data-reveal key={stepKey}>
              <Card className="home-how-card home-editorial-card">
                <div className="home-how-card-head home-editorial-card-head">
                  <span aria-hidden className="home-how-card-index home-editorial-card-index">
                    {String(index + 1).padStart(2, '0')}
                  </span>
                  <h3>{t(`home.how.${stepKey}Title`)}</h3>
                </div>
                <p>{t(`home.how.${stepKey}Description`)}</p>
              </Card>
            </li>
          ))}
        </ol>
      </Section>

      <Section
        className="home-conversion home-editorial-section home-editorial-section--conversion"
        contentClassName="home-conversion-content home-editorial-surface"
        description={t('shell.footerTagline')}
        title={t('home.hero.title')}
      >
        <div className="home-conversion-actions" data-reveal>
          <Link to={ROUTES.marketplace}>
            <Button className="home-conversion-primary-cta" size="lg">
              {t('home.hero.primaryCta')}
            </Button>
          </Link>
          <Link to={ROUTES.register}>
            <Button className="home-conversion-secondary-cta" size="lg" variant="secondary">
              {t('nav.account.register')}
            </Button>
          </Link>
        </div>

        <ul className="home-conversion-trust home-editorial-grid" aria-label={t('common.labels.premiumSelection')}>
          {conversionTrustKeys.map((trustKey, index) => (
            <li className="home-conversion-trust-item" data-reveal key={trustKey}>
              <span aria-hidden className="home-conversion-trust-item-index">
                {String(index + 1).padStart(2, '0')}
              </span>
              <p>{t(`home.hero.${trustKey}`)}</p>
            </li>
          ))}
        </ul>
      </Section>
    </div>
  );
}
