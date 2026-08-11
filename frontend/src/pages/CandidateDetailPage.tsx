import { useRef } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ArrowLeft, Upload, Download, Trash2, Sparkles, Clock, Calendar, FileText, Loader2, Link2 } from 'lucide-react';
import { useToastStore } from '@/lib/useToast';

interface CandidateDocument {
  id: string;
  fileName: string;
  fileSize: number;
  uploadedByName: string;
  createdAt: string;
}

interface StageHistoryItem {
  id: string;
  fromStage: string;
  toStage: string;
  changedByName: string;
  notes?: string;
  createdAt: string;
}

interface InterviewSummary {
  id: string;
  scheduledAt: string;
  type: string;
  status: string;
  interviewerName: string;
  rating?: number;
}

interface CandidateDetail {
  id: string;
  jobVacancyId: string;
  jobVacancyTitle: string;
  fullName: string;
  email: string;
  phone?: string;
  source?: string;
  stage: string;
  aiSummaryJson?: string;
  rejectionReason?: string;
  documents: CandidateDocument[];
  stageHistory: StageHistoryItem[];
  interviews: InterviewSummary[];
  createdAt: string;
}

const STAGE_COLORS: Record<string, string> = {
  Applied: 'bg-blue-100 text-blue-800',
  Screening: 'bg-purple-100 text-purple-800',
  Test: 'bg-yellow-100 text-yellow-800',
  Interview: 'bg-orange-100 text-orange-800',
  Offering: 'bg-cyan-100 text-cyan-800',
  Hired: 'bg-green-100 text-green-800',
  Rejected: 'bg-red-100 text-red-800',
};

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

async function extractErrorMessage(error: unknown, fallback: string): Promise<string> {
  if (!axios.isAxiosError(error)) return fallback;
  const data = error.response?.data;
  if (data instanceof Blob) {
    try {
      const parsed = JSON.parse(await data.text());
      return parsed?.error ?? fallback;
    } catch {
      return fallback;
    }
  }
  return (data as { error?: string } | undefined)?.error ?? fallback;
}

