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
import { Plus, Search, RefreshCw, Pencil, Trash2, Power, PowerOff } from 'lucide-react';

interface UserResponse {
  id: string;
  email: string;
  fullName: string;
  nik: string;
  departmentName: string;
  positionName: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
}

interface PagedResponse {
  items: UserResponse[];
  total: number;
  page: number;
  pageSize: number;
}

const userSchema = z.object({
  email: z.string().email(),
  fullName: z.string().min(2),
  nik: z.string().optional(),
  password: z.string().min(8, 'Min 8 characters').optional(),
  departmentId: z.string().optional(),
  positionId: z.string().optional(),
  roleIds: z.array(z.string()).optional(),
});

type UserForm = z.infer<typeof userSchema>;

export function UsersPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [editingUser, setEditingUser] = useState<UserResponse | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading } = useQuery<PagedResponse>({
    queryKey: ['users', page, search],
    queryFn: () => api.get(`/users?page=${page}&pageSize=10&search=${search}`).then((r) => r.data),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/users/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, activate }: { id: string; activate: boolean }) =>
      activate ? api.post(`/users/${id}/activate`) : api.post(`/users/${id}/deactivate`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['users'] }),
  });

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<UserForm>({
    resolver: zodResolver(userSchema),
  });

  const onCreate = async (data: UserForm) => {
    await api.post('/users', { ...data, roleIds: data.roleIds ?? [] });
    setShowCreate(false);
    reset();
    queryClient.invalidateQueries({ queryKey: ['users'] });
  };

  const onUpdate = async (data: UserForm) => {
    if (!editingUser) return;
    await api.put(`/users/${editingUser.id}`, data);
    setEditingUser(null);
    reset();
    queryClient.invalidateQueries({ queryKey: ['users'] });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Users</h1>
          <p className="text-muted-foreground">Manage system users</p>
        </div>
        <Button onClick={() => setShowCreate(true)}>
          <Plus className="mr-2 h-4 w-4" /> Add User
        </Button>
      </div>

      <Card>
        <CardHeader className="pb-0">
          <div className="flex items-center gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search users..."
                className="pl-9"
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPage(1); }}
              />
            </div>
            <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['users'] })}>
              <RefreshCw className="h-4 w-4" />
            </Button>
          </div>
        </CardHeader>
        <CardContent className="pt-4">
          {isLoading ? (
            <div className="flex justify-center py-8"><Spinner className="h-8 w-8" /></div>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>NIK</TableHead>
                    <TableHead>Department</TableHead>
                    <TableHead>Roles</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="w-24">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data?.items.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium">{user.fullName}</TableCell>
                      <TableCell>{user.email}</TableCell>
                      <TableCell>{user.nik || '—'}</TableCell>
                      <TableCell>{user.departmentName || '—'}</TableCell>
                      <TableCell>
                        <div className="flex gap-1 flex-wrap">
                          {user.roles.map((r) => <Badge key={r} variant="secondary" className="text-xs">{r}</Badge>)}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={user.isActive ? 'success' : 'secondary'}>
                          {user.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button variant="ghost" size="icon" onClick={() => setEditingUser(user)} aria-label="Edit user">
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => toggleMutation.mutate({ id: user.id, activate: !user.isActive })}
                            aria-label={user.isActive ? 'Deactivate user' : 'Activate user'}
                          >
                            {user.isActive ? <PowerOff className="h-4 w-4" /> : <Power className="h-4 w-4" />}
                          </Button>
                          <Button variant="ghost" size="icon" onClick={() => deleteMutation.mutate(user.id)} aria-label="Delete user">
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                  {data?.items.length === 0 && (
                    <TableRow><TableCell colSpan={7} className="text-center py-8 text-muted-foreground">No users found</TableCell></TableRow>
                  )}
                </TableBody>
              </Table>
              {data && data.total > 10 && (
                <div className="flex items-center justify-between pt-4">
                  <p className="text-sm text-muted-foreground">Showing {(page - 1) * 10 + 1}-{Math.min(page * 10, data.total)} of {data.total}</p>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</Button>
                    <Button variant="outline" size="sm" disabled={page * 10 >= data.total} onClick={() => setPage(page + 1)}>Next</Button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent>
          <DialogHeader><DialogTitle>Create User</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onCreate)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="fullName">Full Name</Label><Input id="fullName" {...register('fullName')} aria-invalid={!!errors.fullName} aria-describedby={errors.fullName ? 'fullName-error' : undefined} />{errors.fullName && <p id="fullName-error" role="alert" className="text-sm text-destructive mt-1">{errors.fullName.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="email">Email</Label><Input id="email" type="email" {...register('email')} aria-invalid={!!errors.email} aria-describedby={errors.email ? 'email-error' : undefined} />{errors.email && <p id="email-error" role="alert" className="text-sm text-destructive mt-1">{errors.email.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="nik">NIK</Label><Input id="nik" {...register('nik')} /></div>
            <div className="space-y-2"><Label htmlFor="password">Password</Label><Input id="password" type="password" {...register('password')} aria-invalid={!!errors.password} aria-describedby={errors.password ? 'password-error' : undefined} />{errors.password && <p id="password-error" role="alert" className="text-sm text-destructive mt-1">{errors.password.message}</p>}</div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Create</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editingUser} onOpenChange={() => setEditingUser(null)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Edit User</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="fullName-edit">Full Name</Label><Input id="fullName-edit" {...register('fullName')} defaultValue={editingUser?.fullName} aria-invalid={!!errors.fullName} aria-describedby={errors.fullName ? 'fullName-edit-error' : undefined} />{errors.fullName && <p id="fullName-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.fullName.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="email-edit">Email</Label><Input id="email-edit" type="email" {...register('email')} defaultValue={editingUser?.email} aria-invalid={!!errors.email} aria-describedby={errors.email ? 'email-edit-error' : undefined} />{errors.email && <p id="email-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.email.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="nik-edit">NIK</Label><Input id="nik-edit" {...register('nik')} defaultValue={editingUser?.nik} /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Save</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
