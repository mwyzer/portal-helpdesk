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
import { Plus, Search, RefreshCw, Pencil, Trash2, UserPlus } from 'lucide-react';

interface EmployeeResponse {
  id: string;
  employeeNo: string;
  fullName: string;
  email: string;
  phone: string;
  joinDate: string;
  departmentId: string;
  departmentName: string;
  positionId: string;
  positionName: string;
  managerId: string;
  managerName: string;
  employmentStatus: string;
  workLocation: string;
  createdAt: string;
}

interface EmployeeListResponse {
  items: EmployeeResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface Department {
  id: string;
  name: string;
}

interface Position {
  id: string;
  name: string;
}

const employeeSchema = z.object({
  fullName: z.string().min(2, 'Name is required'),
  email: z.string().email('Valid email required'),
  phone: z.string().optional(),
  employeeNo: z.string().min(1, 'Employee number required'),
  joinDate: z.string().min(1, 'Join date required'),
  departmentId: z.string().optional(),
  positionId: z.string().optional(),
  managerId: z.string().optional(),
  employmentStatus: z.string().min(1),
  workLocation: z.string().optional(),
});

type EmployeeForm = z.infer<typeof employeeSchema>;

const statusBadge = (status: string) => {
  const map: Record<string, string> = {
    Active: 'bg-green-100 text-green-800',
    Inactive: 'bg-gray-100 text-gray-800',
    Resigned: 'bg-red-100 text-red-800',
    Terminated: 'bg-orange-100 text-orange-800',
  };
  return <Badge className={map[status] || ''}>{status}</Badge>;
};

export function EmployeesPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [editingEmployee, setEditingEmployee] = useState<EmployeeResponse | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading } = useQuery<EmployeeListResponse>({
    queryKey: ['employees', page, search],
    queryFn: () => api.get(`/employees?page=${page}&pageSize=10&search=${search}`).then((r) => r.data),
  });

  const { data: departments } = useQuery<Department[]>({
    queryKey: ['departments'],
    queryFn: () => api.get('/departments').then((r) => r.data),
  });

  const { data: positions } = useQuery<Position[]>({
    queryKey: ['positions'],
    queryFn: () => api.get('/positions').then((r) => r.data),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/employees/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['employees'] }),
  });

  const { register, handleSubmit, reset, setValue, formState: { errors, isSubmitting } } = useForm<EmployeeForm>({
    resolver: zodResolver(employeeSchema),
    defaultValues: { employmentStatus: 'Active' },
  });

  const openEdit = (emp: EmployeeResponse) => {
    setEditingEmployee(emp);
    setValue('fullName', emp.fullName);
    setValue('email', emp.email);
    setValue('phone', emp.phone || '');
    setValue('employeeNo', emp.employeeNo);
    setValue('joinDate', emp.joinDate?.split('T')[0] || '');
    setValue('departmentId', emp.departmentId || '');
    setValue('positionId', emp.positionId || '');
    setValue('managerId', emp.managerId || '');
    setValue('employmentStatus', emp.employmentStatus);
    setValue('workLocation', emp.workLocation || '');
  };

  const onCreate = async (formData: EmployeeForm) => {
    await api.post('/employees', formData);
    setShowCreate(false);
    reset();
    queryClient.invalidateQueries({ queryKey: ['employees'] });
  };

  const onUpdate = async (formData: EmployeeForm) => {
    if (!editingEmployee) return;
    await api.put(`/employees/${editingEmployee.id}`, formData);
    setEditingEmployee(null);
    reset();
    queryClient.invalidateQueries({ queryKey: ['employees'] });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Employees</h1>
          <p className="text-muted-foreground">Manage employee records</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => queryClient.invalidateQueries({ queryKey: ['employees'] })}>
            <RefreshCw className="mr-2 h-4 w-4" /> Refresh
          </Button>
          <Button onClick={() => setShowCreate(true)}>
            <UserPlus className="mr-2 h-4 w-4" /> Add Employee
          </Button>
        </div>
      </div>

      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search employees..."
            className="pl-10"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          />
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
                  <TableHead>Employee No</TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Department</TableHead>
                  <TableHead>Position</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-24">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.items.map((emp) => (
                  <TableRow key={emp.id}>
                    <TableCell className="font-mono text-sm">{emp.employeeNo}</TableCell>
                    <TableCell className="font-medium">{emp.fullName}</TableCell>
                    <TableCell>{emp.email}</TableCell>
                    <TableCell>{emp.departmentName || '-'}</TableCell>
                    <TableCell>{emp.positionName || '-'}</TableCell>
                    <TableCell>{statusBadge(emp.employmentStatus)}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" onClick={() => openEdit(emp)}>
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => {
                          if (confirm('Delete this employee?')) deleteMutation.mutate(emp.id);
                        }}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.items.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={7} className="text-center py-8 text-muted-foreground">
                      No employees found
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Add Employee</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onCreate)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Employee No</Label>
                <Input {...register('employeeNo')} placeholder="EMP-001" />
                {errors.employeeNo && <p className="text-sm text-destructive mt-1">{errors.employeeNo.message}</p>}
              </div>
              <div>
                <Label>Full Name</Label>
                <Input {...register('fullName')} placeholder="John Doe" />
                {errors.fullName && <p className="text-sm text-destructive mt-1">{errors.fullName.message}</p>}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Email</Label>
                <Input {...register('email')} type="email" placeholder="john@company.com" />
                {errors.email && <p className="text-sm text-destructive mt-1">{errors.email.message}</p>}
              </div>
              <div>
                <Label>Phone</Label>
                <Input {...register('phone')} placeholder="+62..." />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Join Date</Label>
                <Input {...register('joinDate')} type="date" />
                {errors.joinDate && <p className="text-sm text-destructive mt-1">{errors.joinDate.message}</p>}
              </div>
              <div>
                <Label>Status</Label>
                <select {...register('employmentStatus')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                  <option value="Active">Active</option>
                  <option value="Inactive">Inactive</option>
                  <option value="Resigned">Resigned</option>
                  <option value="Terminated">Terminated</option>
                </select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Department</Label>
                <select {...register('departmentId')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                  <option value="">-- Select --</option>
                  {departments?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </div>
              <div>
                <Label>Position</Label>
                <select {...register('positionId')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                  <option value="">-- Select --</option>
                  {positions?.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
                </select>
              </div>
            </div>
            <div>
              <Label>Work Location</Label>
              <Input {...register('workLocation')} placeholder="Jakarta Office" />
            </div>
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
      <Dialog open={!!editingEmployee} onOpenChange={() => setEditingEmployee(null)}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Edit Employee</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Employee No</Label>
                <Input {...register('employeeNo')} />
                {errors.employeeNo && <p className="text-sm text-destructive mt-1">{errors.employeeNo.message}</p>}
              </div>
              <div>
                <Label>Full Name</Label>
                <Input {...register('fullName')} />
                {errors.fullName && <p className="text-sm text-destructive mt-1">{errors.fullName.message}</p>}
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Email</Label>
                <Input {...register('email')} type="email" />
                {errors.email && <p className="text-sm text-destructive mt-1">{errors.email.message}</p>}
              </div>
              <div>
                <Label>Phone</Label>
                <Input {...register('phone')} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Join Date</Label>
                <Input {...register('joinDate')} type="date" />
                {errors.joinDate && <p className="text-sm text-destructive mt-1">{errors.joinDate.message}</p>}
              </div>
              <div>
                <Label>Status</Label>
                <select {...register('employmentStatus')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                  <option value="Active">Active</option>
                  <option value="Inactive">Inactive</option>
                  <option value="Resigned">Resigned</option>
                  <option value="Terminated">Terminated</option>
                </select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Department</Label>
                <select {...register('departmentId')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                  <option value="">-- Select --</option>
                  {departments?.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </div>
              <div>
                <Label>Position</Label>
                <select {...register('positionId')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                  <option value="">-- Select --</option>
                  {positions?.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
                </select>
              </div>
            </div>
            <div>
              <Label>Work Location</Label>
              <Input {...register('workLocation')} />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setEditingEmployee(null)}>Cancel</Button>
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
