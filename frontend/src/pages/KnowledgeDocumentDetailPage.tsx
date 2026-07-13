import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { ArrowLeft, FileText, Calendar, Hash, HardDrive, AlertTriangle } from 'lucide-react';

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString();
}

interface KnowledgeChunk {
  documentId: string;
  documentTitle: string;
  chunkId: string;
  content: string;
  relevance: number;
}

interface DocumentDetail {
  id: string;
  title: string;
  fileName: string;
  fileType: string;
  contentType: string;
  fileSize: number;
  status: string;
  pageCount: number | null;
  chunkCount: number | null;
  errorMessage: string | null;
  sampleChunks: KnowledgeChunk[];
  createdAt: string;
  updatedAt: string;
}

const statusColors: Record<string, string> = {
  Pending: 'bg-gray-100 text-gray-700',
  Indexing: 'bg-blue-100 text-blue-700',
  Ready: 'bg-green-100 text-green-700',
  Failed: 'bg-red-100 text-red-700',
};

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function KnowledgeDocumentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: doc, isLoading } = useQuery<DocumentDetail>({
    queryKey: ['knowledge-document', id],
    queryFn: () => api.get(`/knowledge-documents/${id}`).then(r => r.data),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner className="h-8 w-8" />
      </div>
    );
  }

  if (!doc) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">Document not found</p>
        <Button variant="link" onClick={() => navigate('/knowledge-base')}>Back to Knowledge Base</Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/knowledge-base')}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{doc.title}</h1>
          <p className="text-muted-foreground">{doc.fileName}</p>
        </div>
      </div>

      {/* Status Banner */}
      {doc.status === 'Failed' && doc.errorMessage && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 text-red-700 border border-red-200">
          <AlertTriangle className="h-5 w-5" />
          <div>
            <p className="font-medium">Indexing Failed</p>
            <p className="text-sm">{doc.errorMessage}</p>
          </div>
        </div>
      )}

      {/* Metadata */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="pb-2"><CardTitle className="text-sm font-medium text-muted-foreground">Status</CardTitle></CardHeader>
          <CardContent>
            <Badge className={statusColors[doc.status] || ''}>{doc.status}</Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2 flex flex-row items-center gap-2"><FileText className="h-4 w-4 text-muted-foreground" /><CardTitle className="text-sm font-medium text-muted-foreground">Type</CardTitle></CardHeader>
          <CardContent><p className="text-lg font-semibold">{doc.fileType}</p></CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2 flex flex-row items-center gap-2"><HardDrive className="h-4 w-4 text-muted-foreground" /><CardTitle className="text-sm font-medium text-muted-foreground">Size</CardTitle></CardHeader>
          <CardContent><p className="text-lg font-semibold">{formatSize(doc.fileSize)}</p></CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2 flex flex-row items-center gap-2"><Hash className="h-4 w-4 text-muted-foreground" /><CardTitle className="text-sm font-medium text-muted-foreground">Chunks</CardTitle></CardHeader>
          <CardContent><p className="text-lg font-semibold">{doc.chunkCount ?? '—'}</p></CardContent>
        </Card>
      </div>

      {/* Details */}
      <div className="grid grid-cols-2 gap-4">
        <Card>
          <CardHeader><CardTitle>Document Info</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground">Content Type</span>
              <span>{doc.contentType}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground">Page Count</span>
              <span>{doc.pageCount ?? '—'}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground flex items-center gap-1"><Calendar className="h-3.5 w-3.5" /> Created</span>
              <span>{formatDate(doc.createdAt)}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground flex items-center gap-1"><Calendar className="h-3.5 w-3.5" /> Updated</span>
              <span>{formatDate(doc.updatedAt)}</span>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>Indexing Stats</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground">Total Chunks</span>
              <span className="font-medium">{doc.chunkCount ?? '—'}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground">Avg Chunk Size</span>
              <span>{doc.chunkCount ? `${Math.round(doc.fileSize / doc.chunkCount)} B` : '—'}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground">Sample Chunks</span>
              <span>{doc.sampleChunks?.length ?? 0}</span>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Sample Chunks */}
      {doc.sampleChunks && doc.sampleChunks.length > 0 && (
        <Card>
          <CardHeader><CardTitle>Content Preview (Sample Chunks)</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            {doc.sampleChunks.map((chunk, i) => (
              <div key={chunk.chunkId || i} className="p-3 rounded-md bg-muted/50 border">
                <p className="text-xs text-muted-foreground mb-1">Chunk #{i + 1}</p>
                <p className="text-sm whitespace-pre-wrap">{chunk.content.substring(0, 300)}{chunk.content.length > 300 ? '...' : ''}</p>
              </div>
            ))}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
