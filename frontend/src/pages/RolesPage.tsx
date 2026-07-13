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
import { Plus, RefreshCw, Pencil, Trash2 } from 'lucide-react';

interface RoleResponse {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  userCount: number;
}

const roleSchema = z.object({
  name: z.string().min(2),
  description: z.string().optional(),
});

export function RolesPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleResponse | null>(null);

  const { data, isLoading } = useQuery<RoleResponse[]>({
    queryKey: ['roles'],
    queryFn: () => api.get('/roles').then((r) => r.data),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/roles/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['roles'] }),
  });

  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm<z.infer<typeof roleSchema>>({
    resolver: zodResolver(roleSchema),
  });

  const onCreate = async (data: z.infer<typeof roleSchema>) => {
    await api.post('/roles', data);
    setShowCreate(false);
    reset();
    queryClient.invalidateQueries({ queryKey: ['roles'] });
  };

  const onUpdate = async (data: z.infer<typeof roleSchema>) => {
    if (!editingRole) return;
    await api.put(`/roles/${editingRole.id}`, { ...data, isActive: editingRole.isActive });
    setEditingRole(null);
    reset();
    queryClient.invalidateQueries({ queryKey: ['roles'] });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Roles</h1>
          <p className="text-muted-foreground">Manage roles and permissions</p>
        </div>
        <Button onClick={() => setShowCreate(true)}>
          <Plus className="mr-2 h-4 w-4" /> Add Role
        </Button>
      </div>

      <Card>
        <CardHeader className="pb-0 flex-row items-center justify-between">
          <CardTitle>All Roles</CardTitle>
          <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['roles'] })}>
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
                  <TableHead>Name</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>Users</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-24">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.map((role) => (
                  <TableRow key={role.id}>
                    <TableCell className="font-medium">{role.name}</TableCell>
                    <TableCell>{role.description || '—'}</TableCell>
                    <TableCell>{role.userCount}</TableCell>
                    <TableCell>
                      <Badge variant={role.isActive ? 'success' : 'secondary'}>
                        {role.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" onClick={() => setEditingRole(role)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => deleteMutation.mutate(role.id)}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.length === 0 && (
                  <TableRow><TableCell colSpan={5} className="text-center py-8 text-muted-foreground">No roles found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent>
          <DialogHeader><DialogTitle>Create Role</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onCreate)} className="space-y-4">
            <div className="space-y-2"><Label>Name</Label><Input {...register('name')} placeholder="Super Admin" /></div>
            <div className="space-y-2"><Label>Description</Label><Input {...register('description')} /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Create</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={!!editingRole} onOpenChange={() => setEditingRole(null)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Edit Role</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <div className="space-y-2"><Label>Name</Label><Input {...register('name')} defaultValue={editingRole?.name} /></div>
            <div className="space-y-2"><Label>Description</Label><Input {...register('description')} defaultValue={editingRole?.description} /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Save</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
