import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { RefreshCw, CheckCircle2, XCircle } from 'lucide-react';

interface ActionItemResponse {
  id: string;
  meetingId: string | null;
  meetingTitle: string | null;
  title: string;
  description: string | null;
  assignedToId: string;
  assignedToName: string;
  dueDate: string;
  priority: string;
  status: string;
  completedAt: string | null;
  createdAt: string;
}

const priorityColors: Record<string, string> = {
  Low: 'bg-gray-100 text-gray-700',
  Medium: 'bg-blue-100 text-blue-700',
  High: 'bg-orange-100 text-orange-700',
  Urgent: 'bg-red-100 text-red-700',
};

const statusColors: Record<string, string> = {
  Open: 'bg-yellow-100 text-yellow-800',
  InProgress: 'bg-blue-100 text-blue-800',
  Completed: 'bg-green-100 text-green-800',
  Cancelled: 'bg-gray-100 text-gray-800',
};

export function ActionItemsPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState('');

  const { data, isLoading } = useQuery<{ items: ActionItemResponse[]; totalCount: number }>({
    queryKey: ['action-items', statusFilter],
    queryFn: () =>
      api.get('/action-items', { params: { pageSize: 50, status: statusFilter || undefined } }).then((r) => r.data),
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => api.post(`/action-items/${id}/complete`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['action-items'] }),
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Action Items</h1>
          <p className="text-muted-foreground">Track your tasks and follow-ups</p>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {['', 'Open', 'InProgress', 'Completed', 'Cancelled'].map((s) => (
          <Button
            key={s || 'all'}
            variant={statusFilter === s ? 'default' : 'outline'}
            size="sm"
            onClick={() => setStatusFilter(s)}
          >
            {s || 'All'}
          </Button>
        ))}
      </div>

      <Card>
        <CardHeader className="pb-0 flex-row items-center justify-between">
          <CardTitle>My Action Items</CardTitle>
          <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['action-items'] })}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </CardHeader>
        <CardContent className="pt-4">
          {isLoading ? (
            <div className="flex justify-center py-8"><Spinner className="h-8 w-8" /></div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Task</TableHead>
                  <TableHead>Assigned To</TableHead>
                  <TableHead>Due Date</TableHead>
                  <TableHead>Priority</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-24">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.items?.map((a) => (
                  <TableRow key={a.id} className={a.status === 'Completed' ? 'opacity-60' : ''}>
                    <TableCell>
                      <div className="font-medium">{a.title}</div>
                      {a.meetingTitle && <div className="text-xs text-muted-foreground">From: {a.meetingTitle}</div>}
                    </TableCell>
                    <TableCell className="text-sm">{a.assignedToName}</TableCell>
                    <TableCell className="text-sm">
                      <span className={new Date(a.dueDate) < new Date() && a.status !== 'Completed' ? 'text-red-600 font-medium' : ''}>
                        {new Date(a.dueDate).toLocaleDateString()}
                      </span>
                    </TableCell>
                    <TableCell><Badge className={priorityColors[a.priority] || ''}>{a.priority}</Badge></TableCell>
                    <TableCell><Badge className={statusColors[a.status] || ''}>{a.status}</Badge></TableCell>
                    <TableCell>
                      {a.status !== 'Completed' && a.status !== 'Cancelled' && (
                        <Button variant="ghost" size="icon" onClick={() => completeMutation.mutate(a.id)} title="Mark complete">
                          <CheckCircle2 className="h-4 w-4 text-green-600" />
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
                {(!data?.items || data.items.length === 0) && (
                  <TableRow><TableCell colSpan={6} className="text-center py-8 text-muted-foreground">No action items found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
