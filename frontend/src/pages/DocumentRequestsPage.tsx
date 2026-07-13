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
import { Plus, RefreshCw, FileText, Download } from 'lucide-react';

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
  Draft: 'bg-gray-100 text-gray-700',
  Submitted: 'bg-blue-100 text-blue-700',
  AiDraftReady: 'bg-purple-100 text-purple-700',
  Review: 'bg-yellow-100 text-yellow-700',
  Approved: 'bg-green-100 text-green-700',
  Rejected: 'bg-red-100 text-red-700',
  Generated: 'bg-emerald-100 text-emerald-700',
};

export function DocumentRequestsPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');

  const { data: templates } = useQuery<DocumentTemplateResponse[]>({
    queryKey: ['document-templates'],
    queryFn: () => api.get('/document-templates').then((r) => r.data),
  });

  const { data, isLoading } = useQuery<{ items: DocumentRequestResponse[]; totalCount: number }>({
    queryKey: ['document-requests', statusFilter],
    queryFn: () =>
      api.get('/document-requests', { params: { pageSize: 50, status: statusFilter || undefined } }).then((r) => r.data),
  });

  const { register, handleSubmit, reset, setValue, formState: { isSubmitting } } = useForm<z.infer<typeof requestSchema>>({
    resolver: zodResolver(requestSchema),
  });

  const createMutation = useMutation({
    mutationFn: (data: z.infer<typeof requestSchema>) => api.post('/document-requests', data),
    onSuccess: () => { setShowCreate(false); reset(); queryClient.invalidateQueries({ queryKey: ['document-requests'] }); },
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
          <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['document-requests'] })}>
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
                  <TableHead className="w-24">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.items?.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell className="font-medium">{r.title}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">{r.templateName}</TableCell>
                    <TableCell className="text-sm">{r.employeeName}</TableCell>
                    <TableCell className="text-sm font-mono">{r.letterNumber || '—'}</TableCell>
                    <TableCell><Badge className={statusColors[r.status] || ''}>{r.status}</Badge></TableCell>
                    <TableCell>
                      {r.status === 'Generated' && (
                        <Button variant="ghost" size="icon" onClick={() => downloadMutation.mutate(r.id)} title="Download">
                          <Download className="h-4 w-4" />
                        </Button>
                      )}
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
              <Label>Template *</Label>
              <Select onValueChange={(v: string) => setValue('templateId', v)}>
                <SelectTrigger><SelectValue placeholder="Select a template" /></SelectTrigger>
                <SelectContent>
                  {templates?.map((t) => (
                    <SelectItem key={t.id} value={t.id}>{t.name} ({t.code})</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2"><Label>Title *</Label><Input {...register('title')} placeholder="e.g., Employment Certificate for John" /></div>
            <div className="space-y-2"><Label>Notes</Label><Input {...register('notes')} placeholder="Additional information..." /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Submit Request</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
