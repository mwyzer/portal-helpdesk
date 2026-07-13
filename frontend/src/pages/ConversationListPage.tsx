import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Search, MessageSquare, Trash2, ExternalLink, AlertCircle } from 'lucide-react';

function timeAgo(dateStr: string): string {
  const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
  if (seconds < 60) return 'just now';
  const mins = Math.floor(seconds / 60);
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 30) return `${days}d ago`;
  const months = Math.floor(days / 30);
  return `${months}mo ago`;
}

interface ChatSession {
  id: string;
  title: string;
  status: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export function ConversationListPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  const { data, isLoading } = useQuery<{ items: ChatSession[] }>({
    queryKey: ['chat-sessions'],
    queryFn: () => api.get('/ai/conversations', { params: { pageSize: 100 } }).then(r => r.data),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/ai/conversations/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['chat-sessions'] }),
  });

  const filtered = data?.items?.filter(s => {
    if (statusFilter && s.status !== statusFilter) return false;
    if (searchQuery && !s.title.toLowerCase().includes(searchQuery.toLowerCase())) return false;
    return true;
  }) ?? [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Conversations</h1>
        <p className="text-muted-foreground">Your AI chat history</p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {['', 'Active', 'Resolved', 'Escalated'].map(s => (
          <Button key={s || 'all'} variant={statusFilter === s ? 'default' : 'outline'} size="sm" onClick={() => setStatusFilter(s)}>
            {s || 'All'}
          </Button>
        ))}
        <div className="flex-1" />
        <Button variant="default" size="sm" onClick={() => navigate('/ai/chat')}>
          <MessageSquare className="h-4 w-4 mr-1" /> New Chat
        </Button>
        <div className="relative">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search conversations..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className="pl-8 w-64"
          />
        </div>
      </div>

      <Card>
        <CardHeader className="pb-0"><CardTitle>All Conversations</CardTitle></CardHeader>
        <CardContent className="pt-4">
          {isLoading ? (
            <div className="flex justify-center py-8"><Spinner className="h-8 w-8" /></div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Messages</TableHead>
                  <TableHead>Last Active</TableHead>
                  <TableHead className="w-24">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filtered.map(s => (
                  <TableRow key={s.id} className="cursor-pointer hover:bg-muted/50" onClick={() => navigate(`/ai/chat/${s.id}`)}>
                    <TableCell className="font-medium text-sm">{s.title}</TableCell>
                    <TableCell>
                      {s.status === 'Escalated' ? (
                        <Badge className="bg-warning/10 text-warning flex items-center gap-1 w-fit">
                          <AlertCircle className="h-3 w-3" /> Escalated
                        </Badge>
                      ) : (
                        <Badge variant="outline">{s.status}</Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-sm">{s.messageCount}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {timeAgo(s.updatedAt)}
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1" onClick={e => e.stopPropagation()}>
                        <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => navigate(`/ai/chat/${s.id}`)} aria-label="Open conversation">
                          <ExternalLink className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="icon" className="h-8 w-8"
                          onClick={() => { if (confirm('Delete this conversation?')) deleteMutation.mutate(s.id); }} aria-label="Delete conversation">
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {filtered.length === 0 && (
                  <TableRow><TableCell colSpan={5} className="text-center py-8 text-muted-foreground">No conversations found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
