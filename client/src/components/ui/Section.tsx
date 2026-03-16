import type { PropsWithChildren } from 'react';

import { Container } from '@/components/ui/Container';
import { classNames } from '@/utils/classNames';

interface SectionProps extends PropsWithChildren {
  title?: string;
  description?: string;
  className?: string;
  contentClassName?: string;
}

export function Section({
  title,
  description,
  className,
  contentClassName,
  children,
}: SectionProps) {
  return (
    <section className={classNames('ui-section', className)}>
      <Container>
        {title ? (
          <header className="ui-section-header">
            <h2>{title}</h2>
            {description ? <p>{description}</p> : null}
          </header>
        ) : null}
        <div className={classNames('ui-section-content', contentClassName)}>{children}</div>
      </Container>
    </section>
  );
}
