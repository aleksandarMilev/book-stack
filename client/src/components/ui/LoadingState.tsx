interface LoadingStateProps {
  title: string;
  description?: string;
}

export function LoadingState({ title, description }: LoadingStateProps) {
  return (
    <div aria-live="polite" className="ui-loading-state" role="status">
      <span aria-hidden className="ui-spinner" />
      <div>
        <p className="ui-loading-title">{title}</p>
        {description ? <p className="ui-loading-description">{description}</p> : null}
      </div>
    </div>
  );
}
