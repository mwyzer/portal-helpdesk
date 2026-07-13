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
import { Plus, RefreshCw, FileText, Download, Wand2, Send, CheckCircle2, XCircle, FileCheck } from 'lucide-react';

interface DocumentRequestResponse {
  id: string;
  employeeId: string;
  employeeName: string;
  templateId: string;
  templateName: string;
  title: string;
  contentDraft: string | null;
  contentFinal: string | null;
  status: string;
  letterNumber: string | null;
  notes: string | null;
  rejectionReason: string | null;
  createdAt: string;
  updatedAt: string;
}

interface DocumentTemplateResponse {
  id: string;
  name: string;
  code: string;
  category: string;
}

const requestSchema = z.object({
  templateId: z.string().min(1, 'Please select a template'),
  title: z.string().min(3, 'Min 3 characters'),
  notes: z.string().optional(),
});

const statusColors: Record<string, string> = {
  Draft: 'bg-muted text-muted-foreground',
  Submitted: 'bg-info/10 text-info',
  AiDraftReady: 'bg-primary/10 text-primary',
  Review: 'bg-warning/10 text-warning',
  Approved: 'bg-success/10 text-success',
  Rejected: 'bg-destructive/10 text-destructive',
  Generated: 'bg-success/10 text-success',
};

