import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { RefreshCw, Upload, Trash2, Search, FileText } from 'lucide-react';

interface KnowledgeDocument {
  id: string;
  title: string;
  fileName: string;
  fileType: string;
  fileSize: number;
  status: string;
  pageCount: number | null;
  chunkCount: number | null;
  errorMessage: string | null;
  createdAt: string;
}

const statusColors: Record<string, string> = {
  Pending: 'bg-gray-100 text-gray-700',
  Indexing: 'bg-blue-100 text-blue-700',
  Ready: 'bg-green-100 text-green-700',
  Failed: 'bg-red-100 text-red-700',
};

const fileTypeIcons: Record<string, string> = {
  '.pdf': '📄',
  '.docx': '📝',
  '.txt': '📃',
};

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function KnowledgeBasePage() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [showUpload, setShowUpload] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadTitle, setUploadTitle] = useState('');

  const { data, isLoading } = useQuery<{ items: KnowledgeDocument[] }>({
    queryKey: ['knowledge-documents', statusFilter],
    queryFn: () => api.get('/knowledge-documents', { params: { pageSize: 50, status: statusFilter || undefined } }).then(r => r.data),
  });

  const uploadMutation = useMutation({
    mutationFn: async () => {
      if (!uploadFile || !uploadTitle) return;
      const form = new FormData();
      form.append('title', uploadTitle);
      form.append('file', uploadFile);
      return api.post('/knowledge-documents', form, { headers: { 'Content-Type': 'multipart/form-data' } });
    },
    onSuccess: () => { setShowUpload(false); setUploadFile(null); setUploadTitle(''); queryClient.invalidateQueries({ queryKey: ['knowledge-documents'] }); },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/knowledge-documents/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['knowledge-documents'] }),
  });

  const reindexMutation = useMutation({
    mutationFn: (id: string) => api.post(`/knowledge-documents/${id}/index`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['knowledge-documents'] }),
  });

  const searchMutation = useMutation({
    mutationFn: (q: string) => api.post('/knowledge-documents/search', { query: q, topK: 5 }),
  });

  const filtered = data?.items?.filter(d =>
    !searchQuery || d.title.toLowerCase().includes(searchQuery.toLowerCase())
  ) ?? [];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Knowledge Base</h1>
          <p className="text-muted-foreground">Manage AI training documents</p>
        </div>
        <Button onClick={() => { setUploadFile(null); setUploadTitle(''); setShowUpload(true); }}>
          <Upload className="mr-2 h-4 w-4" /> Upload Document
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {['', 'Pending', 'Indexing', 'Ready', 'Failed'].map(s => (
          <Button key={s || 'all'} variant={statusFilter === s ? 'default' : 'outline'} size="sm" onClick={() => setStatusFilter(s)}>
            {s || 'All'}
          </Button>
        ))}
        <div className="flex-1" />
        <div className="relative">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search documents..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="pl-8 w-64"
          />
        </div>
      </div>

      <Card>
        <CardHeader className="pb-0 flex-row items-center justify-between">
          <CardTitle>All Documents</CardTitle>
          <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['knowledge-documents'] })}>
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
                  <TableHead>Document</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Size</TableHead>
                  <TableHead>Chunks</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-32">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filtered.map(d => (
                  <TableRow key={d.id}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <span>{fileTypeIcons[d.fileType] || '📁'}</span>
                        <div>
                          <p
                            className="font-medium text-sm hover:text-primary hover:underline cursor-pointer"
                            onClick={() => navigate(`/knowledge-base/${d.id}`)}
                          >
                            {d.title}
                          </p>
                          <p className="text-xs text-muted-foreground">{d.fileName}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell><Badge variant="outline">{d.fileType}</Badge></TableCell>
                    <TableCell className="text-sm text-muted-foreground">{formatSize(d.fileSize)}</TableCell>
                    <TableCell className="text-sm">{d.chunkCount ?? '—'}</TableCell>
                    <TableCell>
                      <Badge className={statusColors[d.status] || ''}>
                        {d.status}
                        {d.errorMessage && <span className="ml-1" title={d.errorMessage}>⚠️</span>}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        {(d.status === 'Failed' || d.status === 'Ready') && (
                          <Button variant="ghost" size="icon" onClick={() => reindexMutation.mutate(d.id)} title="Re-index">
                            <RefreshCw className="h-4 w-4" />
                          </Button>
                        )}
                        <Button variant="ghost" size="icon" onClick={() => { if (confirm('Delete?')) deleteMutation.mutate(d.id); }}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {filtered.length === 0 && (
                  <TableRow><TableCell colSpan={6} className="text-center py-8 text-muted-foreground">No documents found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Upload Dialog */}
      <Dialog open={showUpload} onOpenChange={setShowUpload}>
        <DialogContent>
          <DialogHeader><DialogTitle>Upload Knowledge Document</DialogTitle></DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Title *</Label>
              <Input value={uploadTitle} onChange={e => setUploadTitle(e.target.value)} placeholder="e.g., Employee Handbook 2025" />
            </div>
            <div className="space-y-2">
              <Label>File (PDF, DOCX, TXT) *</Label>
              <Input type="file" accept=".pdf,.docx,.txt"
                onChange={e => setUploadFile(e.target.files?.[0] ?? null)} />
            </div>
            <DialogFooter>
              <Button onClick={() => uploadMutation.mutate()} disabled={!uploadFile || !uploadTitle || uploadMutation.isPending}>
                {uploadMutation.isPending ? <Spinner className="h-4 w-4 mr-2" /> : <Upload className="h-4 w-4 mr-2" />}
                Upload & Index
              </Button>
            </DialogFooter>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
