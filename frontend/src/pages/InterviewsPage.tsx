import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Select, SelectTrigger, SelectValue, SelectContent, SelectItem } from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Textarea } from '@/components/ui/textarea';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter } from '@/components/ui/dialog';
import { Plus, Calendar, Sparkles, Loader2, CalendarPlus } from 'lucide-react';

interface InterviewItem {
  id: string;
  candidateId: string;
  candidateName: string;
  interviewerName: string;
  scheduledAt: string;
  durationMinutes: number;
  type: string;
  status: string;
  rating?: number;
}

interface CandidateOption {
  id: string;
  fullName: string;
}

interface VacancyOption {
  id: string;
  title: string;
}

interface SlotItem {
  id: string;
  interviewerName: string;
  jobVacancyTitle: string;
  scheduledAt: string;
  durationMinutes: number;
  type: string;
  status: string;
}

interface UserOption {
  id: string;
  fullName: string;
}

interface AIQuestion {
  question: string;
  category?: string;
}

const STATUS_COLORS: Record<string, string> = {
  Scheduled: 'bg-info/10 text-info',
  Completed: 'bg-success/10 text-success',
  Cancelled: 'bg-destructive/10 text-destructive',
};

export function InterviewsPage() {
  const queryClient = useQueryClient();
  const [statusFilter, setStatusFilter] = useState('');
  const [createOpen, setCreateOpen] = useState(false);
  const [createSlotOpen, setCreateSlotOpen] = useState(false);
  const [completeId, setCompleteId] = useState<string | null>(null);
  const [aiQuestions, setAiQuestions] = useState<AIQuestion[]>([]);
  const [form, setForm] = useState({ candidateId: '', interviewerId: '', scheduledAt: '', durationMinutes: 60, type: 'Video' });
  const [slotForm, setSlotForm] = useState({ interviewerId: '', jobVacancyId: '', scheduledAt: '', durationMinutes: 60, type: 'Video' });
  const [feedbackForm, setFeedbackForm] = useState({ feedback: '', rating: 3, recommendation: 'Yes' });

  const { data, isLoading } = useQuery<{ items: InterviewItem[] }>({
    queryKey: ['interviews', statusFilter],
    queryFn: () => api.get('/interviews', { params: { pageSize: 100 } }).then((r) => r.data),
  });

  const { data: candidates } = useQuery<{ items: CandidateOption[] }>({
    queryKey: ['candidates', 'for-interviews'],
    queryFn: () => api.get('/candidates', { params: { pageSize: 200 } }).then((r) => r.data),
    enabled: createOpen,
  });

  const { data: users } = useQuery<{ items: UserOption[] }>({
    queryKey: ['users', 'for-interviews'],
    queryFn: () => api.get('/users', { params: { pageSize: 999 } }).then((r) => r.data),
    enabled: createOpen || createSlotOpen,
  });

  const { data: vacancies } = useQuery<{ items: VacancyOption[] }>({
    queryKey: ['vacancies', 'for-slots'],
    queryFn: () => api.get('/job-vacancies', { params: { pageSize: 200 } }).then((r) => r.data),
    enabled: createSlotOpen,
  });

  const { data: slotsData, isLoading: slotsLoading } = useQuery<SlotItem[]>({
    queryKey: ['interview-slots'],
    queryFn: () => api.get('/interviews/slots').then((r) => r.data),
  });

  const createMutation = useMutation({
    mutationFn: () =>
      api.post('/interviews', {
        candidateId: form.candidateId,
        interviewerId: form.interviewerId,
        scheduledAt: new Date(form.scheduledAt).toISOString(),
        durationMinutes: form.durationMinutes,
        type: form.type,
      }),
    onSuccess: () => {
      setCreateOpen(false);
      setForm({ candidateId: '', interviewerId: '', scheduledAt: '', durationMinutes: 60, type: 'Video' });
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
    },
  });

  const completeMutation = useMutation({
    mutationFn: () =>
      api.post(`/interviews/${completeId}/complete`, feedbackForm),
    onSuccess: () => {
      setCompleteId(null);
      setFeedbackForm({ feedback: '', rating: 3, recommendation: 'Yes' });
      queryClient.invalidateQueries({ queryKey: ['interviews'] });
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => api.post(`/interviews/${id}/cancel`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['interviews'] }),
  });

  const createSlotMutation = useMutation({
    mutationFn: () =>
      api.post('/interviews/slots', {
        interviewerId: slotForm.interviewerId,
        jobVacancyId: slotForm.jobVacancyId,
        scheduledAt: new Date(slotForm.scheduledAt).toISOString(),
        durationMinutes: slotForm.durationMinutes,
        type: slotForm.type,
      }),
    onSuccess: () => {
      setCreateSlotOpen(false);
      setSlotForm({ interviewerId: '', jobVacancyId: '', scheduledAt: '', durationMinutes: 60, type: 'Video' });
      queryClient.invalidateQueries({ queryKey: ['interview-slots'] });
    },
  });

  const cancelSlotMutation = useMutation({
    mutationFn: (id: string) => api.post(`/interviews/slots/${id}/cancel`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['interview-slots'] }),
  });

  const questionsMutation = useMutation({
    mutationFn: (id: string) => api.post(`/interviews/${id}/ai-questions`).then((r) => r.data),
    onSuccess: (data) => setAiQuestions(data.questions ?? []),
  });

  const items = (data?.items ?? []).filter((i) => !statusFilter || i.status === statusFilter);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Interviews</h1>
          <p className="text-muted-foreground">Schedule and manage candidate interviews</p>
        </div>
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogTrigger asChild>
            <Button><Plus className="mr-2 h-4 w-4" /> Schedule Interview</Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>Schedule Interview</DialogTitle></DialogHeader>
            <div className="space-y-4 mt-2">
              <div className="space-y-2">
                <Label>Candidate</Label>
                <Select value={form.candidateId} onValueChange={(v) => setForm((p) => ({ ...p, candidateId: v }))}>
                  <SelectTrigger><SelectValue placeholder="Select candidate..." /></SelectTrigger>
                  <SelectContent>
                    {candidates?.items?.map((c) => <SelectItem key={c.id} value={c.id}>{c.fullName}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Interviewer</Label>
                <Select value={form.interviewerId} onValueChange={(v) => setForm((p) => ({ ...p, interviewerId: v }))}>
                  <SelectTrigger><SelectValue placeholder="Select interviewer..." /></SelectTrigger>
                  <SelectContent>
                    {users?.items?.map((u) => <SelectItem key={u.id} value={u.id}>{u.fullName}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Date & Time</Label>
                  <Input type="datetime-local" value={form.scheduledAt} onChange={(e) => setForm((p) => ({ ...p, scheduledAt: e.target.value }))} />
                </div>
                <div className="space-y-2">
                  <Label>Duration (min)</Label>
                  <Input type="number" min={15} value={form.durationMinutes} onChange={(e) => setForm((p) => ({ ...p, durationMinutes: Number(e.target.value) || 60 }))} />
                </div>
              </div>
              <div className="space-y-2">
                <Label>Type</Label>
                <Select value={form.type} onValueChange={(v) => setForm((p) => ({ ...p, type: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Phone">Phone</SelectItem>
                    <SelectItem value="Video">Video</SelectItem>
                    <SelectItem value="OnSite">On-Site</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <DialogFooter>
              <Button
                onClick={() => createMutation.mutate()}
                disabled={!form.candidateId || !form.interviewerId || !form.scheduledAt || createMutation.isPending}
              >
                {createMutation.isPending ? <Spinner /> : 'Schedule'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      <div className="flex items-center gap-4">
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-44"><SelectValue placeholder="All Statuses" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Scheduled">Scheduled</SelectItem>
            <SelectItem value="Completed">Completed</SelectItem>
            <SelectItem value="Cancelled">Cancelled</SelectItem>
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
              {items.map((i) => (
                <div key={i.id} className="flex items-center justify-between rounded-lg border p-4">
                  <div className="flex items-center gap-3">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    <div>
                      <Link to={`/recruitment/candidates/${i.candidateId}`} className="font-medium hover:text-primary hover:underline">
                        {i.candidateName}
                      </Link>
                      <p className="text-sm text-muted-foreground">
                        {i.type} with {i.interviewerName} · {new Date(i.scheduledAt).toLocaleString()} ({i.durationMinutes}m)
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge className={STATUS_COLORS[i.status] || ''}>{i.status}</Badge>
                    {i.status === 'Scheduled' && (
                      <>
                        <Button size="sm" variant="outline" onClick={() => questionsMutation.mutate(i.id)} disabled={questionsMutation.isPending}>
                          {questionsMutation.isPending && questionsMutation.variables === i.id
                            ? <Loader2 className="h-3 w-3 animate-spin" />
                            : <Sparkles className="h-3 w-3" />}
                        </Button>
                        <Button size="sm" variant="outline" onClick={() => setCompleteId(i.id)}>Complete</Button>
                        <Button size="sm" variant="ghost" className="text-destructive" onClick={() => cancelMutation.mutate(i.id)}>Cancel</Button>
                      </>
                    )}
                  </div>
                </div>
              ))}
              {items.length === 0 && <div className="text-center py-8 text-muted-foreground">No interviews found</div>}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <div>
            <CardTitle className="text-base">Interview Slots</CardTitle>
            <p className="text-sm text-muted-foreground">
              Open time slots for candidates to book themselves via the candidate portal
            </p>
          </div>
          <Dialog open={createSlotOpen} onOpenChange={setCreateSlotOpen}>
            <DialogTrigger asChild>
              <Button size="sm" variant="outline"><CalendarPlus className="mr-2 h-4 w-4" /> Open Slot</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader><DialogTitle>Open Interview Slot</DialogTitle></DialogHeader>
              <div className="space-y-4 mt-2">
                <div className="space-y-2">
                  <Label>Vacancy</Label>
                  <Select value={slotForm.jobVacancyId} onValueChange={(v) => setSlotForm((p) => ({ ...p, jobVacancyId: v }))}>
                    <SelectTrigger><SelectValue placeholder="Select vacancy..." /></SelectTrigger>
                    <SelectContent>
                      {vacancies?.items?.map((v) => <SelectItem key={v.id} value={v.id}>{v.title}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label>Interviewer</Label>
                  <Select value={slotForm.interviewerId} onValueChange={(v) => setSlotForm((p) => ({ ...p, interviewerId: v }))}>
                    <SelectTrigger><SelectValue placeholder="Select interviewer..." /></SelectTrigger>
                    <SelectContent>
                      {users?.items?.map((u) => <SelectItem key={u.id} value={u.id}>{u.fullName}</SelectItem>)}
                    </SelectContent>
                  </Select>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Date & Time</Label>
                    <Input type="datetime-local" value={slotForm.scheduledAt} onChange={(e) => setSlotForm((p) => ({ ...p, scheduledAt: e.target.value }))} />
                  </div>
                  <div className="space-y-2">
                    <Label>Duration (min)</Label>
                    <Input type="number" min={15} value={slotForm.durationMinutes} onChange={(e) => setSlotForm((p) => ({ ...p, durationMinutes: Number(e.target.value) || 60 }))} />
                  </div>
                </div>
                <div className="space-y-2">
                  <Label>Type</Label>
                  <Select value={slotForm.type} onValueChange={(v) => setSlotForm((p) => ({ ...p, type: v }))}>
                    <SelectTrigger><SelectValue /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Phone">Phone</SelectItem>
                      <SelectItem value="Video">Video</SelectItem>
                      <SelectItem value="OnSite">On-Site</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <DialogFooter>
                <Button
                  onClick={() => createSlotMutation.mutate()}
                  disabled={!slotForm.jobVacancyId || !slotForm.interviewerId || !slotForm.scheduledAt || createSlotMutation.isPending}
                >
                  {createSlotMutation.isPending ? <Spinner /> : 'Open Slot'}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </CardHeader>
        <CardContent>
          {slotsLoading ? (
            <div className="flex justify-center py-8"><Spinner className="h-8 w-8" /></div>
          ) : (
            <div className="space-y-3">
              {(slotsData ?? []).map((s) => (
                <div key={s.id} className="flex items-center justify-between rounded-lg border p-4">
                  <div>
                    <p className="font-medium">{s.jobVacancyTitle}</p>
                    <p className="text-sm text-muted-foreground">
                      {s.type} with {s.interviewerName} · {new Date(s.scheduledAt).toLocaleString()} ({s.durationMinutes}m)
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge className={s.status === 'Open' ? 'bg-info/10 text-info' : s.status === 'Booked' ? 'bg-success/10 text-success' : 'bg-muted text-muted-foreground'}>
                      {s.status}
                    </Badge>
                    {s.status === 'Open' && (
                      <Button size="sm" variant="ghost" className="text-destructive" onClick={() => cancelSlotMutation.mutate(s.id)}>Cancel</Button>
                    )}
                  </div>
                </div>
              ))}
              {(slotsData ?? []).length === 0 && <div className="text-center py-8 text-muted-foreground">No interview slots opened yet</div>}
            </div>
          )}
        </CardContent>
      </Card>

      {aiQuestions.length > 0 && (
        <Card>
          <CardHeader><CardTitle className="text-base flex items-center gap-2"><Sparkles className="h-4 w-4" /> AI-Suggested Questions</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            {aiQuestions.map((q, idx) => (
              <div key={idx} className="flex items-start gap-2 text-sm">
                <Badge variant="outline" className="shrink-0">{q.category || 'General'}</Badge>
                <span>{q.question}</span>
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      <Dialog open={!!completeId} onOpenChange={() => setCompleteId(null)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Complete Interview</DialogTitle></DialogHeader>
          <div className="space-y-4 mt-2">
            <div className="space-y-2">
              <Label>Feedback</Label>
              <Textarea value={feedbackForm.feedback} onChange={(e) => setFeedbackForm((p) => ({ ...p, feedback: e.target.value }))} rows={4} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Rating (1-5)</Label>
                <Input type="number" min={1} max={5} value={feedbackForm.rating} onChange={(e) => setFeedbackForm((p) => ({ ...p, rating: Number(e.target.value) || 1 }))} />
              </div>
              <div className="space-y-2">
                <Label>Recommendation</Label>
                <Select value={feedbackForm.recommendation} onValueChange={(v) => setFeedbackForm((p) => ({ ...p, recommendation: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="StrongYes">Strong Yes</SelectItem>
                    <SelectItem value="Yes">Yes</SelectItem>
                    <SelectItem value="No">No</SelectItem>
                    <SelectItem value="StrongNo">Strong No</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button onClick={() => completeMutation.mutate()} disabled={!feedbackForm.feedback || completeMutation.isPending}>
              {completeMutation.isPending ? <Spinner /> : 'Submit Feedback'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
