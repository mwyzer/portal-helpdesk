import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import candidatePortalApi from '@/lib/candidatePortalApi';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Calendar, Video, Phone, Building2 } from 'lucide-react';

interface AvailableSlot {
  slotId: string;
  scheduledAt: string;
  durationMinutes: number;
  type: string;
}

interface BookedInterview {
  id: string;
  scheduledAt: string;
  durationMinutes: number;
  type: string;
  status: string;
}

const TYPE_ICONS: Record<string, typeof Video> = { Video, Phone, OnSite: Building2 };

export function PortalInterviewsPage() {
  const queryClient = useQueryClient();
  const [error, setError] = useState('');

  const { data: slots, isLoading: slotsLoading } = useQuery<AvailableSlot[]>({
    queryKey: ['portal-available-slots'],
    queryFn: () => candidatePortalApi.get('/interviews/available-slots').then((r) => r.data),
  });

  const { data: interviews, isLoading: interviewsLoading } = useQuery<BookedInterview[]>({
    queryKey: ['portal-interviews'],
    queryFn: () => candidatePortalApi.get('/interviews').then((r) => r.data),
  });

  const bookMutation = useMutation({
    mutationFn: (slotId: string) => candidatePortalApi.post(`/interviews/slots/${slotId}/book`),
    onSuccess: () => {
      setError('');
      queryClient.invalidateQueries({ queryKey: ['portal-available-slots'] });
      queryClient.invalidateQueries({ queryKey: ['portal-interviews'] });
    },
    onError: (err: unknown) => {
      const resp = (err as { response?: { data?: { message?: string; error?: string } } })?.response?.data;
      setError(resp?.message ?? resp?.error ?? 'This slot could not be booked — it may have just been taken.');
    },
  });

  const hasBookedInterview = (interviews ?? []).some((i) => i.status === 'Scheduled');

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold tracking-tight">Interviews</h1>

      {error && <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}

      <Card>
        <CardHeader>
          <CardTitle>Your Interviews</CardTitle>
        </CardHeader>
        <CardContent>
          {interviewsLoading ? (
            <div className="flex justify-center py-8"><Spinner /></div>
          ) : !interviews || interviews.length === 0 ? (
            <p className="py-4 text-sm text-muted-foreground">No interviews scheduled yet.</p>
          ) : (
            <ul className="divide-y">
              {interviews.map((i) => {
                const Icon = TYPE_ICONS[i.type] ?? Calendar;
                return (
                  <li key={i.id} className="flex items-center gap-3 py-3">
                    <Icon className="h-5 w-5 text-muted-foreground" />
                    <div className="flex-1">
                      <p className="text-sm font-medium">{new Date(i.scheduledAt).toLocaleString()}</p>
                      <p className="text-xs text-muted-foreground">{i.durationMinutes} minutes · {i.type}</p>
                    </div>
                    <Badge variant="outline">{i.status}</Badge>
                  </li>
                );
              })}
            </ul>
          )}
        </CardContent>
      </Card>

      {!hasBookedInterview && (
        <Card>
          <CardHeader>
            <CardTitle>Available Times</CardTitle>
          </CardHeader>
          <CardContent>
            {slotsLoading ? (
              <div className="flex justify-center py-8"><Spinner /></div>
            ) : !slots || slots.length === 0 ? (
              <p className="py-4 text-sm text-muted-foreground">
                No interview times are available yet — check back soon.
              </p>
            ) : (
              <ul className="divide-y">
                {slots.map((slot) => {
                  const Icon = TYPE_ICONS[slot.type] ?? Calendar;
                  return (
                    <li key={slot.slotId} className="flex items-center gap-3 py-3">
                      <Icon className="h-5 w-5 text-muted-foreground" />
                      <div className="flex-1">
                        <p className="text-sm font-medium">{new Date(slot.scheduledAt).toLocaleString()}</p>
                        <p className="text-xs text-muted-foreground">{slot.durationMinutes} minutes · {slot.type}</p>
                      </div>
                      <Button
                        size="sm"
                        onClick={() => bookMutation.mutate(slot.slotId)}
                        disabled={bookMutation.isPending}
                      >
                        Book
                      </Button>
                    </li>
                  );
                })}
              </ul>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
