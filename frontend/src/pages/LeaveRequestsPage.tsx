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
import { Plus, RefreshCw, Send, XCircle, FileText } from 'lucide-react';

interface LeaveType {
  id: string;
  name: string;
  code: string;
  daysPerYear: number;
}

interface LeaveRequestResponse {
  id: string;
  employeeId: string;
  employeeName: string;
  employeeNo: string;
  leaveTypeId: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: string;
  attachmentUrl: string;
  rejectionReason: string;
  createdAt: string;
  approvals: { id: string; approverName: string; approverRole: string; status: string; note: string; approvedAt: string }[];
}

interface LeaveRequestListResponse {
  items: LeaveRequestResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const requestSchema = z.object({
  leaveTypeId: z.string().min(1, 'Leave type required'),
  startDate: z.string().min(1, 'Start date required'),
  endDate: z.string().min(1, 'End date required'),
  reason: z.string().min(5, 'Reason is required'),
  attachmentUrl: z.string().optional(),
});

type RequestForm = z.infer<typeof requestSchema>;

const statusBadge = (status: string) => {
  const map: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-800',
    Submitted: 'bg-blue-100 text-blue-800',
    WaitingForManager: 'bg-yellow-100 text-yellow-800',
    WaitingForHR: 'bg-purple-100 text-purple-800',
    Approved: 'bg-green-100 text-green-800',
    Rejected: 'bg-red-100 text-red-800',
    Cancelled: 'bg-orange-100 text-orange-800',
  };
  return <Badge className={map[status] || ''}>{status}</Badge>;
};

