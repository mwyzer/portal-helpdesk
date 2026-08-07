import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import api from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Plus, Ticket, Clock, AlertTriangle, CheckCircle, XCircle, RefreshCw, Download } from 'lucide-react';
import { useToastStore } from '@/lib/useToast';

interface TicketItem {
  id: string;
  title: string;
  categoryName: string;
  priority: string;
  status: string;
  assignedAgentName?: string;
  submittedByName: string;
  slaDeadline?: string;
  slaStatus: string;
  createdAt: string;
}

interface TicketStats {
  total: number;
  open: number;
  inProgress: number;
  resolved: number;
  closed: number;
  breached: number;
  averageResolutionHours: number;
}

interface TicketCategory {
  id: string;
  name: string;
}

const STATUS_COLORS: Record<string, string> = {
  Open: 'bg-blue-100 text-blue-800',
  Assigned: 'bg-purple-100 text-purple-800',
  InProgress: 'bg-yellow-100 text-yellow-800',
  Resolved: 'bg-green-100 text-green-800',
  Closed: 'bg-gray-100 text-gray-800',
  Reopened: 'bg-orange-100 text-orange-800',
  Rejected: 'bg-red-100 text-red-800',
};

const PRIORITY_COLORS: Record<string, string> = {
  Low: 'bg-gray-100 text-gray-600',
  Normal: 'bg-blue-100 text-blue-700',
  High: 'bg-orange-100 text-orange-800',
  Urgent: 'bg-red-100 text-red-800',
};

const SLA_COLORS: Record<string, string> = {
  OnTrack: 'bg-green-100 text-green-700',
  AtRisk: 'bg-yellow-100 text-yellow-800',
  Breached: 'bg-red-100 text-red-800',
};

