import type { ReactNode } from 'react';

import { Card } from '@/components/ui/Card';

interface EmptyStateProps {
  title: string;
  description: string;
  action?: ReactNode;
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <Card className="ui-empty-state">
      <h3>{title}</h3>
      <p>{description}</p>
      {action ? <div className="ui-empty-state-action">{action}</div> : null}
    </Card>
  );
}
