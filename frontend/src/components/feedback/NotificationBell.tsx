import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Bell } from 'lucide-react';
import api from '@/lib/axios';
import { useSignalR } from '@/lib/useSignalR';
import { useToastStore } from '@/lib/useToast';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { cn } from '@/lib/utils';

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

const typeIcon: Record<string, string> = {
  LeaveSubmitted: '📩',
  LeaveApproved: '✅',
  LeaveRejected: '❌',
  LeaveCancelled: '↩️',
  BalanceAdjusted: '⚖️',
  General: '🔔',
};

export function NotificationBell() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const addToast = useToastStore((s) => s.addToast);

  const { data: unreadCount = 0 } = useQuery<number>({
    queryKey: ['notifications', 'unread-count'],
    queryFn: () => api.get('/notifications/unread-count').then((r) => r.data),
    refetchInterval: 30_000,
  });

  const { data: recent } = useQuery<NotificationItem[]>({
    queryKey: ['notifications', 'recent'],
    queryFn: () =>
      api.get('/notifications?page=1&pageSize=5').then((r) => r.data.items ?? []),
    enabled: open,
  });

  // Listen for real-time notifications via SignalR
  useSignalR();

  // Use browser event for cross-component toast notifications
  const handleMarkAsRead = async (id: string) => {
    await api.put(`/notifications/${id}/read`);
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
  };

  const handleMarkAllRead = async () => {
    await api.put('/notifications/read-all');
    queryClient.invalidateQueries({ queryKey: ['notifications'] });
  };

  const formatTime = (dateStr: string) => {
    const d = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMin = Math.floor(diffMs / 60000);
    if (diffMin < 1) return 'Just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    const diffHrs = Math.floor(diffMin / 60);
    if (diffHrs < 24) return `${diffHrs}h ago`;
    return d.toLocaleDateString();
  };

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="relative">
          <Bell className="h-5 w-5" />
          {unreadCount > 0 && (
            <span className="absolute -top-0.5 -right-0.5 flex h-5 w-5 items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-destructive-foreground">
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-80">
        <DropdownMenuLabel className="flex items-center justify-between">
          <span>Notifications</span>
          {unreadCount > 0 && (
            <button
              onClick={handleMarkAllRead}
              className="text-xs text-primary hover:underline"
            >
              Mark all read
            </button>
          )}
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        {recent && recent.length === 0 && (
          <div className="px-2 py-6 text-center text-sm text-muted-foreground">
            No notifications yet
          </div>
        )}
        {recent?.map((n) => (
          <DropdownMenuItem
            key={n.id}
            className={cn(
              'flex flex-col items-start gap-0.5 cursor-pointer',
              !n.isRead && 'bg-primary/5',
            )}
            onClick={() => {
              handleMarkAsRead(n.id);
              if (n.referenceType === 'LeaveRequest' && n.referenceId) {
                navigate(`/leave-requests`);
              }
            }}
          >
            <div className="flex items-center gap-2 w-full">
              <span className="text-sm">
                {typeIcon[n.type] || '🔔'}
              </span>
              <span className={cn('text-sm font-medium flex-1', !n.isRead && 'text-foreground')}>
                {n.title}
              </span>
              <span className="text-xs text-muted-foreground whitespace-nowrap">
                {formatTime(n.createdAt)}
              </span>
            </div>
            <span className="text-xs text-muted-foreground pl-6 line-clamp-1">
              {n.body}
            </span>
          </DropdownMenuItem>
        ))}
        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="justify-center text-sm text-primary font-medium cursor-pointer"
          onClick={() => {
            setOpen(false);
            navigate('/notifications');
          }}
        >
          View all notifications
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
