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
import { Calendar, Clock, MapPin, Plus, RefreshCw, Pencil, Trash2, Users as UsersIcon } from 'lucide-react';
import { Link } from 'react-router-dom';

interface MeetingResponse {
  id: string;
  title: string;
  date: string;
  startTime: string;
  endTime: string;
  location: string | null;
  meetingLink: string | null;
  description: string | null;
  status: string;
  organizerName: string;
  participantCount: number;
  createdAt: string;
}

const meetingSchema = z.object({
  title: z.string().min(3, 'Min 3 characters'),
  date: z.string().min(1, 'Date is required'),
  startTime: z.string().min(1, 'Start time is required'),
  endTime: z.string().min(1, 'End time is required'),
  location: z.string().optional(),
  meetingLink: z.string().optional(),
  description: z.string().optional(),
});

type MeetingForm = z.infer<typeof meetingSchema>;

const statusColors: Record<string, string> = {
  Scheduled: 'bg-blue-100 text-blue-800',
  InProgress: 'bg-green-100 text-green-800',
  Completed: 'bg-gray-100 text-gray-800',
  Cancelled: 'bg-red-100 text-red-800',
};

function formatTime(t: string) {
  if (!t) return '';
  const [h, m] = t.split(':');
  return `${h}:${m}`;
}

export function MeetingsPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [editingMeeting, setEditingMeeting] = useState<MeetingResponse | null>(null);
  const [statusFilter, setStatusFilter] = useState('');

  const { data, isLoading } = useQuery<{ items: MeetingResponse[]; totalCount: number }>({
    queryKey: ['meetings', statusFilter],
    queryFn: () =>
      api.get('/meetings', { params: { pageSize: 50, status: statusFilter || undefined } }).then((r) => r.data),
  });

  const { register, handleSubmit, reset, formState: { isSubmitting, errors } } = useForm<MeetingForm>({
    resolver: zodResolver(meetingSchema),
  });

  const createMutation = useMutation({
    mutationFn: (data: MeetingForm) => api.post('/meetings', data),
    onSuccess: () => { setShowCreate(false); reset(); queryClient.invalidateQueries({ queryKey: ['meetings'] }); },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: MeetingForm }) => api.put(`/meetings/${id}`, data),
    onSuccess: () => { setEditingMeeting(null); reset(); queryClient.invalidateQueries({ queryKey: ['meetings'] }); },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/meetings/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['meetings'] }),
  });

  const onCreate = (formData: MeetingForm) => createMutation.mutate(formData);
  const onUpdate = (formData: MeetingForm) => {
    if (!editingMeeting) return;
    updateMutation.mutate({ id: editingMeeting.id, data: formData });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Meetings</h1>
          <p className="text-muted-foreground">Schedule and manage meetings</p>
        </div>
        <Button onClick={() => { reset(); setShowCreate(true); }}>
          <Plus className="mr-2 h-4 w-4" /> Schedule Meeting
        </Button>
      </div>

      <div className="flex items-center gap-4">
        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="All Statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Scheduled">Scheduled</SelectItem>
            <SelectItem value="InProgress">In Progress</SelectItem>
            <SelectItem value="Completed">Completed</SelectItem>
            <SelectItem value="Cancelled">Cancelled</SelectItem>
          </SelectContent>
        </Select>
        {statusFilter && (
          <Button variant="ghost" size="sm" onClick={() => setStatusFilter('')}>Clear</Button>
        )}
      </div>

      <Card>
        <CardHeader className="pb-0 flex-row items-center justify-between">
          <CardTitle>All Meetings</CardTitle>
          <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['meetings'] })}>
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
                  <TableHead>Date</TableHead>
                  <TableHead>Time</TableHead>
                  <TableHead>Location</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-24">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.items?.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell className="font-medium">
                      <Link to={`/meetings/${m.id}`} className="hover:text-primary hover:underline">{m.title}</Link>
                    </TableCell>
                    <TableCell>{new Date(m.date).toLocaleDateString()}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatTime(m.startTime)} - {formatTime(m.endTime)}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">{m.location || '—'}</TableCell>
                    <TableCell><Badge className={statusColors[m.status] || ''}>{m.status}</Badge></TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" onClick={() => {
                          reset({
                            title: m.title,
                            date: m.date.split('T')[0],
                            startTime: m.startTime,
                            endTime: m.endTime,
                            location: m.location || '',
                            meetingLink: m.meetingLink || '',
                            description: m.description || '',
                          });
                          setEditingMeeting(m);
                        }}><Pencil className="h-4 w-4" /></Button>
                        <Button variant="ghost" size="icon" onClick={() => { if (confirm('Delete this meeting?')) deleteMutation.mutate(m.id); }}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {(!data?.items || data.items.length === 0) && (
                  <TableRow><TableCell colSpan={6} className="text-center py-8 text-muted-foreground">No meetings found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Schedule Meeting</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onCreate)} className="space-y-4">
            <div className="space-y-2"><Label>Title *</Label><Input {...register('title')} placeholder="Weekly sync" /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label>Date *</Label><Input type="date" {...register('date')} /></div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label>Start Time *</Label><Input type="time" {...register('startTime')} /></div>
              <div className="space-y-2"><Label>End Time *</Label><Input type="time" {...register('endTime')} /></div>
            </div>
            <div className="space-y-2"><Label>Location</Label><Input {...register('location')} placeholder="Room 301" /></div>
            <div className="space-y-2"><Label>Meeting Link</Label><Input {...register('meetingLink')} placeholder="https://..." /></div>
            <div className="space-y-2"><Label>Description</Label><Input {...register('description')} placeholder="Agenda..." /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Create Meeting</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editingMeeting} onOpenChange={() => setEditingMeeting(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Edit Meeting</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <div className="space-y-2"><Label>Title *</Label><Input {...register('title')} /></div>
            <div className="space-y-2"><Label>Date *</Label><Input type="date" {...register('date')} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label>Start Time *</Label><Input type="time" {...register('startTime')} /></div>
              <div className="space-y-2"><Label>End Time *</Label><Input type="time" {...register('endTime')} /></div>
            </div>
            <div className="space-y-2"><Label>Location</Label><Input {...register('location')} /></div>
            <div className="space-y-2"><Label>Meeting Link</Label><Input {...register('meetingLink')} /></div>
            <div className="space-y-2"><Label>Description</Label><Input {...register('description')} /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Save Changes</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
