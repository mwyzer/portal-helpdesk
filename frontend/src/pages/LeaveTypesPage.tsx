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
import { Card, CardContent } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Plus, RefreshCw, Pencil, Trash2 } from 'lucide-react';

interface LeaveTypeResponse {
  id: string;
  name: string;
  code: string;
  daysPerYear: number;
  isPaid: boolean;
  isActive: boolean;
  minServiceMonths: number;
  requiresAttachment: boolean;
  skipManagerApproval: boolean;
  createdAt: string;
}

const leaveTypeSchema = z.object({
  name: z.string().min(2),
  code: z.string().min(1).max(10),
  daysPerYear: z.coerce.number().min(0),
  isPaid: z.boolean(),
  isActive: z.boolean(),
  minServiceMonths: z.coerce.number().min(0),
  requiresAttachment: z.boolean(),
  skipManagerApproval: z.boolean(),
});

type LeaveTypeForm = z.infer<typeof leaveTypeSchema>;

export function LeaveTypesPage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<LeaveTypeResponse | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading } = useQuery<LeaveTypeResponse[]>({
    queryKey: ['leaveTypes'],
    queryFn: () => api.get('/leave-types').then((r) => r.data),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/leave-types/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['leaveTypes'] }),
  });

  const { register, handleSubmit, reset, setValue, formState: { errors, isSubmitting } } = useForm<LeaveTypeForm>({
    resolver: zodResolver(leaveTypeSchema),
    defaultValues: { isPaid: true, isActive: true, daysPerYear: 12, minServiceMonths: 0, requiresAttachment: false, skipManagerApproval: false },
  });

  const openEdit = (lt: LeaveTypeResponse) => {
    setEditing(lt);
    setValue('name', lt.name);
    setValue('code', lt.code);
    setValue('daysPerYear', lt.daysPerYear);
    setValue('isPaid', lt.isPaid);
    setValue('isActive', lt.isActive);
    setValue('minServiceMonths', lt.minServiceMonths);
    setValue('requiresAttachment', lt.requiresAttachment);
    setValue('skipManagerApproval', lt.skipManagerApproval);
  };

  const onCreate = async (data: LeaveTypeForm) => {
    await api.post('/leave-types', data);
    setShowCreate(false);
    reset();
    queryClient.invalidateQueries({ queryKey: ['leaveTypes'] });
  };

  const onUpdate = async (data: LeaveTypeForm) => {
    if (!editing) return;
    await api.put(`/leave-types/${editing.id}`, data);
    setEditing(null);
    reset();
    queryClient.invalidateQueries({ queryKey: ['leaveTypes'] });
  };

  const FormFields = () => (
    <>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label>Name</Label>
          <Input {...register('name')} placeholder="Annual Leave" />
          {errors.name && <p className="text-sm text-destructive mt-1">{errors.name.message}</p>}
        </div>
        <div>
          <Label>Code</Label>
          <Input {...register('code')} placeholder="AL" />
          {errors.code && <p className="text-sm text-destructive mt-1">{errors.code.message}</p>}
        </div>
      </div>
      <div className="grid grid-cols-3 gap-4">
        <div>
          <Label>Days / Year</Label>
          <Input {...register('daysPerYear')} type="number" min="0" />
          {errors.daysPerYear && <p className="text-sm text-destructive mt-1">{errors.daysPerYear.message}</p>}
        </div>
        <div>
          <Label>Min Service (months)</Label>
          <Input {...register('minServiceMonths')} type="number" min="0" />
          {errors.minServiceMonths && <p className="text-sm text-destructive mt-1">{errors.minServiceMonths.message}</p>}
        </div>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="flex items-center gap-2">
          <input type="checkbox" id="isPaid" {...register('isPaid')} className="h-4 w-4" />
          <Label htmlFor="isPaid">Paid Leave</Label>
        </div>
        <div className="flex items-center gap-2">
          <input type="checkbox" id="isActive" {...register('isActive')} className="h-4 w-4" />
          <Label htmlFor="isActive">Active</Label>
        </div>
        <div className="flex items-center gap-2">
          <input type="checkbox" id="requiresAttachment" {...register('requiresAttachment')} className="h-4 w-4" />
          <Label htmlFor="requiresAttachment">Requires Attachment</Label>
        </div>
        <div className="flex items-center gap-2">
          <input type="checkbox" id="skipManagerApproval" {...register('skipManagerApproval')} className="h-4 w-4" />
          <Label htmlFor="skipManagerApproval">Skip Manager Approval</Label>
        </div>
      </div>
    </>
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Leave Types</h1>
          <p className="text-muted-foreground">Configure leave categories</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => queryClient.invalidateQueries({ queryKey: ['leaveTypes'] })}>
            <RefreshCw className="mr-2 h-4 w-4" /> Refresh
          </Button>
          <Button onClick={() => setShowCreate(true)}>
            <Plus className="mr-2 h-4 w-4" /> Add Type
          </Button>
        </div>
      </div>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex justify-center py-12"><Spinner /></div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Code</TableHead>
                  <TableHead>Days/Year</TableHead>
                  <TableHead>Paid</TableHead>
                  <TableHead>Active</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.map((lt) => (
                  <TableRow key={lt.id}>
                    <TableCell className="font-medium">{lt.name}</TableCell>
                    <TableCell className="font-mono">{lt.code}</TableCell>
                    <TableCell>{lt.daysPerYear}</TableCell>
                    <TableCell>{lt.isPaid ? <Badge variant="outline">Paid</Badge> : <Badge variant="secondary">Unpaid</Badge>}</TableCell>
                    <TableCell>{lt.isActive ? <Badge className="bg-green-100 text-green-800">Active</Badge> : <Badge className="bg-gray-100 text-gray-800">Inactive</Badge>}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" onClick={() => openEdit(lt)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => {
                          if (confirm('Delete this leave type?')) deleteMutation.mutate(lt.id);
                        }}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.length === 0 && (
                  <TableRow><TableCell colSpan={6} className="text-center py-8 text-muted-foreground">No leave types found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="max-w-md">
          <DialogHeader><DialogTitle>Add Leave Type</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onCreate)} className="space-y-4">
            <FormFields />
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setShowCreate(false)}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? <Spinner className="mr-2" /> : null} Create
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editing} onOpenChange={() => setEditing(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader><DialogTitle>Edit Leave Type</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <FormFields />
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setEditing(null)}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? <Spinner className="mr-2" /> : null} Save
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