export function DocumentRequestsPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [detailRequest, setDetailRequest] = useState<DocumentRequestResponse | null>(null);
  const [rejectDialog, setRejectDialog] = useState<{ id: string } | null>(null);
  const [rejectionReason, setRejectionReason] = useState('');

  const { data: templates } = useQuery<DocumentTemplateResponse[]>({
    queryKey: ['document-templates'],
    queryFn: () => api.get('/document-templates').then((r) => r.data),
  });

  const { data, isLoading } = useQuery<{ items: DocumentRequestResponse[]; totalCount: number }>({
    queryKey: ['document-requests', statusFilter],
    queryFn: () =>
      api.get('/document-requests', { params: { pageSize: 50, status: statusFilter || undefined } }).then((r) => r.data),
  });

  const inv = () => queryClient.invalidateQueries({ queryKey: ['document-requests'] });

  const { register, handleSubmit, reset, setValue, formState: { errors, isSubmitting } } = useForm<z.infer<typeof requestSchema>>({
    resolver: zodResolver(requestSchema),
  });

  const createMutation = useMutation({
    mutationFn: (data: z.infer<typeof requestSchema>) => api.post('/document-requests', data),
    onSuccess: () => { setShowCreate(false); reset(); inv(); },
  });

  const generateDraftMutation = useMutation({
    mutationFn: (id: string) => api.post(`/document-requests/${id}/generate-draft`),
    onSuccess: () => inv(),
  });

  const submitForReviewMutation = useMutation({
    mutationFn: (id: string) => api.post(`/document-requests/${id}/submit-for-review`),
    onSuccess: () => inv(),
  });

  const approveMutation = useMutation({
    mutationFn: (id: string) => api.post(`/document-requests/${id}/approve`),
    onSuccess: () => inv(),
  });

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      api.post(`/document-requests/${id}/reject`, { reason }),
    onSuccess: () => { setRejectDialog(null); setRejectionReason(''); inv(); },
  });

  const generateFinalMutation = useMutation({
    mutationFn: (id: string) => api.post(`/document-requests/${id}/generate-final`),
    onSuccess: () => inv(),
  });

  const downloadMutation = useMutation({
    mutationFn: (id: string) =>
      api.get(`/document-requests/${id}/download`, { responseType: 'blob' }).then((r) => {
        const url = window.URL.createObjectURL(new Blob([r.data]));
        const a = document.createElement('a');
        a.href = url;
        a.download = `document-${id}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
      }),
  });

  const workflowButtons = (r: DocumentRequestResponse) => {
    const btns: JSX.Element[] = [];
    if (r.status === 'Draft' || r.status === 'Submitted') {
      btns.push(
        <Button key="draft" variant="outline" size="sm" onClick={() => generateDraftMutation.mutate(r.id)} title="Generate AI Draft">
          <Wand2 className="mr-1 h-3.5 w-3.5" /> Draft
        </Button>
      );
    }
    if (r.status === 'AiDraftReady') {
      btns.push(
        <Button key="review" variant="outline" size="sm" onClick={() => submitForReviewMutation.mutate(r.id)} title="Submit for Review">
          <Send className="mr-1 h-3.5 w-3.5" /> Review
        </Button>
      );
    }
    if (r.status === 'Review') {
      btns.push(
        <Button key="approve" variant="outline" size="sm" onClick={() => approveMutation.mutate(r.id)} title="Approve">
          <CheckCircle2 className="mr-1 h-3.5 w-3.5 text-success" /> Approve
        </Button>,
        <Button key="reject" variant="outline" size="sm" onClick={() => setRejectDialog({ id: r.id })} title="Reject">
          <XCircle className="mr-1 h-3.5 w-3.5 text-destructive" /> Reject
        </Button>
      );
    }
    if (r.status === 'Approved') {
      btns.push(
        <Button key="generate" variant="outline" size="sm" onClick={() => generateFinalMutation.mutate(r.id)} title="Generate Final Document">
          <FileCheck className="mr-1 h-3.5 w-3.5" /> Generate
        </Button>
      );
    }
    if (r.status === 'Generated') {
      btns.push(
        <Button key="download" variant="outline" size="sm" onClick={() => downloadMutation.mutate(r.id)} title="Download">
          <Download className="mr-1 h-3.5 w-3.5" /> Download
        </Button>
      );
    }
    return btns;
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Document Requests</h1>
          <p className="text-muted-foreground">Request and track official documents</p>
        </div>
        <Button onClick={() => { reset(); setShowCreate(true); }}>
          <Plus className="mr-2 h-4 w-4" /> New Request
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {['', 'Draft', 'Submitted', 'AiDraftReady', 'Review', 'Approved', 'Rejected', 'Generated'].map((s) => (
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
          <CardTitle>All Requests</CardTitle>
          <Button variant="outline" size="icon" onClick={() => inv()}>
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
                  <TableHead>Title</TableHead>
                  <TableHead>Template</TableHead>
                  <TableHead>Employee</TableHead>
                  <TableHead>Letter No.</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-64">Workflow</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.items?.map((r) => (
                  <TableRow key={r.id} className="cursor-pointer hover:bg-muted/50" onClick={() => setDetailRequest(r)}>
                    <TableCell className="font-medium">{r.title}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{r.templateName}</TableCell>
                    <TableCell className="text-sm">{r.employeeName}</TableCell>
                    <TableCell className="text-sm font-mono">{r.letterNumber || '—'}</TableCell>
                    <TableCell><Badge className={statusColors[r.status] || ''}>{r.status}</Badge></TableCell>
                    <TableCell onClick={(e) => e.stopPropagation()}>
                      <div className="flex flex-wrap gap-1">{workflowButtons(r)}</div>
                    </TableCell>
                  </TableRow>
                ))}
                {(!data?.items || data.items.length === 0) && (
                  <TableRow><TableCell colSpan={6} className="text-center py-8 text-muted-foreground">No document requests found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent>
          <DialogHeader><DialogTitle>New Document Request</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit((data) => createMutation.mutate(data))} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="templateId">Template *</Label>
              <Select onValueChange={(v: string) => setValue('templateId', v)}>
                <SelectTrigger id="templateId" aria-invalid={!!errors.templateId} aria-describedby={errors.templateId ? 'templateId-error' : undefined}><SelectValue placeholder="Select a template" /></SelectTrigger>
                <SelectContent>
                  {templates?.map((t) => (
                    <SelectItem key={t.id} value={t.id}>{t.name} ({t.code})</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.templateId && <p id="templateId-error" role="alert" className="text-sm text-destructive mt-1">{errors.templateId.message}</p>}
            </div>
            <div className="space-y-2"><Label htmlFor="title">Title *</Label><Input id="title" {...register('title')} placeholder="e.g., Employment Certificate for John" aria-invalid={!!errors.title} aria-describedby={errors.title ? 'title-error' : undefined} />{errors.title && <p id="title-error" role="alert" className="text-sm text-destructive mt-1">{errors.title.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="notes">Notes</Label><Input id="notes" {...register('notes')} placeholder="Additional information..." /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Submit Request</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Detail Dialog */}
      <Dialog open={!!detailRequest} onOpenChange={() => setDetailRequest(null)}>
        <DialogContent className="sm:max-w-2xl max-h-[80vh] overflow-y-auto">
          {detailRequest && (
            <>
              <DialogHeader>
                <DialogTitle>{detailRequest.title}</DialogTitle>
                <p className="text-sm text-muted-foreground">
                  Template: {detailRequest.templateName} &middot; Employee: {detailRequest.employeeName} &middot; Status: <Badge className={statusColors[detailRequest.status] || ''}>{detailRequest.status}</Badge>
                  {detailRequest.letterNumber && <span className="ml-2 font-mono">#{detailRequest.letterNumber}</span>}
                </p>
              </DialogHeader>
              <div className="space-y-4">
                {detailRequest.notes && (
                  <div>
                    <Label className="text-xs text-muted-foreground">Notes</Label>
                    <p className="text-sm">{detailRequest.notes}</p>
                  </div>
                )}
                {detailRequest.contentDraft && (
                  <div>
                    <Label className="text-xs text-muted-foreground">AI Draft</Label>
                    <div className="mt-1 rounded-md border bg-muted/50 p-4 text-sm whitespace-pre-wrap max-h-60 overflow-y-auto">
                      {detailRequest.contentDraft}
                    </div>
                  </div>
                )}
                {detailRequest.contentFinal && (
                  <div>
                    <Label className="text-xs text-muted-foreground">Final Content</Label>
                    <div className="mt-1 rounded-md border bg-muted/50 p-4 text-sm whitespace-pre-wrap max-h-60 overflow-y-auto">
                      {detailRequest.contentFinal}
                    </div>
                  </div>
                )}
                {detailRequest.rejectionReason && (
                  <div>
                    <Label className="text-xs text-muted-foreground">Rejection Reason</Label>
                    <p className="text-sm text-destructive">{detailRequest.rejectionReason}</p>
                  </div>
                )}
                <div className="flex flex-wrap gap-2 pt-2">{workflowButtons(detailRequest)}</div>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>

      {/* Reject Dialog */}
      <Dialog open={!!rejectDialog} onOpenChange={() => setRejectDialog(null)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Reject Document Request</DialogTitle></DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Reason for rejection</Label>
              <Input value={rejectionReason} onChange={(e) => setRejectionReason(e.target.value)} placeholder="e.g., Incorrect template data" />
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setRejectDialog(null)}>Cancel</Button>
              <Button variant="destructive" onClick={() => rejectDialog && rejectMutation.mutate({ id: rejectDialog.id, reason: rejectionReason })} disabled={!rejectionReason.trim()}>
                Confirm Reject
              </Button>
            </DialogFooter>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
