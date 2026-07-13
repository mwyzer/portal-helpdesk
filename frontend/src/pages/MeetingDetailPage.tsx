import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ArrowLeft, Plus, Trash2, CheckCircle2, Pencil, Clock, MapPin, Users as UsersIcon, FileText, Calendar, Sparkles, Loader2, Search } from 'lucide-react';
import { AISummaryButton } from '@/components/ai/AISummaryButton';
import { ParticipantSelector } from '@/components/domain/ParticipantSelector';

// ── Types ──

interface MeetingDetail {
  id: string;
  title: string;
  date: string;
  startTime: string;
  endTime: string;
  location: string | null;
  meetingLink: string | null;
  description: string | null;
  status: string;
  notes: string | null;
  transcriptUrl: string | null;
  organizerId: string;
  organizerName: string;
  participants: Participant[];
  meetingNotes: MeetingNote[];
  actionItems: ActionItem[];
  createdAt: string;
  updatedAt: string;
}

interface Participant {
  id: string;
  employeeId: string;
  employeeName: string;
  role: string;
  isRequired: boolean;
  attendanceStatus: string;
}

interface MeetingNote {
  id: string;
  title: string;
  content: string;
  createdBy: string;
  createdByName: string;
  isAISummary: boolean;
  createdAt: string;
}

interface ActionItem {
  id: string;
  meetingId: string | null;
  meetingTitle: string | null;
  title: string;
  description: string | null;
  assignedToId: string;
  assignedToName: string;
  dueDate: string;
  priority: string;
  status: string;
  completedAt: string | null;
  createdAt: string;
}

interface EmployeeOption {
  id: string;
  fullName: string;
}

// ── Schemas ──

const noteSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  content: z.string().min(1, 'Content is required'),
});

const participantSchema = z.object({
  employeeId: z.string().min(1, 'Please select an employee'),
  role: z.string().min(1, 'Please select a role'),
  isRequired: z.boolean(),
});

const actionItemSchema = z.object({
  title: z.string().min(3, 'Min 3 characters'),
  description: z.string().optional(),
  assignedToId: z.string().min(1, 'Please select an assignee'),
  dueDate: z.string().min(1, 'Due date is required'),
  priority: z.string().min(1, 'Please select priority'),
});

// ── Helpers ──

function formatTime(t: string) {
  if (!t) return '';
  const [h, m] = t.split(':');
  return `${h}:${m}`;
}

const statusColors: Record<string, string> = {
  Scheduled: 'bg-info/10 text-info',
  InProgress: 'bg-success/10 text-success',
  Completed: 'bg-muted text-muted-foreground',
  Cancelled: 'bg-destructive/10 text-destructive',
};

const priorityColors: Record<string, string> = {
  Low: 'bg-muted text-muted-foreground',
  Medium: 'bg-info/10 text-info',
  High: 'bg-warning/10 text-warning',
  Urgent: 'bg-destructive/10 text-destructive',
};

const actionItemStatusColors: Record<string, string> = {
  Open: 'bg-warning/10 text-warning',
  InProgress: 'bg-info/10 text-info',
  Completed: 'bg-success/10 text-success',
  Cancelled: 'bg-muted text-muted-foreground',
};

const participantRoleColors: Record<string, string> = {
  Organizer: 'bg-primary/10 text-primary',
  Presenter: 'bg-info/10 text-info',
  Attendee: 'bg-muted text-muted-foreground',
};

const attendanceColors: Record<string, string> = {
  Pending: 'bg-warning/10 text-warning',
  Accepted: 'bg-success/10 text-success',
  Declined: 'bg-destructive/10 text-destructive',
  Attended: 'bg-success/10 text-success',
  Absent: 'bg-destructive/10 text-destructive',
};

// ═══════════════════════════ MAIN COMPONENT ═══════════════════════════