export function LeaveRequestsPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [showApply, setShowApply] = useState(false);
  const [detail, setDetail] = useState<LeaveRequestResponse | null>(null);

  const { data, isLoading } = useQuery<LeaveRequestListResponse>({
    queryKey: ['leaveRequests', page],
    queryFn: () => api.get(`/leave-requests?page=${page}&pageSize=10`).then((r) => r.data),
  });

  const { data: leaveTypes } = useQuery<LeaveType[]>({
    queryKey: ['leaveTypes'],
    queryFn: () => api.get('/leave-types').then((r) => r.data),
  });

  const submitMutation = useMutation({
    mutationFn: (id: string) => api.post(`/leave-requests/${id}/submit`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['leaveRequests'] }),
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => api.post(`/leave-requests/${id}/cancel`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['leaveRequests'] }),
  });

  const { register, handleSubmit, reset, setValue, formState: { errors, isSubmitting } } = useForm<RequestForm>({
    resolver: zodResolver(requestSchema),
  });

  const onApply = async (data: RequestForm) => {
    const result = await api.post('/leave-requests', data);
    await api.post(`/leave-requests/${result.data.id}/submit`);
    setShowApply(false);
    reset();
    queryClient.invalidateQueries({ queryKey: ['leaveRequests'] });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Leave Requests</h1>
          <p className="text-muted-foreground">Apply and track your leave</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => queryClient.invalidateQueries({ queryKey: ['leaveRequests'] })}>
            <RefreshCw className="mr-2 h-4 w-4" /> Refresh
          </Button>
          <Button onClick={() => setShowApply(true)}>
            <Plus className="mr-2 h-4 w-4" /> Apply Leave
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      {leaveTypes && (
        <div className="grid gap-4 md:grid-cols-3">
          {leaveTypes.filter(lt => lt.daysPerYear > 0).slice(0, 3).map((lt) => (
            <Card key={lt.id}>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">{lt.name}</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{lt.daysPerYear}</div>
                <p className="text-xs text-muted-foreground">days per year</p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Leave History */}
      <Card>
        <CardHeader>
          <CardTitle>Leave History</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex justify-center py-12"><Spinner /></div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Type</TableHead>
                  <TableHead>Dates</TableHead>
                  <TableHead>Days</TableHead>
                  <TableHead>Reason</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.items.map((lr) => (
                  <TableRow key={lr.id}>
                    <TableCell className="font-medium">{lr.leaveTypeName}</TableCell>
                    <TableCell>{lr.startDate} → {lr.endDate}</TableCell>
                    <TableCell>{lr.totalDays}</TableCell>
                    <TableCell className="max-w-[200px] truncate">{lr.reason}</TableCell>
                    <TableCell>{statusBadge(lr.status)}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" onClick={() => setDetail(lr)} title="View detail">
                          <FileText className="h-4 w-4" />
                        </Button>
                        {lr.status === 'Draft' && (
                          <Button variant="ghost" size="icon" onClick={() => submitMutation.mutate(lr.id)} title="Submit">
                            <Send className="h-4 w-4 text-blue-600" />
                          </Button>
                        )}
                        {['Draft', 'Submitted', 'WaitingForManager', 'WaitingForHR'].includes(lr.status) && (
                          <Button variant="ghost" size="icon" onClick={() => {
                            if (confirm('Cancel this leave request?')) cancelMutation.mutate(lr.id);
                          }} title="Cancel">
                            <XCircle className="h-4 w-4 text-destructive" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.items.length === 0 && (
                  <TableRow><TableCell colSpan={6} className="text-center py-8 text-muted-foreground">No leave requests yet</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Apply Dialog */}
      <Dialog open={showApply} onOpenChange={setShowApply}>
        <DialogContent className="max-w-md">
          <DialogHeader><DialogTitle>Apply for Leave</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onApply)} className="space-y-4">
            <div>
              <Label>Leave Type</Label>
              <select {...register('leaveTypeId')} className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                <option value="">-- Select --</option>
                {leaveTypes?.map((lt) => <option key={lt.id} value={lt.id}>{lt.name} ({lt.code})</option>)}
              </select>
              {errors.leaveTypeId && <p className="text-sm text-destructive mt-1">{errors.leaveTypeId.message}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>Start Date</Label>
                <Input {...register('startDate')} type="date" />
                {errors.startDate && <p className="text-sm text-destructive mt-1">{errors.startDate.message}</p>}
              </div>
              <div>
                <Label>End Date</Label>
                <Input {...register('endDate')} type="date" />
                {errors.endDate && <p className="text-sm text-destructive mt-1">{errors.endDate.message}</p>}
              </div>
            </div>
            <div>
              <Label>Reason</Label>
              <textarea {...register('reason')} className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm" placeholder="Describe your reason..." />
              {errors.reason && <p className="text-sm text-destructive mt-1">{errors.reason.message}</p>}
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setShowApply(false)}>Cancel</Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? <Spinner className="mr-2" /> : null} Submit
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Detail Dialog */}
      <Dialog open={!!detail} onOpenChange={() => setDetail(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader><DialogTitle>Leave Request Detail</DialogTitle></DialogHeader>
          {detail && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-2 text-sm">
                <div className="text-muted-foreground">Type:</div><div className="font-medium">{detail.leaveTypeName}</div>
                <div className="text-muted-foreground">Dates:</div><div>{detail.startDate} → {detail.endDate}</div>
                <div className="text-muted-foreground">Days:</div><div>{detail.totalDays}</div>
                <div className="text-muted-foreground">Status:</div><div>{statusBadge(detail.status)}</div>
                <div className="text-muted-foreground">Reason:</div><div>{detail.reason}</div>
                {detail.rejectionReason && (
                  <>
                    <div className="text-muted-foreground">Rejection:</div><div className="text-red-600">{detail.rejectionReason}</div>
                  </>
                )}
              </div>
              {detail.approvals.length > 0 && (
                <div>
                  <Label className="text-sm font-medium mb-2 block">Approvals</Label>
                  {detail.approvals.map((a) => (
                    <div key={a.id} className="flex items-center justify-between py-1 text-sm border-b last:border-0">
                      <span>{a.approverName} ({a.approverRole})</span>
                      <Badge variant="outline">{a.status}</Badge>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
