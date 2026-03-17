import { Card } from '@/components/ui';

interface AdminMetricCardProps {
  label: string;
  value: number | string;
}

export function AdminMetricCard({ label, value }: AdminMetricCardProps) {
  const displayValue = typeof value === 'number' ? value.toLocaleString() : value;

  return (
    <Card className="admin-metric-card">
      <p className="admin-metric-label">{label}</p>
      <p className="admin-metric-value">{displayValue}</p>
    </Card>
  );
}
