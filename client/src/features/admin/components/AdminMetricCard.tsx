import { Card } from '@/components/ui';

interface AdminMetricCardProps {
  label: string;
  value: number;
}

export function AdminMetricCard({ label, value }: AdminMetricCardProps) {
  return (
    <Card className="admin-metric-card">
      <p className="admin-metric-label">{label}</p>
      <p className="admin-metric-value">{value.toLocaleString()}</p>
    </Card>
  );
}
