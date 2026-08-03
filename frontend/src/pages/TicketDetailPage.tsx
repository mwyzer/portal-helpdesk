import { useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ArrowLeft, Send, CheckCircle, XCircle, RefreshCw, AlertTriangle, Upload, Paperclip } from 'lucide-react';

interface TicketDetail {
  id: string;
  title: string;
  description: string;
  categoryName: string;
  subCategory?: string;
  priority: string;
  status: string;
  assignedToName: string;
  assignedAgentName?: string;
  submittedByName: string;
  departmentName?: string;
  slaDeadline?: string;
  slaStatus: string;
  resolvedAt?: string;
  closedAt?: string;
  comments: TicketComment[];
  attachments: TicketAttachment[];
  history: TicketHistory[];
  createdAt: string;
  updatedAt: string;
}

interface TicketComment {
  id: string;
  authorName: string;
  content: string;
  isInternal: boolean;
  createdAt: string;
}

interface TicketAttachment {
  id: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  uploadedByName: string;
  createdAt: string;
}

interface TicketHistory {
  id: string;
  field: string;
  oldValue?: string;
  newValue?: string;
  changedByName: string;
  createdAt: string;
}

const STATUS_COLORS: Record<string, string> = {
  Open: 'bg-blue-100 text-blue-800', Assigned: 'bg-purple-100 text-purple-800',
  InProgress: 'bg-yellow-100 text-yellow-800', Resolved: 'bg-green-100 text-green-800',
  Closed: 'bg-gray-100 text-gray-800', Reopened: 'bg-orange-100 text-orange-800',
  Rejected: 'bg-red-100 text-red-800',
};

const PRIORITY_COLORS: Record<string, string> = {
  Low: 'bg-gray-100 text-gray-600', Normal: 'bg-blue-100 text-blue-700',
  High: 'bg-orange-100 text-orange-800', Urgent: 'bg-red-100 text-red-800',
};

