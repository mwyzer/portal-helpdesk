import { useQuery } from '@tanstack/react-query';
import candidatePortalApi from '@/lib/candidatePortalApi';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';

interface StatusResponse {
  jobVacancyTitle: string;
  stage: string;
  rejectionReason: string | null;
  appliedAt: string;
}

const STAGE_COLORS: Record<string, string> = {
  Applied: 'bg-slate-100 text-slate-700',
  Screening: 'bg-blue-100 text-blue-700',
  Test: 'bg-purple-100 text-purple-700',
  Interview: 'bg-amber-100 text-amber-700',
  Offering: 'bg-emerald-100 text-emerald-700',
  Hired: 'bg-green-100 text-green-700',
  Rejected: 'bg-red-100 text-red-700',
};

export function PortalStatusPage() {
  const { data, isLoading } = useQuery<StatusResponse>({
    queryKey: ['portal-status'],
    queryFn: () => candidatePortalApi.get('/status').then((r) => r.data),
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner />
      </div>
    );
  }

  if (!data) return null;

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold tracking-tight">Application Status</h1>

      <Card>
        <CardHeader>
          <CardTitle>{data.jobVacancyTitle}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Current stage:</span>
            <Badge className={STAGE_COLORS[data.stage] || ''}>{data.stage}</Badge>
          </div>

          {data.stage === 'Rejected' && data.rejectionReason && (
            <div className="rounded-md bg-muted p-3 text-sm">
              <p className="font-medium">Note from the hiring team:</p>
              <p className="text-muted-foreground">{data.rejectionReason}</p>
            </div>
          )}

          <p className="text-xs text-muted-foreground">
            Applied on {new Date(data.appliedAt).toLocaleDateString()}
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
