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
  Scheduled: 'bg-info/10 text-info',
  InProgress: 'bg-success/10 text-success',
  Completed: 'bg-muted text-muted-foreground',
  Cancelled: 'bg-destructive/10 text-destructive',
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
                        }} aria-label="Edit meeting"><Pencil className="h-4 w-4" /></Button>
                        <Button variant="ghost" size="icon" onClick={() => { if (confirm('Delete this meeting?')) deleteMutation.mutate(m.id); }} aria-label="Delete meeting">
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
            <div className="space-y-2"><Label htmlFor="title">Title *</Label><Input id="title" {...register('title')} placeholder="Weekly sync" aria-invalid={!!errors.title} aria-describedby={errors.title ? 'title-error' : undefined} />{errors.title && <p id="title-error" role="alert" className="text-sm text-destructive mt-1">{errors.title.message}</p>}</div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="date">Date *</Label><Input id="date" type="date" {...register('date')} aria-invalid={!!errors.date} aria-describedby={errors.date ? 'date-error' : undefined} />{errors.date && <p id="date-error" role="alert" className="text-sm text-destructive mt-1">{errors.date.message}</p>}</div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="startTime">Start Time *</Label><Input id="startTime" type="time" {...register('startTime')} aria-invalid={!!errors.startTime} aria-describedby={errors.startTime ? 'startTime-error' : undefined} />{errors.startTime && <p id="startTime-error" role="alert" className="text-sm text-destructive mt-1">{errors.startTime.message}</p>}</div>
              <div className="space-y-2"><Label htmlFor="endTime">End Time *</Label><Input id="endTime" type="time" {...register('endTime')} aria-invalid={!!errors.endTime} aria-describedby={errors.endTime ? 'endTime-error' : undefined} />{errors.endTime && <p id="endTime-error" role="alert" className="text-sm text-destructive mt-1">{errors.endTime.message}</p>}</div>
            </div>
            <div className="space-y-2"><Label htmlFor="location">Location</Label><Input id="location" {...register('location')} placeholder="Room 301" /></div>
            <div className="space-y-2"><Label htmlFor="meetingLink">Meeting Link</Label><Input id="meetingLink" {...register('meetingLink')} placeholder="https://..." /></div>
            <div className="space-y-2"><Label htmlFor="description">Description</Label><Input id="description" {...register('description')} placeholder="Agenda..." /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Create Meeting</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editingMeeting} onOpenChange={() => setEditingMeeting(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Edit Meeting</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="title-edit">Title *</Label><Input id="title-edit" {...register('title')} aria-invalid={!!errors.title} aria-describedby={errors.title ? 'title-edit-error' : undefined} />{errors.title && <p id="title-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.title.message}</p>}</div>
            <div className="space-y-2"><Label htmlFor="date-edit">Date *</Label><Input id="date-edit" type="date" {...register('date')} aria-invalid={!!errors.date} aria-describedby={errors.date ? 'date-edit-error' : undefined} />{errors.date && <p id="date-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.date.message}</p>}</div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="startTime-edit">Start Time *</Label><Input id="startTime-edit" type="time" {...register('startTime')} aria-invalid={!!errors.startTime} aria-describedby={errors.startTime ? 'startTime-edit-error' : undefined} />{errors.startTime && <p id="startTime-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.startTime.message}</p>}</div>
              <div className="space-y-2"><Label htmlFor="endTime-edit">End Time *</Label><Input id="endTime-edit" type="time" {...register('endTime')} aria-invalid={!!errors.endTime} aria-describedby={errors.endTime ? 'endTime-edit-error' : undefined} />{errors.endTime && <p id="endTime-edit-error" role="alert" className="text-sm text-destructive mt-1">{errors.endTime.message}</p>}</div>
            </div>
            <div className="space-y-2"><Label htmlFor="location-edit">Location</Label><Input id="location-edit" {...register('location')} /></div>
            <div className="space-y-2"><Label htmlFor="meetingLink-edit">Meeting Link</Label><Input id="meetingLink-edit" {...register('meetingLink')} /></div>
            <div className="space-y-2"><Label htmlFor="description-edit">Description</Label><Input id="description-edit" {...register('description')} /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Save Changes</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
