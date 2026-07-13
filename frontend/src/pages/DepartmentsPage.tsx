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
import { Plus, RefreshCw, Pencil } from 'lucide-react';

interface DepartmentResponse {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  positionCount: number;
}

const deptSchema = z.object({
  name: z.string().min(2),
  code: z.string().min(2, 'Min 2 characters').max(10),
});

export function DepartmentsPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [editingDept, setEditingDept] = useState<DepartmentResponse | null>(null);

  const { data, isLoading } = useQuery<DepartmentResponse[]>({
    queryKey: ['departments'],
    queryFn: () => api.get('/departments').then((r) => r.data),
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<z.infer<typeof deptSchema>>({
    resolver: zodResolver(deptSchema),
  });

  const createMutation = useMutation({
    mutationFn: (data: { name: string; code: string }) => api.post('/departments', data),
    onSuccess: () => {
      setShowCreate(false);
      reset();
      queryClient.invalidateQueries({ queryKey: ['departments'] });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { name: string; code: string; isActive: boolean } }) =>
      api.put(`/departments/${id}`, data),
    onSuccess: () => {
      setEditingDept(null);
      reset();
      queryClient.invalidateQueries({ queryKey: ['departments'] });
    },
  });

  const onCreate = (data: z.infer<typeof deptSchema>) => createMutation.mutate(data);
  const onUpdate = (data: z.infer<typeof deptSchema>) => {
    if (!editingDept) return;
    updateMutation.mutate({ id: editingDept.id, data: { ...data, isActive: editingDept.isActive } });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Departments</h1>
          <p className="text-muted-foreground">Manage organizational departments</p>
        </div>
        <Button onClick={() => setShowCreate(true)}>
          <Plus className="mr-2 h-4 w-4" /> Add Department
        </Button>
      </div>

      <Card>
        <CardHeader className="pb-0 flex-row items-center justify-between">
          <CardTitle>All Departments</CardTitle>
          <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['departments'] })}>
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
                  <TableHead>Code</TableHead>
                  <TableHead>Positions</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-24">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.map((dept) => (
                  <TableRow key={dept.id}>
                    <TableCell className="font-medium">{dept.name}</TableCell>
                    <TableCell><Badge variant="outline">{dept.code}</Badge></TableCell>
                    <TableCell>{dept.positionCount}</TableCell>
                    <TableCell>
                      <Badge variant={dept.isActive ? 'success' : 'secondary'}>
                        {dept.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Button variant="ghost" size="icon" onClick={() => setEditingDept(dept)} aria-label="Edit department">
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.length === 0 && (
                  <TableRow><TableCell colSpan={5} className="text-center py-8 text-muted-foreground">No departments found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent>
          <DialogHeader><DialogTitle>Create Department</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onCreate)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="name">Name</Label><Input id="name" {...register('name')} placeholder="Human Resources" aria-invalid={!!errors.name} aria-describedby={errors.name ? 'name-error' : undefined} />{errors.name && <p id="name-error" role="alert" className="text-sm text-destructive mt-1">{errors.name.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="code">Code</Label><Input id="code" {...register('code')} placeholder="HR" maxLength={10} aria-invalid={!!errors.code} aria-describedby={errors.code ? 'code-error' : undefined} />{errors.code && <p id="code-error" role="alert" className="text-sm text-destructive mt-1">{errors.code.message}</p>}</div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Create</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={!!editingDept} onOpenChange={() => setEditingDept(null)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Edit Department</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="name-edit">Name</Label><Input id="name-edit" {...register('name')} defaultValue={editingDept?.name} aria-invalid={!!errors.name} aria-describedby={errors.name ? 'name-edit-error' : undefined} />{errors.name && <p id="name-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.name.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="code-edit">Code</Label><Input id="code-edit" {...register('code')} defaultValue={editingDept?.code} maxLength={10} aria-invalid={!!errors.code} aria-describedby={errors.code ? 'code-edit-error' : undefined} />{errors.code && <p id="code-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.code.message}</p>}</div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Save</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
