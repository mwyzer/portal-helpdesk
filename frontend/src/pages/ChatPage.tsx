import { useState, useRef, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Send, Plus, MessageSquare, Trash2, ThumbsUp, ThumbsDown, AlertCircle } from 'lucide-react';

interface ChatSession {
  id: string;
  title: string;
  status: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

interface ChatMessage {
  id: string;
  role: string;
  content: string;
  sources: string | null;
  createdAt: string;
}

interface SessionDetail {
  id: string;
  title: string;
  status: string;
  messages: ChatMessage[];
  createdAt: string;
  updatedAt: string;
}

export function ChatPage() {
  const queryClient = useQueryClient();
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [streamingContent, setStreamingContent] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const { data: sessions } = useQuery<{ items: ChatSession[] }>({
    queryKey: ['chat-sessions'],
    queryFn: () => api.get('/ai/conversations', { params: { pageSize: 50 } }).then(r => r.data),
  });

  const { data: sessionDetail, isLoading: isLoadingSession } = useQuery<SessionDetail>({
    queryKey: ['chat-session', activeSessionId],
    queryFn: () => api.get(`/ai/conversations/${activeSessionId}`).then(r => r.data),
    enabled: !!activeSessionId,
  });

  const sendMutation = useMutation({
    mutationFn: async (data: { message: string; sessionId?: string }) => {
      setIsStreaming(true);
      setStreamingContent('');
      const res = await api.post('/ai/chat', {
        message: data.message,
        sessionId: data.sessionId || undefined,
      });
      return res.data as SessionDetail;
    },
    onSuccess: (data) => {
      setActiveSessionId(data.id);
      setIsStreaming(false);
      setStreamingContent('');
      setInput('');
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] });
    },
    onError: () => {
      setIsStreaming(false);
      setStreamingContent('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/ai/conversations/${id}`),
    onSuccess: (_data, id) => {
      if (activeSessionId === id) setActiveSessionId(null);
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] });
    },
  });

  const feedbackMutation = useMutation({
    mutationFn: ({ messageId, score }: { messageId: string; score: number }) =>
      api.post(`/ai/responses/${messageId}/feedback`, { score, comment: null }),
  });

  useEffect(() => { messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [sessionDetail?.messages, streamingContent]);

  const handleSend = () => {
    if (!input.trim() || isStreaming) return;
    sendMutation.mutate({ message: input, sessionId: activeSessionId ?? undefined });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); }
  };

  const messages = sessionDetail?.messages ?? [];

  return (
    <div className="flex h-[calc(100vh-8rem)] gap-4">
      {/* Sidebar */}
      <div className="w-72 shrink-0 flex flex-col border rounded-lg bg-card">
        <div className="p-3 border-b flex items-center justify-between">
          <h2 className="font-semibold text-sm">Conversations</h2>
          <Button variant="ghost" size="icon" onClick={() => { setActiveSessionId(null); setInput(''); }}>
            <Plus className="h-4 w-4" />
          </Button>
        </div>
        <div className="flex-1 overflow-y-auto p-2 space-y-1">
          {sessions?.items?.map(s => (
            <div
              key={s.id}
              className={`flex items-center justify-between p-2 rounded-md cursor-pointer text-sm ${
                activeSessionId === s.id ? 'bg-primary/10 text-primary' : 'hover:bg-muted'
              }`}
              onClick={() => setActiveSessionId(s.id)}
            >
              <div className="flex items-center gap-2 min-w-0">
                <MessageSquare className="h-3.5 w-3.5 shrink-0" />
                <span className="truncate">{s.title}</span>
              </div>
              <Button variant="ghost" size="icon" className="h-6 w-6 opacity-50 hover:opacity-100"
                onClick={(e) => { e.stopPropagation(); if (confirm('Delete?')) deleteMutation.mutate(s.id); }}>
                <Trash2 className="h-3 w-3" />
              </Button>
            </div>
          ))}
        </div>
      </div>

      {/* Chat Area */}
      <div className="flex-1 flex flex-col border rounded-lg bg-card">
        {!activeSessionId && !isStreaming ? (
          <div className="flex-1 flex items-center justify-center text-muted-foreground">
            <div className="text-center">
              <MessageSquare className="h-16 w-16 mx-auto mb-4 opacity-20" />
              <p className="text-lg font-medium">AI Helpdesk Assistant</p>
              <p className="text-sm">Ask any question about company policies, procedures, or HR matters.</p>
            </div>
          </div>
        ) : (
          <>
            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {isLoadingSession && <div className="flex justify-center"><Spinner className="h-6 w-6" /></div>}
              {messages.map(m => (
                <div key={m.id} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                  <div className={`max-w-[75%] rounded-lg px-4 py-2.5 ${
                    m.role === 'user' ? 'bg-primary text-primary-foreground' : 'bg-muted'
                  }`}>
                    <p className="text-sm whitespace-pre-wrap">{m.content}</p>
                    {m.sources && (() => {
                      try {
                        const srcs = JSON.parse(m.sources);
                        if (srcs.length > 0) return (
                          <div className="mt-2 pt-2 border-t border-border/50">
                            <p className="text-xs font-medium mb-1 opacity-70">Sources:</p>
                            {srcs.map((s: any, i: number) => (
                              <div key={i} className="text-xs opacity-60 flex items-center gap-1">
                                <span>•</span> {s.documentTitle}
                                <Badge variant="outline" className="text-[10px] px-1 py-0">
                                  {(s.relevance * 100).toFixed(0)}%
                                </Badge>
                              </div>
                            ))}
                          </div>
                        );
                      } catch { return null; }
                    })()}
                    {m.role === 'assistant' && (
                      <div className="flex gap-1 mt-2 pt-1 border-t border-border/50">
                        <Button variant="ghost" size="icon" className="h-6 w-6"
                          onClick={() => feedbackMutation.mutate({ messageId: m.id, score: 1 })}>
                          <ThumbsUp className="h-3 w-3" />
                        </Button>
                        <Button variant="ghost" size="icon" className="h-6 w-6"
                          onClick={() => feedbackMutation.mutate({ messageId: m.id, score: 0 })}>
                          <ThumbsDown className="h-3 w-3" />
                        </Button>
                      </div>
                    )}
                  </div>
                </div>
              ))}
              {isStreaming && streamingContent && (
                <div className="flex justify-start">
                  <div className="max-w-[75%] rounded-lg px-4 py-2.5 bg-muted">
                    <p className="text-sm whitespace-pre-wrap">{streamingContent}</p>
                    <span className="inline-block w-2 h-4 bg-primary animate-pulse ml-0.5" />
                  </div>
                </div>
              )}
              {isStreaming && !streamingContent && (
                <div className="flex justify-start">
                  <div className="rounded-lg px-4 py-3 bg-muted flex gap-1">
                    <span className="w-2 h-2 rounded-full bg-foreground/30 animate-bounce" style={{ animationDelay: '0ms' }} />
                    <span className="w-2 h-2 rounded-full bg-foreground/30 animate-bounce" style={{ animationDelay: '150ms' }} />
                    <span className="w-2 h-2 rounded-full bg-foreground/30 animate-bounce" style={{ animationDelay: '300ms' }} />
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            {/* Input */}
            <div className="p-4 border-t">
              <div className="flex gap-2">
                <Input
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder="Ask a question... (Enter to send, Shift+Enter for newline)"
                  disabled={isStreaming}
                  className="flex-1"
                />
                <Button onClick={handleSend} disabled={!input.trim() || isStreaming}>
                  <Send className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