export function CandidateDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data: candidate, isLoading } = useQuery<CandidateDetail>({
    queryKey: ['candidate', id],
    queryFn: () => api.get(`/candidates/${id}`).then((r) => r.data),
  });

  const addToast = useToastStore((s) => s.addToast);

  const uploadMutation = useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData();
      formData.append('file', file);
      return api.post(`/candidates/${id}/cv`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['candidate', id] });
      if (fileInputRef.current) fileInputRef.current.value = '';
    },
    onError: async (error) => {
      const message = await extractErrorMessage(error, 'Failed to upload CV');
      addToast({ title: 'CV upload failed', message, type: 'error' });
      if (fileInputRef.current) fileInputRef.current.value = '';
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (documentId: string) => api.delete(`/candidates/${id}/cv/${documentId}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['candidate', id] }),
    onError: async (error) => {
      const message = await extractErrorMessage(error, 'Failed to delete CV');
      addToast({ title: 'CV delete failed', message, type: 'error' });
    },
  });

  const inviteMutation = useMutation({
    mutationFn: () => api.post(`/candidates/${id}/portal-invite`).then((r) => r.data as { setupToken: string; expiresAt: string }),
    onSuccess: async (data) => {
      const link = `${window.location.origin}/portal/activate?token=${data.setupToken}`;
      try {
        await navigator.clipboard.writeText(link);
        addToast({ title: 'Portal invite link copied to clipboard', message: `Expires ${new Date(data.expiresAt).toLocaleDateString()}`, type: 'success' });
      } catch {
        addToast({ title: 'Portal invite link generated', message: link, type: 'success' });
      }
    },
  });

  const summarizeMutation = useMutation({
    mutationFn: () => api.post(`/candidates/${id}/ai-summarize`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['candidate', id] }),
  });

  const handleDownload = async (documentId: string, fileName: string) => {
    try {
      const response = await api.get(`/candidates/${id}/cv/${documentId}`, { responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.download = fileName;
      link.click();
      window.URL.revokeObjectURL(url);
    } catch (error) {
      const message = await extractErrorMessage(error, 'Failed to download CV');
      addToast({ title: 'CV download failed', message, type: 'error' });
    }
  };

  if (isLoading) return <div className="flex justify-center py-12"><Spinner className="h-8 w-8" /></div>;
  if (!candidate) return <div className="text-center py-12 text-muted-foreground">Candidate not found</div>;

  let summary: { skills?: string[]; experienceSummary?: string; educationSummary?: string } | null = null;
  try {
    if (candidate.aiSummaryJson) summary = JSON.parse(candidate.aiSummaryJson);
  } catch {
    summary = null;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate(-1)}><ArrowLeft className="h-4 w-4" /></Button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold flex items-center gap-2">
            {candidate.fullName}
            <Badge className={STAGE_COLORS[candidate.stage] || ''}>{candidate.stage}</Badge>
          </h1>
          <p className="text-muted-foreground">
            Applied for{' '}
            <Link to={`/recruitment/vacancies/${candidate.jobVacancyId}`} className="hover:text-primary hover:underline">
              {candidate.jobVacancyTitle}
            </Link>
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => inviteMutation.mutate()} disabled={inviteMutation.isPending}>
          {inviteMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Link2 className="mr-2 h-4 w-4" />}
          Copy Portal Invite Link
        </Button>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card><CardContent className="pt-4"><div className="text-xs text-muted-foreground">Email</div><div className="font-medium">{candidate.email}</div></CardContent></Card>
        <Card><CardContent className="pt-4"><div className="text-xs text-muted-foreground">Phone</div><div className="font-medium">{candidate.phone || 'N/A'}</div></CardContent></Card>
        <Card><CardContent className="pt-4"><div className="text-xs text-muted-foreground">Source</div><div className="font-medium">{candidate.source || 'N/A'}</div></CardContent></Card>
      </div>

      {candidate.rejectionReason && (
        <Card className="border-destructive/50">
          <CardContent className="pt-4">
            <p className="text-sm text-destructive"><strong>Rejection reason:</strong> {candidate.rejectionReason}</p>
          </CardContent>
        </Card>
      )}

      <Tabs defaultValue="cv">
        <TabsList>
          <TabsTrigger value="cv">CV & AI Summary</TabsTrigger>
          <TabsTrigger value="history">Stage History ({candidate.stageHistory.length})</TabsTrigger>
          <TabsTrigger value="interviews">Interviews ({candidate.interviews.length})</TabsTrigger>
        </TabsList>

        <TabsContent value="cv" className="space-y-4 pt-4">
          <Card>
            <CardHeader className="flex-row items-center justify-between">
              <CardTitle className="text-base">Documents</CardTitle>
              <div>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".pdf,.docx"
                  className="hidden"
                  onChange={(e) => {
                    const file = e.target.files?.[0];
                    if (file) uploadMutation.mutate(file);
                  }}
                />
                <Button variant="outline" size="sm" onClick={() => fileInputRef.current?.click()} disabled={uploadMutation.isPending}>
                  <Upload className="mr-2 h-4 w-4" /> {uploadMutation.isPending ? 'Uploading...' : 'Upload CV'}
                </Button>
              </div>
            </CardHeader>
            <CardContent>
              {candidate.documents.length === 0 ? (
                <p className="text-sm text-muted-foreground">No CV uploaded yet.</p>
              ) : (
                <div className="space-y-2">
                  {candidate.documents.map((d) => (
                    <div key={d.id} className="flex items-center justify-between rounded-md border p-2">
                      <div className="flex items-center gap-2 text-sm">
                        <FileText className="h-4 w-4 text-muted-foreground" />
                        <span>{d.fileName}</span>
                        <span className="text-xs text-muted-foreground">({formatBytes(d.fileSize)})</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <Button variant="ghost" size="icon" onClick={() => handleDownload(d.id, d.fileName)} aria-label="Download CV">
                          <Download className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          aria-label="Remove CV"
                          disabled={deleteMutation.isPending}
                          onClick={() => {
                            if (confirm(`Remove "${d.fileName}"?`)) deleteMutation.mutate(d.id);
                          }}
                        >
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex-row items-center justify-between">
              <CardTitle className="text-base">AI Summary</CardTitle>
              <Button
                size="sm"
                variant="outline"
                onClick={() => summarizeMutation.mutate()}
                disabled={summarizeMutation.isPending || candidate.documents.length === 0}
              >
                {summarizeMutation.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Sparkles className="mr-2 h-4 w-4" />}
                Generate Summary
              </Button>
            </CardHeader>
            <CardContent>
              {!summary ? (
                <p className="text-sm text-muted-foreground">No AI summary yet. Upload a CV and click "Generate Summary".</p>
              ) : (
                <div className="space-y-3">
                  {summary.skills && summary.skills.length > 0 && (
                    <div>
                      <Label3>Skills</Label3>
                      <div className="flex flex-wrap gap-1 mt-1">
                        {summary.skills.map((s) => <Badge key={s} variant="outline">{s}</Badge>)}
                      </div>
                    </div>
                  )}
                  {summary.experienceSummary && (
                    <div><Label3>Experience</Label3><p className="text-sm mt-1">{summary.experienceSummary}</p></div>
                  )}
                  {summary.educationSummary && (
                    <div><Label3>Education</Label3><p className="text-sm mt-1">{summary.educationSummary}</p></div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="history" className="pt-4">
          <Card>
            <CardContent className="pt-4 space-y-3">
              {candidate.stageHistory.length === 0 ? (
                <p className="text-sm text-muted-foreground">No stage changes yet.</p>
              ) : (
                candidate.stageHistory.map((h) => (
                  <div key={h.id} className="flex items-start gap-3 border-l-2 border-primary/30 pl-3 py-1">
                    <Clock className="h-4 w-4 text-muted-foreground mt-0.5" />
                    <div>
                      <p className="text-sm font-medium">{h.fromStage} → {h.toStage}</p>
                      {h.notes && <p className="text-xs text-muted-foreground">{h.notes}</p>}
                      <p className="text-xs text-muted-foreground">{h.changedByName} · {new Date(h.createdAt).toLocaleString()}</p>
                    </div>
                  </div>
                ))
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="interviews" className="pt-4">
          <Card>
            <CardContent className="pt-4 space-y-3">
              {candidate.interviews.length === 0 ? (
                <p className="text-sm text-muted-foreground">No interviews scheduled yet.</p>
              ) : (
                candidate.interviews.map((i) => (
                  <div key={i.id} className="flex items-center justify-between rounded-md border p-3">
                    <div className="flex items-center gap-2">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                      <div>
                        <p className="text-sm font-medium">{i.type} with {i.interviewerName}</p>
                        <p className="text-xs text-muted-foreground">{new Date(i.scheduledAt).toLocaleString()}</p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      {i.rating && <span className="text-xs text-muted-foreground">Rating: {i.rating}/5</span>}
                      <Badge variant="outline">{i.status}</Badge>
                    </div>
                  </div>
                ))
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}

function Label3({ children }: { children: React.ReactNode }) {
  return <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">{children}</p>;
}
