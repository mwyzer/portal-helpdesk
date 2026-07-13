import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';

export interface LeaveBalanceCardProps {
  leaveTypeId: string;
  leaveTypeName: string;
  totalDays: number;
  usedDays: number;
  remainingDays: number;
}

export function LeaveBalanceCard({
  leaveTypeName,
  totalDays,
  usedDays,
  remainingDays,
}: LeaveBalanceCardProps) {
  const pct = totalDays > 0 ? (remainingDays / totalDays) * 100 : 0;
  const barColor =
    pct >= 50 ? 'bg-green-500' : pct >= 25 ? 'bg-yellow-500' : 'bg-red-500';

  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex items-center justify-between mb-2">
          <span className="font-medium">{leaveTypeName}</span>
          <span className="text-sm text-muted-foreground">
            {remainingDays} / {totalDays} days
          </span>
        </div>
        <div className="h-2 w-full rounded-full bg-secondary">
          <div
            className={cn('h-2 rounded-full transition-all', barColor)}
            style={{ width: `${Math.min(pct, 100)}%` }}
          />
        </div>
        {usedDays > 0 && (
          <p className="text-xs text-muted-foreground mt-2">
            {usedDays} day{usedDays !== 1 ? 's' : ''} used this year
          </p>
        )}
      </CardContent>
    </Card>
  );
}
