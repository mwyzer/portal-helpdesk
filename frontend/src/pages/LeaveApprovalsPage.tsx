import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { RefreshCw, CheckCircle, XCircle, Eye } from 'lucide-react';

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

export function LeaveApprovalsPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [rejecting, setRejecting] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState('');
  const [detail, setDetail] = useState<LeaveRequestResponse | null>(null);

  const { data, isLoading } = useQuery<LeaveRequestListResponse>({
    queryKey: ['leaveApprovals', page],
    queryFn: () => api.get(`/leave-requests/pending-approval?page=${page}&pageSize=10`).then((r) => r.data),
  });

  const approveMutation = useMutation({
    mutationFn: (id: string) => api.post(`/leave-requests/${id}/approve`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['leaveApprovals'] }),
  });

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      api.post(`/leave-requests/${id}/reject`, { reason }),
    onSuccess: () => {
      setRejecting(null);
      setRejectReason('');
      queryClient.invalidateQueries({ queryKey: ['leaveApprovals'] });
    },
  });

  const onReject = () => {
    if (rejecting && rejectReason.trim()) {
      rejectMutation.mutate({ id: rejecting, reason: rejectReason });
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Leave Approvals</h1>
          <p className="text-muted-foreground">Review and approve leave requests</p>
        </div>
        <Button variant="outline" onClick={() => queryClient.invalidateQueries({ queryKey: ['leaveApprovals'] })}>
          <RefreshCw className="mr-2 h-4 w-4" /> Refresh
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Pending Approvals {data ? `(${data.totalCount})` : ''}</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex justify-center py-12"><Spinner /></div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Employee</TableHead>
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
                    <TableCell>
                      <div className="font-medium">{lr.employeeName}</div>
                      <div className="text-xs text-muted-foreground">{lr.employeeNo}</div>
                    </TableCell>
                    <TableCell>{lr.leaveTypeName}</TableCell>
                    <TableCell className="text-sm">{lr.startDate} → {lr.endDate}</TableCell>
                    <TableCell>{lr.totalDays}</TableCell>
                    <TableCell className="max-w-[200px] truncate">{lr.reason}</TableCell>
                    <TableCell>{statusBadge(lr.status)}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" onClick={() => setDetail(lr)} title="View detail">
                          <Eye className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => {
                            if (confirm('Approve this request?')) approveMutation.mutate(lr.id);
                          }}
                          title="Approve"
                          disabled={approveMutation.isPending}
                        >
                          <CheckCircle className="h-4 w-4 text-green-600" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => setRejecting(lr.id)}
                          title="Reject"
                        >
                          <XCircle className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.items.length === 0 && (
                  <TableRow><TableCell colSpan={7} className="text-center py-8 text-muted-foreground">No pending approvals</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Reject Dialog */}
      <Dialog open={!!rejecting} onOpenChange={() => setRejecting(null)}>
        <DialogContent className="max-w-sm">
          <DialogHeader><DialogTitle>Reject Leave Request</DialogTitle></DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Reason for rejection</Label>
              <textarea
                className="flex min-h-[100px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                placeholder="Provide a reason..."
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
              />
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => { setRejecting(null); setRejectReason(''); }}>Cancel</Button>
              <Button
                variant="destructive"
                onClick={onReject}
                disabled={rejectMutation.isPending || !rejectReason.trim()}
              >
                {rejectMutation.isPending ? <Spinner className="mr-2" /> : null} Reject
              </Button>
            </DialogFooter>
          </div>
        </DialogContent>
      </Dialog>

      {/* Detail Dialog */}
      <Dialog open={!!detail} onOpenChange={() => setDetail(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader><DialogTitle>Leave Request Detail</DialogTitle></DialogHeader>
          {detail && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-2 text-sm">
                <div className="text-muted-foreground">Employee:</div><div className="font-medium">{detail.employeeName} ({detail.employeeNo})</div>
                <div className="text-muted-foreground">Type:</div><div className="font-medium">{detail.leaveTypeName}</div>
                <div className="text-muted-foreground">Dates:</div><div>{detail.startDate} → {detail.endDate}</div>
                <div className="text-muted-foreground">Days:</div><div>{detail.totalDays}</div>
                <div className="text-muted-foreground">Status:</div><div>{statusBadge(detail.status)}</div>
                <div className="text-muted-foreground">Reason:</div><div>{detail.reason}</div>
              </div>
              {detail.approvals.length > 0 && (
                <div>
                  <Label className="text-sm font-medium mb-2 block">Approval History</Label>
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
