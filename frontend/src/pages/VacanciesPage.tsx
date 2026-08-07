import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import api from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter } from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { Plus, Briefcase, Users, TrendingUp, Send, Lock } from 'lucide-react';
import { useToastStore } from '@/lib/useToast';

interface VacancyItem {
  id: string;
  title: string;
  departmentName?: string;
  positionName?: string;
  openingsCount: number;
  status: string;
  candidateCount: number;
  createdAt: string;
}

interface Department {
  id: string;
  name: string;
}

interface RecruitmentStats {
  totalVacancies: number;
  publishedVacancies: number;
  totalCandidates: number;
  candidatesPerStage: Record<string, number>;
  averageDaysInPipeline: number;
}

const STATUS_COLORS: Record<string, string> = {
  Draft: 'bg-muted text-muted-foreground',
  Published: 'bg-success/10 text-success',
  Closed: 'bg-warning/10 text-warning',
  Filled: 'bg-info/10 text-info',
};

export function VacanciesPage() {
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const [statusFilter, setStatusFilter] = useState('');
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState({ title: '', description: '', requirements: '', departmentId: '', openingsCount: 1 });

  const canManage = user?.roles?.some((r) => ['HRD', 'Manager', 'Super Admin'].includes(r));

  const { data, isLoading } = useQuery<{ items: VacancyItem[]; totalCount: number }>({
    queryKey: ['job-vacancies', statusFilter],
    queryFn: () => api.get('/job-vacancies', { params: { pageSize: 50, status: statusFilter || undefined } }).then((r) => r.data),
  });

  const { data: stats } = useQuery<RecruitmentStats>({
    queryKey: ['candidates', 'stats'],
    queryFn: () => api.get('/candidates/stats').then((r) => r.data),
    enabled: canManage,
  });

  const { data: departments } = useQuery<Department[]>({
    queryKey: ['departments'],
    queryFn: () => api.get('/departments').then((r) => r.data),
    enabled: createOpen,
  });

  const addToast = useToastStore((s) => s.addToast);
  const onMutationError = (err: unknown) => {
    const resp = (err as { response?: { data?: { message?: string; error?: string } } })?.response?.data;
    addToast({ title: 'Action failed', message: resp?.message ?? resp?.error ?? 'Something went wrong. Please try again.', type: 'error' });
  };

  const createMutation = useMutation({
    mutationFn: () =>
      api.post('/job-vacancies', {
        title: form.title,
        description: form.description,
        requirements: form.requirements,
        departmentId: form.departmentId || null,
        positionId: null,
        openingsCount: form.openingsCount,
      }),
    onSuccess: () => {
      setCreateOpen(false);
      setForm({ title: '', description: '', requirements: '', departmentId: '', openingsCount: 1 });
      queryClient.invalidateQueries({ queryKey: ['job-vacancies'] });
    },
    onError: onMutationError,
  });

  const publishMutation = useMutation({
    mutationFn: (id: string) => api.post(`/job-vacancies/${id}/publish`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['job-vacancies'] }),
    onError: onMutationError,
  });

  const closeMutation = useMutation({
    mutationFn: (id: string) => api.post(`/job-vacancies/${id}/close`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['job-vacancies'] }),
    onError: onMutationError,
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Job Vacancies</h1>
          <p className="text-muted-foreground">Manage open positions and the candidate pipeline</p>
        </div>
        {canManage && (
          <Dialog open={createOpen} onOpenChange={setCreateOpen}>
            <DialogTrigger asChild>
              <Button><Plus className="mr-2 h-4 w-4" /> New Vacancy</Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-lg">
              <DialogHeader><DialogTitle>Create Job Vacancy</DialogTitle></DialogHeader>
              <div className="space-y-4 mt-2">
                <div className="space-y-2">
                  <Label>Title</Label>
                  <Input value={form.title} onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))} placeholder="Backend Engineer" />
                </div>
                <div className="space-y-2">
                  <Label>Description</Label>
                  <Textarea value={form.description} onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} placeholder="Role overview..." rows={3} />
                </div>
                <div className="space-y-2">
                  <Label>Requirements</Label>
                  <Textarea value={form.requirements} onChange={(e) => setForm((p) => ({ ...p, requirements: e.target.value }))} placeholder="5+ years experience with..." rows={3} />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Department</Label>
                    <Select value={form.departmentId} onValueChange={(v) => setForm((p) => ({ ...p, departmentId: v }))}>
                      <SelectTrigger><SelectValue placeholder="Select..." /></SelectTrigger>
                      <SelectContent>
                        {departments?.map((d) => <SelectItem key={d.id} value={d.id}>{d.name}</SelectItem>)}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label>Openings</Label>
                    <Input
                      type="number"
                      min={1}
                      value={form.openingsCount}
                      onChange={(e) => setForm((p) => ({ ...p, openingsCount: Number(e.target.value) || 1 }))}
                    />
                  </div>
                </div>
              </div>
              <DialogFooter>
                <Button onClick={() => createMutation.mutate()} disabled={!form.title || createMutation.isPending}>
                  {createMutation.isPending ? <Spinner /> : 'Create Vacancy'}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {stats && (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Vacancies</CardTitle>
              <Briefcase className="h-4 w-4 text-info" />
            </CardHeader>
            <CardContent><div className="text-2xl font-bold">{stats.totalVacancies}</div></CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Published</CardTitle>
              <Send className="h-4 w-4 text-success" />
            </CardHeader>
            <CardContent><div className="text-2xl font-bold">{stats.publishedVacancies}</div></CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Candidates</CardTitle>
              <Users className="h-4 w-4 text-primary" />
            </CardHeader>
            <CardContent><div className="text-2xl font-bold">{stats.totalCandidates}</div></CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Avg. Days in Pipeline</CardTitle>
              <TrendingUp className="h-4 w-4 text-warning" />
            </CardHeader>
            <CardContent><div className="text-2xl font-bold">{stats.averageDaysInPipeline}</div></CardContent>
          </Card>
        </div>
      )}

      <div className="flex items-center gap-4">
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-44"><SelectValue placeholder="All Statuses" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Draft">Draft</SelectItem>
            <SelectItem value="Published">Published</SelectItem>
            <SelectItem value="Closed">Closed</SelectItem>
            <SelectItem value="Filled">Filled</SelectItem>
          </SelectContent>
        </Select>
        {statusFilter && <Button variant="ghost" size="sm" onClick={() => setStatusFilter('')}>Clear</Button>}
      </div>

      <Card>
        <CardContent className="pt-4">
          {isLoading ? (
            <div className="flex justify-center py-8"><Spinner className="h-8 w-8" /></div>
          ) : (
            <div className="space-y-3">
              {data?.items?.map((v) => (
                <div key={v.id} className="flex items-center justify-between rounded-lg border p-4 hover:bg-muted/50 transition-colors">
                  <div>
                    <Link to={`/recruitment/candidates?vacancyId=${v.id}`} className="font-medium hover:text-primary hover:underline">{v.title}</Link>
                    <p className="text-sm text-muted-foreground mt-0.5">
                      {v.departmentName ?? 'No department'} · {v.openingsCount} opening{v.openingsCount !== 1 ? 's' : ''} · {v.candidateCount} candidate{v.candidateCount !== 1 ? 's' : ''}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge className={STATUS_COLORS[v.status] || ''}>{v.status}</Badge>
                    {canManage && v.status === 'Draft' && (
                      <Button size="sm" variant="outline" onClick={() => publishMutation.mutate(v.id)}>
                        <Send className="mr-1 h-3 w-3" /> Publish
                      </Button>
                    )}
                    {canManage && v.status === 'Published' && (
                      <Button size="sm" variant="outline" onClick={() => closeMutation.mutate(v.id)}>
                        <Lock className="mr-1 h-3 w-3" /> Close
                      </Button>
                    )}
                  </div>
                </div>
              ))}
              {(!data?.items || data.items.length === 0) && (
                <div className="text-center py-8 text-muted-foreground">No job vacancies found</div>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
