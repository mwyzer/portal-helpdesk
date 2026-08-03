import { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter } from '@/components/ui/dialog';
import { ArrowRight, X, Plus, Download, User } from 'lucide-react';

interface CandidateItem {
  id: string;
  jobVacancyId: string;
  jobVacancyTitle: string;
  fullName: string;
  email: string;
  source?: string;
  stage: string;
  hasAISummary: boolean;
}

interface VacancyOption {
  id: string;
  title: string;
}

const STAGES = ['Applied', 'Screening', 'Test', 'Interview', 'Offering', 'Hired', 'Rejected'];

const STAGE_COLORS: Record<string, string> = {
  Applied: 'border-t-blue-400',
  Screening: 'border-t-purple-400',
  Test: 'border-t-yellow-400',
  Interview: 'border-t-orange-400',
  Offering: 'border-t-cyan-400',
  Hired: 'border-t-green-500',
  Rejected: 'border-t-red-400',
};

export function CandidatesPage() {
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const [vacancyId, setVacancyId] = useState(searchParams.get('vacancyId') ?? '');
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', source: '' });

  const { data: vacancies } = useQuery<{ items: VacancyOption[] }>({
    queryKey: ['job-vacancies', 'for-candidates'],
    queryFn: () => api.get('/job-vacancies', { params: { pageSize: 100, status: 'Published' } }).then((r) => r.data),
  });

  const { data, isLoading } = useQuery<{ items: CandidateItem[] }>({
    queryKey: ['candidates', 'pipeline', vacancyId],
    queryFn: () => api.get('/candidates', { params: { pageSize: 200, jobVacancyId: vacancyId || undefined } }).then((r) => r.data),
  });

  const createMutation = useMutation({
    mutationFn: () =>
      api.post('/candidates', {
        jobVacancyId: vacancyId,
        fullName: form.fullName,
        email: form.email,
        phone: form.phone || null,
        source: form.source || null,
      }),
    onSuccess: () => {
      setCreateOpen(false);
      setForm({ fullName: '', email: '', phone: '', source: '' });
      queryClient.invalidateQueries({ queryKey: ['candidates'] });
    },
  });

  const advanceMutation = useMutation({
    mutationFn: (id: string) => api.post(`/candidates/${id}/advance-stage`, {}),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['candidates'] }),
  });

  const rejectMutation = useMutation({
    mutationFn: (id: string) => api.post(`/candidates/${id}/reject`, { reason: 'Not proceeding' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['candidates'] }),
  });

  const handleExport = async () => {
    const response = await api.get('/candidates/export', {
      params: { jobVacancyId: vacancyId || undefined },
      responseType: 'blob',
    });
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.download = `candidates-export-${new Date().toISOString().split('T')[0]}.xlsx`;
    link.click();
    window.URL.revokeObjectURL(url);
  };

  const columns = useMemo(() => {
    const grouped: Record<string, CandidateItem[]> = Object.fromEntries(STAGES.map((s) => [s, []]));
    data?.items?.forEach((c) => {
      (grouped[c.stage] ??= []).push(c);
    });
    return grouped;
  }, [data]);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Candidate Pipeline</h1>
          <p className="text-muted-foreground">Track candidates through the hiring process</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={handleExport}>
            <Download className="mr-2 h-4 w-4" /> Export
          </Button>
          <Dialog open={createOpen} onOpenChange={setCreateOpen}>
            <DialogTrigger asChild>
              <Button disabled={!vacancyId}><Plus className="mr-2 h-4 w-4" /> Add Candidate</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader><DialogTitle>Add Candidate</DialogTitle></DialogHeader>
              <div className="space-y-4 mt-2">
                <div className="space-y-2">
                  <Label>Full Name</Label>
                  <Input value={form.fullName} onChange={(e) => setForm((p) => ({ ...p, fullName: e.target.value }))} />
                </div>
                <div className="space-y-2">
                  <Label>Email</Label>
                  <Input type="email" value={form.email} onChange={(e) => setForm((p) => ({ ...p, email: e.target.value }))} />
                </div>
                <div className="space-y-2">
                  <Label>Phone</Label>
                  <Input value={form.phone} onChange={(e) => setForm((p) => ({ ...p, phone: e.target.value }))} />
                </div>
                <div className="space-y-2">
                  <Label>Source</Label>
                  <Input value={form.source} onChange={(e) => setForm((p) => ({ ...p, source: e.target.value }))} placeholder="LinkedIn, Referral, ..." />
                </div>
              </div>
              <DialogFooter>
                <Button onClick={() => createMutation.mutate()} disabled={!form.fullName || !form.email || createMutation.isPending}>
                  {createMutation.isPending ? <Spinner /> : 'Add Candidate'}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      <Select value={vacancyId} onValueChange={setVacancyId}>
        <SelectTrigger className="w-72"><SelectValue placeholder="Select a vacancy to view its pipeline..." /></SelectTrigger>
        <SelectContent>
          {vacancies?.items?.map((v) => <SelectItem key={v.id} value={v.id}>{v.title}</SelectItem>)}
        </SelectContent>
      </Select>

      {isLoading ? (
        <div className="flex justify-center py-12"><Spinner className="h-8 w-8" /></div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-7 gap-4">
          {STAGES.map((stage) => (
            <Card key={stage} className={`border-t-4 ${STAGE_COLORS[stage]}`}>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm flex items-center justify-between">
                  {stage}
                  <span className="text-xs text-muted-foreground font-normal">{columns[stage]?.length ?? 0}</span>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 min-h-[100px]">
                {columns[stage]?.map((c) => (
                  <div key={c.id} className="rounded-md border p-2 text-sm space-y-1 bg-card">
                    <Link to={`/recruitment/candidates/${c.id}`} className="font-medium hover:text-primary hover:underline flex items-center gap-1">
                      <User className="h-3 w-3" /> {c.fullName}
                    </Link>
                    <p className="text-xs text-muted-foreground truncate">{c.source || 'Unknown source'}</p>
                    {stage !== 'Hired' && stage !== 'Rejected' && (
                      <div className="flex gap-1 pt-1">
                        <Button
                          size="sm"
                          variant="ghost"
                          className="h-6 px-1.5 text-xs"
                          onClick={() => advanceMutation.mutate(c.id)}
                          aria-label="Advance stage"
                        >
                          <ArrowRight className="h-3 w-3" />
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          className="h-6 px-1.5 text-xs text-destructive"
                          onClick={() => rejectMutation.mutate(c.id)}
                          aria-label="Reject candidate"
                        >
                          <X className="h-3 w-3" />
                        </Button>
                      </div>
                    )}
                  </div>
                ))}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
