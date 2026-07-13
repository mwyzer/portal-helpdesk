import { cn } from '@/lib/utils';
import { CheckCircle, Circle, Clock } from 'lucide-react';

export interface ApprovalStage {
  label: string;
  status: 'completed' | 'current' | 'pending';
  approverName?: string;
  note?: string;
  date?: string;
}

interface ApprovalTimelineProps {
  stages: ApprovalStage[];
  className?: string;
}

const iconMap = {
  completed: CheckCircle,
  current: Clock,
  pending: Circle,
};

const colorMap = {
  completed: 'text-success',
  current: 'text-info',
  pending: 'text-muted',
};

export function ApprovalTimeline({ stages, className }: ApprovalTimelineProps) {
  return (
    <div className={cn('space-y-0', className)}>
      {stages.map((stage, idx) => {
        const Icon = iconMap[stage.status];
        const isLast = idx === stages.length - 1;

        return (
          <div key={idx} className="flex gap-3">
            {/* Line + Icon column */}
            <div className="flex flex-col items-center">
              <Icon className={cn('h-5 w-5 shrink-0', colorMap[stage.status])} />
              {!isLast && (
                <div
                  className={cn(
                    'w-0.5 flex-1 min-h-[24px]',
                    stage.status === 'completed' ? 'bg-success/50' : 'bg-muted',
                  )}
                />
              )}
            </div>

            {/* Content */}
            <div className={cn('pb-4', isLast && 'pb-0')}>
              <p
                className={cn(
                  'text-sm font-medium',
                  stage.status === 'pending' && 'text-muted-foreground',
                )}
              >
                {stage.label}
              </p>
              {stage.approverName && (
                <p className="text-xs text-muted-foreground">{stage.approverName}</p>
              )}
              {stage.note && (
                <p className="text-xs text-muted-foreground mt-0.5 italic">
                  "{stage.note}"
                </p>
              )}
              {stage.date && (
                <p className="text-xs text-muted-foreground">{stage.date}</p>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
