import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Plus, RefreshCw, CheckCircle2, XCircle, Pencil } from 'lucide-react';

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

interface EmployeeOption {
  id: string;
  fullName: string;
}

interface MeetingOption {
  id: string;
  title: string;
}

const actionItemSchema = z.object({
  title: z.string().min(3, 'Min 3 characters'),
  description: z.string().optional(),
  assignedToId: z.string().min(1, 'Please select an assignee'),
  dueDate: z.string().min(1, 'Due date is required'),
  priority: z.string().min(1, 'Please select priority'),
  meetingId: z.string().optional(),
});

type ActionItemForm = z.infer<typeof actionItemSchema>;

const priorityColors: Record<string, string> = {
  Low: 'bg-muted text-muted-foreground',
  Medium: 'bg-info/10 text-info',
  High: 'bg-warning/10 text-warning',
  Urgent: 'bg-destructive/10 text-destructive',
};

const statusColors: Record<string, string> = {
  Open: 'bg-warning/10 text-warning',
  InProgress: 'bg-info/10 text-info',
  Completed: 'bg-success/10 text-success',
  Cancelled: 'bg-muted text-muted-foreground',
};

export function ActionItemsPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [editingItem, setEditingItem] = useState<ActionItemResponse | null>(null);

  const { data, isLoading } = useQuery<{ items: ActionItemResponse[]; totalCount: number }>({
    queryKey: ['action-items', statusFilter],
    queryFn: () =>
      api.get('/action-items', { params: { pageSize: 50, status: statusFilter || undefined } }).then((r) => r.data),
  });

  const { data: employees } = useQuery<{ items: EmployeeOption[] }>({
    queryKey: ['employees-dropdown'],
    queryFn: () => api.get('/employees', { params: { pageSize: 200 } }).then((r) => r.data),
  });

  const { data: meetingsData } = useQuery<{ items: MeetingOption[] }>({
    queryKey: ['meetings-dropdown'],
    queryFn: () => api.get('/meetings', { params: { pageSize: 100, status: 'Scheduled' } }).then((r) => r.data),
  });

  const { register, handleSubmit, reset, setValue, formState: { isSubmitting, errors } } = useForm<ActionItemForm>({
    resolver: zodResolver(actionItemSchema),
  });

  const createMutation = useMutation({
    mutationFn: (data: ActionItemForm) => api.post('/action-items', {
      ...data,
      meetingId: data.meetingId || null,
    }),
    onSuccess: () => { setShowCreate(false); reset(); queryClient.invalidateQueries({ queryKey: ['action-items'] }); },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: ActionItemForm }) => api.put(`/action-items/${id}`, {
      ...data,
      meetingId: data.meetingId || null,
    }),
    onSuccess: () => { setEditingItem(null); reset(); queryClient.invalidateQueries({ queryKey: ['action-items'] }); },
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => api.post(`/action-items/${id}/complete`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['action-items'] }),
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => api.post(`/action-items/${id}/cancel`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['action-items'] }),
  });

  const openCreate = () => {
    reset({ title: '', description: '', assignedToId: '', dueDate: '', priority: 'Medium', meetingId: '' });
    setShowCreate(true);
  };

  const openEdit = (item: ActionItemResponse) => {
    reset({
      title: item.title,
      description: item.description || '',
      assignedToId: item.assignedToId,
      dueDate: item.dueDate.split('T')[0],
      priority: item.priority,
      meetingId: item.meetingId || '',
    });
    setEditingItem(item);
  };

  const onCreate = (formData: ActionItemForm) => createMutation.mutate(formData);
  const onUpdate = (formData: ActionItemForm) => {
    if (!editingItem) return;
    updateMutation.mutate({ id: editingItem.id, data: formData });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Action Items</h1>
          <p className="text-muted-foreground">Track your tasks and follow-ups</p>
        </div>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" /> New Action Item
        </Button>
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
          <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['action-items'] })} aria-label="Refresh action items">
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
                  <TableHead className="w-28">Actions</TableHead>
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
                      <span className={new Date(a.dueDate) < new Date() && a.status !== 'Completed' && a.status !== 'Cancelled' ? 'text-destructive font-medium' : ''}>
                        {new Date(a.dueDate).toLocaleDateString()}
                      </span>
                    </TableCell>
                    <TableCell><Badge className={priorityColors[a.priority] || ''}>{a.priority}</Badge></TableCell>
                    <TableCell><Badge className={statusColors[a.status] || ''}>{a.status}</Badge></TableCell>
                    <TableCell>
                      <div className="flex gap-0.5">
                        {(a.status === 'Open' || a.status === 'InProgress') && (
                          <>
                            <Button variant="ghost" size="icon" onClick={() => openEdit(a)} title="Edit" aria-label="Edit action item">
                              <Pencil className="h-4 w-4" />
                            </Button>
                            <Button variant="ghost" size="icon" onClick={() => completeMutation.mutate(a.id)} title="Mark complete" aria-label="Mark complete">
                              <CheckCircle2 className="h-4 w-4 text-success" />
                            </Button>
                            <Button variant="ghost" size="icon" onClick={() => { if (confirm('Cancel this action item?')) cancelMutation.mutate(a.id); }} title="Cancel" aria-label="Cancel action item">
                              <XCircle className="h-4 w-4 text-destructive" />
                            </Button>
                          </>
                        )}
                      </div>
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

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>New Action Item</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onCreate)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="title">Title *</Label><Input id="title" {...register('title')} placeholder="Follow up on Q3 report" aria-invalid={!!errors.title} aria-describedby={errors.title ? 'title-error' : undefined} />{errors.title && <p id="title-error" role="alert" className="text-sm text-destructive mt-1">{errors.title.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="description">Description</Label><Textarea id="description" {...register('description')} placeholder="Details..." rows={3} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="assignedToId">Assigned To *</Label>
                <Select onValueChange={(v: string) => setValue('assignedToId', v)}>
                  <SelectTrigger id="assignedToId" aria-invalid={!!errors.assignedToId} aria-describedby={errors.assignedToId ? 'assignedToId-error' : undefined}><SelectValue placeholder="Select employee" /></SelectTrigger>
                  <SelectContent>
                    {employees?.items?.map((e) => (
                      <SelectItem key={e.id} value={e.id}>{e.fullName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.assignedToId && <p id="assignedToId-error" role="alert" className="text-sm text-destructive mt-1">{errors.assignedToId.message}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="priority">Priority *</Label>
                <Select onValueChange={(v: string) => setValue('priority', v)} defaultValue="Medium">
                  <SelectTrigger id="priority"><SelectValue placeholder="Select" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Medium">Medium</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Urgent">Urgent</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="dueDate">Due Date *</Label><Input id="dueDate" type="date" {...register('dueDate')} aria-invalid={!!errors.dueDate} aria-describedby={errors.dueDate ? 'dueDate-error' : undefined} />{errors.dueDate && <p id="dueDate-error" role="alert" className="text-sm text-destructive mt-1">{errors.dueDate.message}</p>}</div>
              <div className="space-y-2">
                <Label htmlFor="meetingId">Meeting (optional)</Label>
                <Select onValueChange={(v: string) => setValue('meetingId', v)}>
                  <SelectTrigger id="meetingId"><SelectValue placeholder="None" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">None</SelectItem>
                    {meetingsData?.items?.map((m) => (
                      <SelectItem key={m.id} value={m.id}>{m.title}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Create Action Item</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editingItem} onOpenChange={() => setEditingItem(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Edit Action Item</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="title-edit">Title *</Label><Input id="title-edit" {...register('title')} aria-invalid={!!errors.title} aria-describedby={errors.title ? 'title-edit-error' : undefined} />{errors.title && <p id="title-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.title.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="description-edit">Description</Label><Textarea id="description-edit" {...register('description')} rows={3} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="assignedToId-edit">Assigned To *</Label>
                <Select value={editingItem?.assignedToId} onValueChange={(v: string) => setValue('assignedToId', v)}>
                  <SelectTrigger id="assignedToId-edit" aria-invalid={!!errors.assignedToId} aria-describedby={errors.assignedToId ? 'assignedToId-edit-error' : undefined}><SelectValue placeholder="Select employee" /></SelectTrigger>
                  <SelectContent>
                    {employees?.items?.map((e) => (
                      <SelectItem key={e.id} value={e.id}>{e.fullName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.assignedToId && <p id="assignedToId-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.assignedToId.message}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="priority-edit">Priority *</Label>
                <Select onValueChange={(v: string) => setValue('priority', v)} defaultValue={editingItem?.priority}>
                  <SelectTrigger id="priority-edit"><SelectValue placeholder="Select" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Medium">Medium</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Urgent">Urgent</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="dueDate-edit">Due Date *</Label><Input id="dueDate-edit" type="date" {...register('dueDate')} aria-invalid={!!errors.dueDate} aria-describedby={errors.dueDate ? 'dueDate-edit-error' : undefined} />{errors.dueDate && <p id="dueDate-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.dueDate.message}</p>}</div>
              <div className="space-y-2">
                <Label htmlFor="meetingId-edit">Meeting (optional)</Label>
                <Select value={editingItem?.meetingId || 'none'} onValueChange={(v: string) => setValue('meetingId', v)}>
                  <SelectTrigger id="meetingId-edit"><SelectValue placeholder="None" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">None</SelectItem>
                    {meetingsData?.items?.map((m) => (
                      <SelectItem key={m.id} value={m.id}>{m.title}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Save Changes</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