export function MeetingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [showAddNote, setShowAddNote] = useState(false);
  const [editingNote, setEditingNote] = useState<MeetingNote | null>(null);
  const [showAddParticipant, setShowAddParticipant] = useState(false);
  const [showAddActionItem, setShowAddActionItem] = useState(false);
  const [editingActionItem, setEditingActionItem] = useState<ActionItem | null>(null);

  // ── Queries ──

  const { data: meeting, isLoading } = useQuery<MeetingDetail>({
    queryKey: ['meeting', id],
    queryFn: () => api.get(`/meetings/${id}`).then((r) => r.data),
    enabled: !!id,
  });

  const { data: employees } = useQuery<{ items: EmployeeOption[] }>({
    queryKey: ['employees-dropdown'],
    queryFn: () => api.get('/employees', { params: { pageSize: 200 } }).then((r) => r.data),
  });

  const inv = () => queryClient.invalidateQueries({ queryKey: ['meeting', id] });

  // ── Note Forms ──

  const noteForm = useForm<z.infer<typeof noteSchema>>({ resolver: zodResolver(noteSchema) });

  const addNoteMutation = useMutation({
    mutationFn: (data: z.infer<typeof noteSchema>) => api.post(`/meetings/${id}/notes`, data),
    onSuccess: () => { setShowAddNote(false); noteForm.reset(); inv(); },
  });

  const updateNoteMutation = useMutation({
    mutationFn: ({ noteId, data }: { noteId: string; data: z.infer<typeof noteSchema> }) =>
      api.put(`/meetings/${id}/notes/${noteId}`, data),
    onSuccess: () => { setEditingNote(null); noteForm.reset(); inv(); },
  });

  const deleteNoteMutation = useMutation({
    mutationFn: (noteId: string) => api.delete(`/meetings/${id}/notes/${noteId}`),
    onSuccess: () => inv(),
  });

  // ── Participant Form ──

  const [participantData, setParticipantData] = useState({ employeeId: '', role: 'Attendee', isRequired: false });

  const addParticipantMutation = useMutation({
    mutationFn: (data: typeof participantData) => api.post(`/meetings/${id}/participants`, data),
    onSuccess: () => { setShowAddParticipant(false); setParticipantData({ employeeId: '', role: 'Attendee', isRequired: false }); inv(); },
  });

  const removeParticipantMutation = useMutation({
    mutationFn: (participantId: string) => api.delete(`/meetings/${id}/participants/${participantId}`),
    onSuccess: () => inv(),
  });

  // ── Action Item Forms ──

  const actionItemForm = useForm<z.infer<typeof actionItemSchema>>({ resolver: zodResolver(actionItemSchema) });

  const addActionItemMutation = useMutation({
    mutationFn: (data: z.infer<typeof actionItemSchema>) =>
      api.post('/action-items', { ...data, meetingId: id }),
    onSuccess: () => { setShowAddActionItem(false); actionItemForm.reset(); inv(); },
  });

  const updateActionItemMutation = useMutation({
    mutationFn: ({ itemId, data }: { itemId: string; data: z.infer<typeof actionItemSchema> }) =>
      api.put(`/action-items/${itemId}`, data),
    onSuccess: () => { setEditingActionItem(null); actionItemForm.reset(); inv(); },
  });

  const completeActionItemMutation = useMutation({
    mutationFn: (itemId: string) => api.post(`/action-items/${itemId}/complete`),
    onSuccess: () => inv(),
  });

  // ── Helpers ──

  const openEditNote = (note: MeetingNote) => {
    noteForm.reset({ title: note.title, content: note.content });
    setEditingNote(note);
  };

  const openEditActionItem = (item: ActionItem) => {
    actionItemForm.reset({
      title: item.title,
      description: item.description || '',
      assignedToId: item.assignedToId,
      dueDate: item.dueDate.split('T')[0],
      priority: item.priority,
    });
    setEditingActionItem(item);
  };

  // ── Render ──

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner className="h-10 w-10" />
      </div>
    );
  }

  if (!meeting) {
    return (
      <div className="text-center py-16">
        <p className="text-muted-foreground">Meeting not found.</p>
        <Button variant="link" onClick={() => navigate('/meetings')}>Back to Meetings</Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-4">
          <Button variant="ghost" size="icon" onClick={() => navigate('/meetings')} className="mt-1" aria-label="Back to meetings">
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold tracking-tight">{meeting.title}</h1>
            <div className="flex flex-wrap items-center gap-3 mt-2 text-sm text-muted-foreground">
              <span className="flex items-center gap-1"><Calendar className="h-4 w-4" /> {new Date(meeting.date).toLocaleDateString()}</span>
              <span className="flex items-center gap-1"><Clock className="h-4 w-4" /> {formatTime(meeting.startTime)} - {formatTime(meeting.endTime)}</span>
              {meeting.location && <span className="flex items-center gap-1"><MapPin className="h-4 w-4" /> {meeting.location}</span>}
              <Badge className={statusColors[meeting.status] || ''}>{meeting.status}</Badge>
            </div>
            <p className="text-sm text-muted-foreground mt-1">Organized by {meeting.organizerName}</p>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="info" className="w-full">
        <TabsList>
          <TabsTrigger value="info">Info</TabsTrigger>
          <TabsTrigger value="participants">
            Participants ({meeting.participants?.length || 0})
          </TabsTrigger>
          <TabsTrigger value="notes">
            Notes ({meeting.meetingNotes?.length || 0})
          </TabsTrigger>
          <TabsTrigger value="action-items">
            Action Items ({meeting.actionItems?.length || 0})
          </TabsTrigger>
        </TabsList>

        {/* ─── Tab: Info ─── */}
        <TabsContent value="info" className="space-y-4 pt-4">
          <Card>
            <CardHeader><CardTitle>Meeting Details</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              {meeting.description && (
                <div>
                  <Label className="text-xs text-muted-foreground">Description</Label>
                  <p className="text-sm mt-1 whitespace-pre-wrap">{meeting.description}</p>
                </div>
              )}
              {meeting.meetingLink && (
                <div>
                  <Label className="text-xs text-muted-foreground">Meeting Link</Label>
                  <p className="text-sm mt-1">
                    <a href={meeting.meetingLink} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
                      {meeting.meetingLink}
                    </a>
                  </p>
                </div>
              )}
              {meeting.notes && (
                <div>
                  <Label className="text-xs text-muted-foreground">General Notes</Label>
                  <p className="text-sm mt-1 whitespace-pre-wrap">{meeting.notes}</p>
                </div>
              )}
              <div className="grid grid-cols-2 gap-4 pt-2">
                <div>
                  <Label className="text-xs text-muted-foreground">Created</Label>
                  <p className="text-sm">{new Date(meeting.createdAt).toLocaleString()}</p>
                </div>
                <div>
                  <Label className="text-xs text-muted-foreground">Last Updated</Label>
                  <p className="text-sm">{new Date(meeting.updatedAt).toLocaleString()}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        {/* ─── Tab: Participants ─── */}
        <TabsContent value="participants" className="space-y-4 pt-4">
          <div className="flex justify-end">
            <Button size="sm" onClick={() => setShowAddParticipant(true)}>
              <Plus className="mr-1 h-4 w-4" /> Add Participant
            </Button>
          </div>
          <Card>
            <CardContent className="pt-4">
              {meeting.participants?.length === 0 ? (
                <p className="text-center py-8 text-muted-foreground">No participants added yet.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Name</TableHead>
                      <TableHead>Role</TableHead>
                      <TableHead>Required</TableHead>
                      <TableHead>Attendance</TableHead>
                      <TableHead className="w-16"></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {meeting.participants?.map((p) => (
                      <TableRow key={p.id}>
                        <TableCell className="font-medium">{p.employeeName}</TableCell>
                        <TableCell><Badge className={participantRoleColors[p.role] || ''}>{p.role}</Badge></TableCell>
                        <TableCell>{p.isRequired ? 'Yes' : 'No'}</TableCell>
                        <TableCell><Badge className={attendanceColors[p.attendanceStatus] || ''}>{p.attendanceStatus}</Badge></TableCell>
                        <TableCell>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => { if (confirm('Remove this participant?')) removeParticipantMutation.mutate(p.id); }}
                            aria-label="Remove participant"
                          >
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* ─── Tab: Notes ─── */}
        <TabsContent value="notes" className="space-y-4 pt-4">
          <div className="flex items-center justify-between">
            <AISummaryButton
              meetingId={id!}
              notesCount={meeting.meetingNotes?.length ?? 0}
            />
            <Button size="sm" onClick={() => { noteForm.reset({ title: '', content: '' }); setShowAddNote(true); }}>
              <Plus className="mr-1 h-4 w-4" /> Add Note
            </Button>
          </div>
          <div className="space-y-3">
            {meeting.meetingNotes?.length === 0 ? (
              <Card><CardContent className="py-8 text-center text-muted-foreground">No notes yet.</CardContent></Card>
            ) : (
              meeting.meetingNotes?.map((note) => (
                <Card key={note.id}>
                  <CardHeader className="pb-2 flex-row items-start justify-between">
                    <div>
                      <CardTitle className="text-base flex items-center gap-2">
                        {note.title}
                        {note.isAISummary && <Badge className="bg-primary/10 text-primary text-xs">AI</Badge>}
                      </CardTitle>
                      <p className="text-xs text-muted-foreground">
                        By {note.createdByName} &middot; {new Date(note.createdAt).toLocaleString()}
                      </p>
                    </div>
                    <div className="flex gap-1">
                      <Button variant="ghost" size="icon" onClick={() => openEditNote(note)} aria-label="Edit note">
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => { if (confirm('Delete this note?')) deleteNoteMutation.mutate(note.id); }}
                        aria-label="Delete note"
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm whitespace-pre-wrap">{note.content}</p>
                  </CardContent>
                </Card>
              ))
            )}
          </div>
        </TabsContent>

        {/* ─── Tab: Action Items ─── */}
        <TabsContent value="action-items" className="space-y-4 pt-4">
          <div className="flex justify-end">
            <Button size="sm" onClick={() => { actionItemForm.reset({ title: '', description: '', assignedToId: '', dueDate: '', priority: 'Medium' }); setShowAddActionItem(true); }}>
              <Plus className="mr-1 h-4 w-4" /> Add Action Item
            </Button>
          </div>
          <Card>
            <CardContent className="pt-4">
              {meeting.actionItems?.length === 0 ? (
                <p className="text-center py-8 text-muted-foreground">No action items for this meeting.</p>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Task</TableHead>
                      <TableHead>Assigned To</TableHead>
                      <TableHead>Due Date</TableHead>
                      <TableHead>Priority</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead className="w-20"></TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {meeting.actionItems?.map((a) => (
                      <TableRow key={a.id} className={a.status === 'Completed' ? 'opacity-60' : ''}>
                        <TableCell>
                          <div className="font-medium">{a.title}</div>
                          {a.description && <div className="text-xs text-muted-foreground">{a.description}</div>}
                        </TableCell>
                        <TableCell className="text-sm">{a.assignedToName}</TableCell>
                        <TableCell className="text-sm">
                          <span className={new Date(a.dueDate) < new Date() && a.status !== 'Completed' && a.status !== 'Cancelled' ? 'text-destructive font-medium' : ''}>
                            {new Date(a.dueDate).toLocaleDateString()}
                          </span>
                        </TableCell>
                        <TableCell><Badge className={priorityColors[a.priority] || ''}>{a.priority}</Badge></TableCell>
                        <TableCell><Badge className={actionItemStatusColors[a.status] || ''}>{a.status}</Badge></TableCell>
                        <TableCell>
                          <div className="flex gap-0.5">
                            {(a.status === 'Open' || a.status === 'InProgress') && (
                              <>
                                <Button variant="ghost" size="icon" onClick={() => openEditActionItem(a)} title="Edit" aria-label="Edit action item">
                                  <Pencil className="h-3.5 w-3.5" />
                                </Button>
                                <Button variant="ghost" size="icon" onClick={() => completeActionItemMutation.mutate(a.id)} title="Complete" aria-label="Complete action item">
                                  <CheckCircle2 className="h-3.5 w-3.5 text-success" />
                                </Button>
                              </>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* ── Add Note Dialog ── */}
      <Dialog open={showAddNote} onOpenChange={setShowAddNote}>
        <DialogContent>
          <DialogHeader><DialogTitle>Add Note</DialogTitle></DialogHeader>
          <form onSubmit={noteForm.handleSubmit((d) => addNoteMutation.mutate(d))} className="space-y-4">
            <div className="space-y-2"><Label>Title *</Label><Input {...noteForm.register('title')} placeholder="Meeting summary" /></div>
            <div className="space-y-2"><Label>Content *</Label><Textarea {...noteForm.register('content')} placeholder="Notes content..." rows={6} /></div>
            <DialogFooter><Button type="submit">Save Note</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Edit Note Dialog ── */}
      <Dialog open={!!editingNote} onOpenChange={() => setEditingNote(null)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Edit Note</DialogTitle></DialogHeader>
          <form onSubmit={noteForm.handleSubmit((d) => editingNote && updateNoteMutation.mutate({ noteId: editingNote.id, data: d }))} className="space-y-4">
            <div className="space-y-2"><Label>Title *</Label><Input {...noteForm.register('title')} /></div>
            <div className="space-y-2"><Label>Content *</Label><Textarea {...noteForm.register('content')} rows={6} /></div>
            <DialogFooter><Button type="submit">Save Changes</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Add Participant Dialog ── */}
      <Dialog open={showAddParticipant} onOpenChange={setShowAddParticipant}>
        <DialogContent>
          <DialogHeader><DialogTitle>Add Participant</DialogTitle></DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Employee *</Label>
              <ParticipantSelector
                selectedIds={participantData.employeeId ? [participantData.employeeId] : []}
                onChange={(ids) => setParticipantData((p) => ({ ...p, employeeId: ids[ids.length - 1] || '' }))}
                excludeIds={meeting?.participants?.map((p) => p.employeeId) ?? []}
                placeholder="Search and select an employee..."
              />
            </div>
            <div className="space-y-2">
              <Label>Role *</Label>
              <Select value={participantData.role} onValueChange={(v) => setParticipantData((p) => ({ ...p, role: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Organizer">Organizer</SelectItem>
                  <SelectItem value="Presenter">Presenter</SelectItem>
                  <SelectItem value="Attendee">Attendee</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="isRequired"
                checked={participantData.isRequired}
                onChange={(e) => setParticipantData((p) => ({ ...p, isRequired: e.target.checked }))}
              />
              <Label htmlFor="isRequired">Required attendee</Label>
            </div>
            <DialogFooter>
              <Button onClick={() => addParticipantMutation.mutate(participantData)} disabled={!participantData.employeeId}>
                Add Participant
              </Button>
            </DialogFooter>
          </div>
        </DialogContent>
      </Dialog>

      {/* ── Add Action Item Dialog ── */}
      <Dialog open={showAddActionItem} onOpenChange={setShowAddActionItem}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Add Action Item</DialogTitle></DialogHeader>
          <form onSubmit={actionItemForm.handleSubmit((d) => addActionItemMutation.mutate(d))} className="space-y-4">
            <div className="space-y-2"><Label>Title *</Label><Input {...actionItemForm.register('title')} placeholder="Follow up on..." /></div>
            <div className="space-y-2"><Label>Description</Label><Textarea {...actionItemForm.register('description')} rows={3} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Assigned To *</Label>
                <Select onValueChange={(v: string) => actionItemForm.setValue('assignedToId', v)}>
                  <SelectTrigger><SelectValue placeholder="Select" /></SelectTrigger>
                  <SelectContent>
                    {employees?.items?.map((e) => (
                      <SelectItem key={e.id} value={e.id}>{e.fullName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Priority *</Label>
                <Select onValueChange={(v: string) => actionItemForm.setValue('priority', v)} defaultValue="Medium">
                  <SelectTrigger><SelectValue placeholder="Select" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Medium">Medium</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Urgent">Urgent</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2"><Label>Due Date *</Label><Input type="date" {...actionItemForm.register('dueDate')} /></div>
            <DialogFooter><Button type="submit">Create Action Item</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* ── Edit Action Item Dialog ── */}
      <Dialog open={!!editingActionItem} onOpenChange={() => setEditingActionItem(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Edit Action Item</DialogTitle></DialogHeader>
          <form
            onSubmit={actionItemForm.handleSubmit((d) =>
              editingActionItem && updateActionItemMutation.mutate({ itemId: editingActionItem.id, data: d })
            )}
            className="space-y-4"
          >
            <div className="space-y-2"><Label>Title *</Label><Input {...actionItemForm.register('title')} /></div>
            <div className="space-y-2"><Label>Description</Label><Textarea {...actionItemForm.register('description')} rows={3} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Assigned To *</Label>
                <Select value={editingActionItem?.assignedToId} onValueChange={(v: string) => actionItemForm.setValue('assignedToId', v)}>
                  <SelectTrigger><SelectValue placeholder="Select" /></SelectTrigger>
                  <SelectContent>
                    {employees?.items?.map((e) => (
                      <SelectItem key={e.id} value={e.id}>{e.fullName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Priority *</Label>
                <Select value={editingActionItem?.priority} onValueChange={(v: string) => actionItemForm.setValue('priority', v)}>
                  <SelectTrigger><SelectValue placeholder="Select" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Low">Low</SelectItem>
                    <SelectItem value="Medium">Medium</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Urgent">Urgent</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2"><Label>Due Date *</Label><Input type="date" {...actionItemForm.register('dueDate')} /></div>
            <DialogFooter><Button type="submit">Save Changes</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