export function TicketDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const [comment, setComment] = useState('');
  const [isInternal, setIsInternal] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data: ticket, isLoading } = useQuery({
    queryKey: ['ticket', id],
    queryFn: () => api.get(`/tickets/${id}`).then(r => r.data),
    enabled: !!id,
  });

  const commentMutation = useMutation({
    mutationFn: (body: { content: string; isInternal: boolean }) =>
      api.post(`/tickets/${id}/comment`, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ticket', id] });
      setComment('');
      setIsInternal(false);
    },
  });

  const statusMutation = useMutation({
    mutationFn: ({ action }: { action: string }) => api.post(`/tickets/${id}/${action}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ticket', id] }),
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      return api.post(`/tickets/${id}/upload`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ticket', id] });
      if (fileInputRef.current) fileInputRef.current.value = '';
    },
  });

  if (isLoading) return <Spinner />;
  if (!ticket) return <div className="text-center py-12 text-muted-foreground">Ticket not found</div>;

  const t = ticket as TicketDetail;
  const isAgent = user?.roles?.some(r => ['Agent','Manager','Super Admin'].includes(r));

  return (
    <div className="space-y-6">
      <Button variant="ghost" onClick={() => navigate('/tickets')}>
        <ArrowLeft className="mr-2 h-4 w-4" /> Back to Tickets
      </Button>

      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold">{t.title}</h1>
          <div className="flex items-center gap-2 mt-2">
            <Badge className={STATUS_COLORS[t.status]}>{t.status}</Badge>
            <Badge className={PRIORITY_COLORS[t.priority]}>{t.priority}</Badge>
            <span className="text-sm text-muted-foreground">{t.categoryName}{t.subCategory ? ` / ${t.subCategory}` : ''}</span>
          </div>
        </div>
        {isAgent && (
          <div className="flex gap-2">
            {t.status === 'Open' || t.status === 'Reopened' ? (
              <Button size="sm" onClick={() => statusMutation.mutate({ action: 'accept' })}>
                <CheckCircle className="mr-1 h-4 w-4" /> Accept
              </Button>
            ) : null}
            {t.status === 'Assigned' || t.status === 'InProgress' ? (
              <Button size="sm" variant="outline" onClick={() => statusMutation.mutate({ action: 'resolve' })}>
                <CheckCircle className="mr-1 h-4 w-4" /> Resolve
              </Button>
            ) : null}
            {(t.status === 'Resolved') && t.submittedByName === user?.fullName ? (
              <Button size="sm" variant="outline" onClick={() => statusMutation.mutate({ action: 'close' })}>
                <XCircle className="mr-1 h-4 w-4" /> Close
              </Button>
            ) : null}
            {t.status === 'Resolved' ? (
              <Button size="sm" variant="ghost" onClick={() => statusMutation.mutate({ action: 'reopen' })}>
                <RefreshCw className="mr-1 h-4 w-4" /> Reopen
              </Button>
            ) : null}
          </div>
        )}
      </div>

      {/* Meta */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card><CardContent className="pt-4"><div className="text-xs text-muted-foreground">Submitted by</div><div className="font-medium">{t.submittedByName}</div></CardContent></Card>
        <Card><CardContent className="pt-4"><div className="text-xs text-muted-foreground">Assigned to</div><div className="font-medium">{t.assignedAgentName || t.assignedToName || 'Unassigned'}</div></CardContent></Card>
        <Card><CardContent className="pt-4"><div className="text-xs text-muted-foreground">SLA Deadline</div><div className="font-medium">{t.slaDeadline ? new Date(t.slaDeadline).toLocaleString() : 'N/A'}</div></CardContent></Card>
        <Card><CardContent className="pt-4"><div className="text-xs text-muted-foreground">Created</div><div className="font-medium">{new Date(t.createdAt).toLocaleString()}</div></CardContent></Card>
      </div>

      <Tabs defaultValue="comments">
        <TabsList>
          <TabsTrigger value="comments">Comments ({t.comments.length})</TabsTrigger>
          <TabsTrigger value="details">Description</TabsTrigger>
          <TabsTrigger value="history">History ({t.history.length})</TabsTrigger>
          <TabsTrigger value="attachments">Attachments ({t.attachments.length})</TabsTrigger>
        </TabsList>

        <TabsContent value="comments" className="space-y-4 mt-4">
          {t.comments.map(c => (
            <Card key={c.id} className={c.isInternal ? 'border-l-4 border-l-yellow-400' : ''}>
              <CardContent className="py-3">
                <div className="flex items-center gap-2 mb-1">
                  <span className="font-medium text-sm">{c.authorName}</span>
                  {c.isInternal && <Badge variant="outline" className="text-xs">Internal</Badge>}
                  <span className="text-xs text-muted-foreground">{new Date(c.createdAt).toLocaleString()}</span>
                </div>
                <p className="text-sm whitespace-pre-wrap">{c.content}</p>
              </CardContent>
            </Card>
          ))}
          <Card>
            <CardContent className="py-3 space-y-3">
              <Label>Add Comment</Label>
              <Input value={comment} onChange={e => setComment(e.target.value)} placeholder="Type your comment..." />
              <div className="flex items-center justify-between">
                <Label>
                  <input type="checkbox" checked={isInternal} onChange={e => setIsInternal(e.target.checked)} className="mr-1" />
                  Internal note
                </Label>
                <Button size="sm" onClick={() => commentMutation.mutate({ content: comment, isInternal })} disabled={!comment.trim() || commentMutation.isPending}>
                  <Send className="mr-1 h-4 w-4" /> Send
                </Button>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="details" className="mt-4">
          <Card><CardContent className="py-4 whitespace-pre-wrap text-sm">{t.description || 'No description provided.'}</CardContent></Card>
        </TabsContent>

        <TabsContent value="history" className="mt-4 space-y-2">
          {t.history.map(h => (
            <Card key={h.id}><CardContent className="py-3 flex items-center justify-between text-sm">
              <div><span className="font-medium">{h.field}</span>: {h.oldValue || '—'} → {h.newValue || '—'}</div>
              <div className="text-xs text-muted-foreground">{h.changedByName} · {new Date(h.createdAt).toLocaleString()}</div>
            </CardContent></Card>
          ))}
        </TabsContent>

        <TabsContent value="attachments" className="mt-4 space-y-2">
          {t.attachments.map(a => (
            <Card key={a.id}><CardContent className="py-3 flex items-center justify-between text-sm">
              <div><span className="font-medium">{a.fileName}</span> ({(a.fileSize / 1024).toFixed(1)} KB)</div>
              <div className="text-xs text-muted-foreground">{a.uploadedByName} · {new Date(a.createdAt).toLocaleString()}</div>
            </CardContent></Card>
          ))}
          {t.attachments.length === 0 && <div className="text-center py-4 text-muted-foreground text-sm">No attachments</div>}
          <div className="flex items-center gap-2 pt-2">
            <input
              ref={fileInputRef}
              type="file"
              className="hidden"
              onChange={e => {
                const file = e.target.files?.[0];
                if (file) uploadMutation.mutate(file);
              }}
            />
            <Button variant="outline" size="sm" onClick={() => fileInputRef.current?.click()} disabled={uploadMutation.isPending}>
              <Paperclip className="mr-1 h-4 w-4" />
              {uploadMutation.isPending ? 'Uploading...' : 'Attach File'}
            </Button>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