export function TicketsPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [createOpen, setCreateOpen] = useState(false);

  // GET /tickets is scoped server-side to "my tickets" (SubmittedById == current user) --
  // Agent/Manager/Super Admin need /tickets/queue instead to see the organization-wide queue,
  // same role check already used for export/stats elsewhere on this page.
  const canSeeAllTickets = user?.roles?.some(r => ['Agent', 'Manager', 'Super Admin'].includes(r));

  const { data: ticketsData, isLoading } = useQuery({
    queryKey: ['tickets', statusFilter, priorityFilter, canSeeAllTickets],
    queryFn: () =>
      api
        .get(canSeeAllTickets ? '/tickets/queue' : '/tickets', {
          params: { status: statusFilter || undefined, priority: priorityFilter || undefined },
        })
        .then(r => r.data),
  });

  const { data: stats } = useQuery({
    queryKey: ['ticket-stats'],
    queryFn: () => api.get('/tickets/stats').then(r => r.data),
    enabled: user?.roles?.some(r => ['Agent','Manager','Super Admin'].includes(r)),
  });

  const { data: categories } = useQuery({
    queryKey: ['ticket-categories'],
    queryFn: () => api.get('/ticket-categories').then(r => r.data),
    enabled: createOpen,
  });

  const addToast = useToastStore((s) => s.addToast);
  const onMutationError = (err: unknown) => {
    const resp = (err as { response?: { data?: { message?: string; error?: string } } })?.response?.data;
    addToast({ title: 'Action failed', message: resp?.message ?? resp?.error ?? 'Something went wrong. Please try again.', type: 'error' });
  };

  const createMutation = useMutation({
    mutationFn: (body: { categoryId: string; title: string; description: string; priority?: string }) =>
      api.post('/tickets', body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tickets'] });
      setCreateOpen(false);
      newTicket.title = '';
      newTicket.description = '';
    },
    onError: onMutationError,
  });

  const [newTicket, setNewTicket] = useState({ title: '', description: '', categoryId: '', priority: '' });

  const handleCreate = () => {
    if (!newTicket.title || !newTicket.categoryId) return;
    createMutation.mutate(newTicket);
  };

  const handleExport = async () => {
    const response = await api.get('/tickets/export', {
      params: { status: statusFilter || undefined, priority: priorityFilter || undefined },
      responseType: 'blob',
    });
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.download = `tickets-export-${new Date().toISOString().split('T')[0]}.xlsx`;
    link.click();
    window.URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Tickets</h1>
        <div className="flex items-center gap-2">
        {canSeeAllTickets && (
          <Button variant="outline" onClick={handleExport}>
            <Download className="mr-2 h-4 w-4" /> Export
          </Button>
        )}
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="mr-2 h-4 w-4" /> New Ticket</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>Create Ticket</DialogTitle></DialogHeader>
            <div className="space-y-4 mt-4">
              <div>
                <Label>Title</Label>
                <Input value={newTicket.title} onChange={e => setNewTicket(p => ({ ...p, title: e.target.value }))} placeholder="Brief description of the issue" />
              </div>
              <div>
                <Label>Description</Label>
                <Input value={newTicket.description} onChange={e => setNewTicket(p => ({ ...p, description: e.target.value }))} placeholder="Detailed description" />
              </div>
              <div>
                <Label>Category</Label>
                <Select value={newTicket.categoryId} onValueChange={v => setNewTicket(p => ({ ...p, categoryId: v }))}>
                  <SelectTrigger><SelectValue placeholder="Select category..." /></SelectTrigger>
                  <SelectContent>
                    {(categories as TicketCategory[])?.map(c => (
                      <SelectItem key={c.id} value={c.id}>{c.name}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Priority</Label>
                <Select value={newTicket.priority} onValueChange={v => setNewTicket(p => ({ ...p, priority: v }))}>
                  <SelectTrigger><SelectValue placeholder="Default" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Normal">Normal</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Urgent">Urgent</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <Button onClick={handleCreate} disabled={createMutation.isPending}>
                {createMutation.isPending ? <Spinner /> : 'Submit Ticket'}
              </Button>
            </div>
          </DialogContent>
        </Dialog>
        </div>
      </div>

      {/* Stats */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-6 gap-3">
          <Card><CardContent className="pt-4 text-center"><div className="text-2xl font-bold">{(stats as TicketStats).total}</div><div className="text-xs text-muted-foreground">Total</div></CardContent></Card>
          <Card><CardContent className="pt-4 text-center"><div className="text-2xl font-bold text-blue-600">{(stats as TicketStats).open}</div><div className="text-xs text-muted-foreground">Open</div></CardContent></Card>
          <Card><CardContent className="pt-4 text-center"><div className="text-2xl font-bold text-yellow-600">{(stats as TicketStats).inProgress}</div><div className="text-xs text-muted-foreground">In Progress</div></CardContent></Card>
          <Card><CardContent className="pt-4 text-center"><div className="text-2xl font-bold text-green-600">{(stats as TicketStats).resolved}</div><div className="text-xs text-muted-foreground">Resolved</div></CardContent></Card>
          <Card><CardContent className="pt-4 text-center"><div className="text-2xl font-bold text-gray-600">{(stats as TicketStats).closed}</div><div className="text-xs text-muted-foreground">Closed</div></CardContent></Card>
          <Card><CardContent className="pt-4 text-center"><div className="text-2xl font-bold text-red-600">{(stats as TicketStats).breached}</div><div className="text-xs text-muted-foreground">Breached</div></CardContent></Card>
        </div>
      )}

      {/* Filters */}
      <div className="flex gap-3">
        <Select value={statusFilter || undefined} onValueChange={v => setStatusFilter(v)}>
          <SelectTrigger className="w-[180px]"><SelectValue placeholder="All Statuses" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Open">Open</SelectItem>
            <SelectItem value="Assigned">Assigned</SelectItem>
            <SelectItem value="InProgress">In Progress</SelectItem>
            <SelectItem value="Resolved">Resolved</SelectItem>
            <SelectItem value="Closed">Closed</SelectItem>
            <SelectItem value="Reopened">Reopened</SelectItem>
          </SelectContent>
        </Select>
        <Select value={priorityFilter || undefined} onValueChange={v => setPriorityFilter(v)}>
          <SelectTrigger className="w-[180px]"><SelectValue placeholder="All Priorities" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Priorities</SelectItem>
            <SelectItem value="Low">Low</SelectItem>
            <SelectItem value="Normal">Normal</SelectItem>
            <SelectItem value="High">High</SelectItem>
            <SelectItem value="Urgent">Urgent</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Ticket List */}
      {isLoading ? (
        <Spinner />
      ) : (
        <div className="space-y-3">
          {(ticketsData as any)?.items?.map((t: TicketItem) => (
            <Card key={t.id} className="cursor-pointer hover:shadow-md transition-shadow" onClick={() => navigate(`/tickets/${t.id}`)}>
              <CardContent className="flex items-center justify-between py-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <Ticket className="h-4 w-4 text-muted-foreground" />
                    <span className="font-medium">{t.title}</span>
                    <Badge className={PRIORITY_COLORS[t.priority] || ''}>{t.priority}</Badge>
                    <Badge className={STATUS_COLORS[t.status] || ''}>{t.status}</Badge>
                    {t.slaStatus && <Badge className={SLA_COLORS[t.slaStatus] || ''}>{t.slaStatus}</Badge>}
                  </div>
                  <div className="text-sm text-muted-foreground mt-1">
                    {t.categoryName} · Submitted by {t.submittedByName} · {new Date(t.createdAt).toLocaleDateString()}
                  </div>
                </div>
                <div className="text-right text-sm text-muted-foreground">
                  {t.assignedAgentName && <div>Agent: {t.assignedAgentName}</div>}
                  {t.slaDeadline && <div>SLA: {new Date(t.slaDeadline).toLocaleDateString()}</div>}
                </div>
              </CardContent>
            </Card>
          ))}
          {(!ticketsData || (ticketsData as any)?.items?.length === 0) && (
            <div className="text-center py-12 text-muted-foreground">No tickets found</div>
          )}
        </div>
      )}
    </div>
  );
}
