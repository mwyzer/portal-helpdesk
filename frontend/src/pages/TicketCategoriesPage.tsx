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
import { Plus, Edit, Trash2 } from 'lucide-react';

interface CategoryItem {
  id: string;
  name: string;
  description: string;
  defaultPriority: string;
  slaHours: number;
  departmentName?: string;
}

interface DepartmentItem {
  id: string;
  name: string;
}

export function TicketCategoriesPage() {
  const queryClient = useQueryClient();
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState({ name: '', description: '', defaultPriority: 'Normal', slaHours: 24, departmentId: '' });

  const { data: categories, isLoading } = useQuery({
    queryKey: ['ticket-categories'],
    queryFn: () => api.get('/ticket-categories').then(r => r.data),
  });

  const { data: departments } = useQuery({
    queryKey: ['departments'],
    queryFn: () => api.get('/departments').then(r => r.data),
  });

  const saveMutation = useMutation({
    mutationFn: (body: any) =>
      editId ? api.put(`/ticket-categories/${editId}`, body) : api.post('/ticket-categories', body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ticket-categories'] });
      setEditId(null);
      setForm({ name: '', description: '', defaultPriority: 'Normal', slaHours: 24, departmentId: '' });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/ticket-categories/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ticket-categories'] }),
  });

  const openEdit = (c: CategoryItem) => {
    setEditId(c.id);
    setForm({ name: c.name, description: c.description, defaultPriority: c.defaultPriority, slaHours: c.slaHours, departmentId: '' });
  };

  if (isLoading) return <Spinner />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Ticket Categories</h1>
        <Dialog>
          <DialogTrigger asChild>
            <Button onClick={() => { setEditId(null); setForm({ name: '', description: '', defaultPriority: 'Normal', slaHours: 24, departmentId: '' }); }}>
              <Plus className="mr-2 h-4 w-4" /> Add Category
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>{editId ? 'Edit' : 'Add'} Category</DialogTitle></DialogHeader>
            <div className="space-y-4 mt-4">
              <div><Label>Name</Label><Input value={form.name} onChange={e => setForm(p => ({ ...p, name: e.target.value }))} /></div>
              <div><Label>Description</Label><Input value={form.description} onChange={e => setForm(p => ({ ...p, description: e.target.value }))} /></div>
              <div><Label>Default Priority</Label>
                <Select value={form.defaultPriority} onValueChange={v => setForm(p => ({ ...p, defaultPriority: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Normal">Normal</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Urgent">Urgent</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div><Label>SLA Hours</Label><Input type="number" value={form.slaHours} onChange={e => setForm(p => ({ ...p, slaHours: Number(e.target.value) }))} /></div>
              <div><Label>Department (optional)</Label>
                <Select value={form.departmentId} onValueChange={v => setForm(p => ({ ...p, departmentId: v }))}>
                  <SelectTrigger><SelectValue placeholder="All Departments" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Departments</SelectItem>
                    {(departments as DepartmentItem[])?.map(d => <SelectItem key={d.id} value={d.id}>{d.name}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <Button onClick={() => saveMutation.mutate(form)} disabled={saveMutation.isPending}>
                {saveMutation.isPending ? <Spinner /> : 'Save'}
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {(categories as CategoryItem[])?.map(c => (
          <Card key={c.id}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-lg">{c.name}</CardTitle>
              <div className="flex gap-1">
                <Button variant="ghost" size="icon" onClick={() => openEdit(c)}><Edit className="h-4 w-4" /></Button>
                <Button variant="ghost" size="icon" onClick={() => { if (confirm('Delete this category?')) deleteMutation.mutate(c.id); }}>
                  <Trash2 className="h-4 w-4 text-destructive" />
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground mb-2">{c.description}</p>
              <div className="flex gap-2">
                <Badge variant="outline">{c.defaultPriority}</Badge>
                <Badge variant="outline">{c.slaHours}h SLA</Badge>
                {c.departmentName && <Badge variant="outline">{c.departmentName}</Badge>}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
