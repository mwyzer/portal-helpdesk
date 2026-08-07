import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Plus, Trash2, Edit } from 'lucide-react';
import { useToastStore } from '@/lib/useToast';

interface AgentAssignment {
  id: string;
  userId: string;
  userName: string;
  departmentId: string;
  departmentName: string;
  isActive: boolean;
  maxTickets: number;
  currentLoad: number;
}

interface UserItem {
  id: string;
  fullName: string;
}

interface DepartmentItem {
  id: string;
  name: string;
}

export function AgentAssignmentsPage() {
  const queryClient = useQueryClient();
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({ userId: '', departmentId: '', maxTickets: 10, isActive: true });

  const { data: assignments, isLoading } = useQuery({
    queryKey: ['agent-assignments'],
    queryFn: () => api.get('/agent-assignments').then(r => r.data),
  });

  const { data: users } = useQuery({
    queryKey: ['users'],
    queryFn: () => api.get('/users', { params: { pageSize: 999 } }).then(r => r.data),
  });

  const { data: departments } = useQuery({
    queryKey: ['departments'],
    queryFn: () => api.get('/departments').then(r => r.data),
  });

  const addToast = useToastStore((s) => s.addToast);
  const onMutationError = (err: unknown) => {
    const resp = (err as { response?: { data?: { message?: string; error?: string } } })?.response?.data;
    addToast({ title: 'Action failed', message: resp?.message ?? resp?.error ?? 'Something went wrong. Please try again.', type: 'error' });
  };

  const saveMutation = useMutation({
    mutationFn: (body: any) => api.post('/agent-assignments', body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agent-assignments'] });
      setForm({ userId: '', departmentId: '', maxTickets: 10, isActive: true });
    },
    onError: onMutationError,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, ...body }: any) => api.put(`/agent-assignments/${id}`, body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['agent-assignments'] }),
    onError: onMutationError,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/agent-assignments/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['agent-assignments'] }),
    onError: onMutationError,
  });

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Agent Assignments</h1>
        <Dialog>
          <DialogTrigger asChild>
            <Button><Plus className="mr-2 h-4 w-4" /> Assign Agent</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>New Agent Assignment</DialogTitle></DialogHeader>
            <div className="space-y-4 mt-4">
              <div><Label>User</Label>
                <Select value={form.userId} onValueChange={v => setForm(p => ({ ...p, userId: v }))}>
                  <SelectTrigger><SelectValue placeholder="Select user..." /></SelectTrigger>
                  <SelectContent>
                    {(users as any)?.items?.map((u: UserItem) => <SelectItem key={u.id} value={u.id}>{u.fullName}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div><Label>Department</Label>
                <Select value={form.departmentId} onValueChange={v => setForm(p => ({ ...p, departmentId: v }))}>
                  <SelectTrigger><SelectValue placeholder="Select department..." /></SelectTrigger>
                  <SelectContent>
                    {(departments as DepartmentItem[])?.map(d => <SelectItem key={d.id} value={d.id}>{d.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div><Label>Max Tickets</Label><Input type="number" value={form.maxTickets} onChange={e => setForm(p => ({ ...p, maxTickets: Number(e.target.value) }))} /></div>
              <Button onClick={() => saveMutation.mutate(form)} disabled={saveMutation.isPending}>
                {saveMutation.isPending ? <Spinner /> : 'Save'}
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <div className="space-y-3">
        {(assignments as AgentAssignment[])?.map(a => (
          <Card key={a.id}>
            <CardContent className="flex items-center justify-between py-4">
              <div className="flex-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium">{a.userName}</span>
                  <Badge variant="outline">{a.departmentName}</Badge>
                  {a.isActive ? <Badge className="bg-green-100 text-green-800">Active</Badge> : <Badge variant="outline">Inactive</Badge>}
                </div>
                <div className="text-sm text-muted-foreground mt-1">
                  Load: {a.currentLoad} / {a.maxTickets} tickets
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2 mt-2">
                  <div
                    className="bg-primary h-2 rounded-full transition-all"
                    style={{ width: `${Math.min(100, (a.currentLoad / a.maxTickets) * 100)}%` }}
                  />
                </div>
              </div>
              <div className="flex gap-1 ml-4">
                <Button variant="ghost" size="icon" onClick={() => updateMutation.mutate({ id: a.id, maxTickets: a.maxTickets, isActive: !a.isActive })}>
                  <Edit className="h-4 w-4" />
                </Button>
                <Button variant="ghost" size="icon" onClick={() => { if (confirm('Remove assignment?')) deleteMutation.mutate(a.id); }}>
                  <Trash2 className="h-4 w-4 text-destructive" />
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
        {(!assignments || assignments.length === 0) && (
          <div className="text-center py-12 text-muted-foreground">No agent assignments</div>
        )}
      </div>
    </div>
  );
}
