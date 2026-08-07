import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { CheckCircle, XCircle, Clock } from 'lucide-react';
import { useToastStore } from '@/lib/useToast';

interface EscalationItem {
  id: string;
  ticketId: string;
  escalatedByName: string;
  reason: string;
  status: string;
  resolvedAt?: string;
  createdAt: string;
}

const STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-800',
  Accepted: 'bg-blue-100 text-blue-800',
  Resolved: 'bg-green-100 text-green-800',
  Declined: 'bg-red-100 text-red-800',
};

export function EscalationsPage() {
  const queryClient = useQueryClient();

  const { data: escalations, isLoading } = useQuery({
    queryKey: ['escalations'],
    queryFn: () => api.get('/escalations').then(r => r.data),
  });

  const addToast = useToastStore((s) => s.addToast);
  const onMutationError = (err: unknown) => {
    const resp = (err as { response?: { data?: { message?: string; error?: string } } })?.response?.data;
    addToast({ title: 'Action failed', message: resp?.message ?? resp?.error ?? 'Something went wrong. Please try again.', type: 'error' });
  };

  const acceptMutation = useMutation({
    mutationFn: (id: string) => api.post(`/escalations/${id}/accept`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['escalations'] }),
    onError: onMutationError,
  });

  const declineMutation = useMutation({
    mutationFn: (id: string) => api.post(`/escalations/${id}/decline`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['escalations'] }),
    onError: onMutationError,
  });

  const resolveMutation = useMutation({
    mutationFn: (id: string) => api.post(`/escalations/${id}/resolve`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['escalations'] }),
    onError: onMutationError,
  });

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Escalations</h1>
      <div className="space-y-3">
        {(escalations as any)?.items?.map((e: EscalationItem) => (
          <Card key={e.id}>
            <CardContent className="py-4 flex items-center justify-between">
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">Ticket #{e.ticketId.substring(0, 8)}</span>
                  <Badge className={STATUS_COLORS[e.status]}>{e.status}</Badge>
                </div>
                <div className="text-sm text-muted-foreground mt-1">
                  Escalated by {e.escalatedByName}: {e.reason}
                </div>
                <div className="text-xs text-muted-foreground mt-1">{new Date(e.createdAt).toLocaleString()}</div>
              </div>
              {e.status === 'Pending' && (
                <div className="flex gap-2">
                  <Button size="sm" onClick={() => acceptMutation.mutate(e.id)}>
                    <CheckCircle className="mr-1 h-4 w-4" /> Accept
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => declineMutation.mutate(e.id)}>
                    <XCircle className="mr-1 h-4 w-4" /> Decline
                  </Button>
                </div>
              )}
              {e.status === 'Accepted' && (
                <Button size="sm" onClick={() => resolveMutation.mutate(e.id)}>
                  <CheckCircle className="mr-1 h-4 w-4" /> Resolve
                </Button>
              )}
            </CardContent>
          </Card>
        ))}
        {(!escalations || (escalations as any)?.items?.length === 0) && (
          <div className="text-center py-12 text-muted-foreground">No escalations found</div>
        )}
      </div>
    </div>
  );
}
