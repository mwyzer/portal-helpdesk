import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Badge } from '@/components/ui/badge';
import { CheckCheck, Filter } from 'lucide-react';

interface NotificationItem {
  id: string;
  title: string;
  body: string;
  type: string;
  referenceType?: string;
  referenceId?: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

interface NotificationListResponse {
  items: NotificationItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const typeIcon: Record<string, string> = {
  LeaveSubmitted: '📩',
  LeaveApproved: '✅',
  LeaveRejected: '❌',
  LeaveCancelled: '↩️',
  BalanceAdjusted: '⚖️',
  General: '🔔',
};

export function NotificationCenterPage() {
  const queryClient = useQueryClient();
  const [page] = useState(1);
  const [filter, setFilter] = useState<'all' | 'unread' | 'read'>('all');

  const { data, isLoading } = useQuery<NotificationListResponse>({
    queryKey: ['notifications', 'all', page, filter],
    queryFn: () => {
      const isReadParam =
        filter === 'unread' ? 'false' : filter === 'read' ? 'true' : undefined;
      const params = new URLSearchParams({ page: String(page), pageSize: '50' });
      if (isReadParam) params.set('isRead', isReadParam);
      return api.get(`/notifications?${params.toString()}`).then((r) => r.data);
    },
  });

  const markReadMutation = useMutation({
    mutationFn: (id: string) => api.put(`/notifications/${id}/read`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  const markAllReadMutation = useMutation({
    mutationFn: () => api.put('/notifications/read-all'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const items = data?.items ?? [];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Notifications</h1>
          <p className="text-muted-foreground">Stay updated with your alerts</p>
        </div>
        <Button
          variant="outline"
          onClick={() => markAllReadMutation.mutate()}
          disabled={markAllReadMutation.isPending}
        >
          <CheckCheck className="mr-2 h-4 w-4" />
          Mark All Read
        </Button>
      </div>

      {/* Filter Tabs */}
      <div className="flex gap-2">
        {(['all', 'unread', 'read'] as const).map((f) => (
          <Button
            key={f}
            variant={filter === f ? 'default' : 'outline'}
            size="sm"
            onClick={() => setFilter(f)}
          >
            <Filter className="mr-1 h-3 w-3" />
            {f.charAt(0).toUpperCase() + f.slice(1)}
          </Button>
        ))}
      </div>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex justify-center py-12">
              <Spinner />
            </div>
          ) : items.length === 0 ? (
            <div className="py-12 text-center text-muted-foreground">
              <p className="text-lg">No notifications</p>
              <p className="text-sm mt-1">You're all caught up!</p>
            </div>
          ) : (
            <div className="divide-y">
              {items.map((n) => (
                <div
                  key={n.id}
                  className={cn(
                    'flex items-start gap-4 px-6 py-4 transition-colors hover:bg-muted/50',
                    !n.isRead && 'bg-primary/5',
                  )}
                >
                  <span className="text-xl mt-0.5 shrink-0">
                    {typeIcon[n.type] || '🔔'}
                  </span>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p
                        className={cn(
                          'text-sm font-medium',
                          !n.isRead && 'text-foreground',
                        )}
                      >
                        {n.title}
                      </p>
                      {!n.isRead && (
                        <Badge variant="default" className="text-[10px] h-5 px-1.5">
                          New
                        </Badge>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground mt-0.5">{n.body}</p>
                    <p className="text-xs text-muted-foreground mt-1">
                      {formatDate(n.createdAt)}
                    </p>
                  </div>
                  {!n.isRead && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="shrink-0 text-xs"
                      onClick={() => markReadMutation.mutate(n.id)}
                    >
                      Mark read
                    </Button>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {data && data.totalCount > 50 && (
        <p className="text-center text-sm text-muted-foreground">
          Showing 50 of {data.totalCount} notifications
        </p>
      )}
    </div>
  );
}
