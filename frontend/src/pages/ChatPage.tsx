import { useState, useRef, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Send, Plus, MessageSquare, Trash2, ThumbsUp, ThumbsDown, AlertCircle, Square, ArrowUpCircle } from 'lucide-react';

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

interface AIResponseMeta {
  id: string;
  modelUsed: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  latencyMs: number;
}

export function ChatPage() {
  const queryClient = useQueryClient();
  const token = useAuthStore(s => s.accessToken);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [streamingContent, setStreamingContent] = useState('');
  const [streamingMeta, setStreamingMeta] = useState<AIResponseMeta | null>(null);
  const abortRef = useRef<AbortController | null>(null);
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

  const handleSend = useCallback(async () => {
    if (!input.trim() || isStreaming) return;

    const message = input;
    setInput('');
    setIsStreaming(true);
    setStreamingContent('');
    setStreamingMeta(null);

    const controller = new AbortController();
    abortRef.current = controller;

    try {
      const baseUrl = api.defaults.baseURL || '';
      const res = await fetch(`${baseUrl}/ai/chat/stream`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({
          message,
          sessionId: activeSessionId || undefined,
        }),
        signal: controller.signal,
      });

      if (!res.ok) {
        const errText = await res.text();
        throw new Error(errText || `HTTP ${res.status}`);
      }

      const reader = res.body?.getReader();
      if (!reader) throw new Error('No response body');

      const decoder = new TextDecoder();
      let buffer = '';
      let finalSessionId = activeSessionId;

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (!line.startsWith('data: ')) continue;
          const data = line.substring(6);
          if (data === '[DONE]') break;
          if (data === '[CANCELLED]') break;

          try {
            const parsed = JSON.parse(data);
            switch (parsed.type) {
              case 'token':
                setStreamingContent(prev => prev + parsed.content);
                break;
              case 'metadata':
                setStreamingMeta(parsed.metadata);
                break;
              case 'complete':
                finalSessionId = parsed.session?.id;
                break;
            }
          } catch {
            // skip malformed SSE lines
          }
        }
      }

      if (finalSessionId) {
        setActiveSessionId(finalSessionId);
        queryClient.invalidateQueries({ queryKey: ['chat-sessions'] });
        queryClient.invalidateQueries({ queryKey: ['chat-session', finalSessionId] });
      }
    } catch (err: unknown) {
      if (err instanceof Error && err.name === 'AbortError') {
        // User cancelled — keep streaming content as partial
      } else {
        console.error('Chat stream error:', err);
      }
    } finally {
      setIsStreaming(false);
      abortRef.current = null;
    }
  }, [input, isStreaming, activeSessionId, token, queryClient]);

  const handleCancel = () => {
    abortRef.current?.abort();
  };

  const handleEscalate = useMutation({
    mutationFn: async () => {
      if (!activeSessionId) return;
      return api.post(`/ai/conversations/${activeSessionId}/escalate`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] });
      queryClient.invalidateQueries({ queryKey: ['chat-session', activeSessionId] });
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

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); }
  };

  const messages = sessionDetail?.messages ?? [];
  const currentStatus = sessionDetail?.status;
  const isEscalated = currentStatus === 'Escalated';

  return (
    <div className="flex h-[calc(100vh-8rem)] gap-4">
      {/* Sidebar */}
      <div className="w-72 shrink-0 flex flex-col border rounded-lg bg-card">
        <div className="p-3 border-b flex items-center justify-between">
          <h2 className="font-semibold text-sm">Conversations</h2>
          <Button variant="ghost" size="icon" onClick={() => { setActiveSessionId(null); setInput(''); setStreamingContent(''); setStreamingMeta(null); }} aria-label="New chat">
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
                {s.status === 'Escalated' && (
                  <Badge variant="outline" className="text-[10px] px-1 py-0 text-warning border-warning/40">Escalated</Badge>
                )}
              </div>
              <Button variant="ghost" size="icon" className="h-6 w-6 opacity-50 hover:opacity-100"
                onClick={(e) => { e.stopPropagation(); if (confirm('Delete?')) deleteMutation.mutate(s.id); }} aria-label="Delete conversation">
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
                          onClick={() => feedbackMutation.mutate({ messageId: m.id, score: 1 })} aria-label="Helpful">
                          <ThumbsUp className="h-3 w-3" />
                        </Button>
                        <Button variant="ghost" size="icon" className="h-6 w-6"
                          onClick={() => feedbackMutation.mutate({ messageId: m.id, score: 0 })} aria-label="Not helpful">
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
              {streamingMeta && !isStreaming && (
                <div className="flex justify-start">
                  <div className="text-xs text-muted-foreground px-4">
                    ⚡ {streamingMeta.totalTokens} tokens · {(streamingMeta.latencyMs / 1000).toFixed(1)}s · {streamingMeta.modelUsed}
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            {/* Input + Actions */}
            <div className="p-4 border-t space-y-2">
              {activeSessionId && !isEscalated && (
                <div className="flex justify-end">
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-warning hover:text-warning hover:bg-warning/5 text-xs"
                    onClick={() => { if (confirm('Escalate this conversation to a human agent?')) handleEscalate.mutate(); }}
                    disabled={handleEscalate.isPending}
                  >
                    <ArrowUpCircle className="h-3.5 w-3.5 mr-1" />
                    Escalate to Human
                  </Button>
                </div>
              )}
              {isEscalated && (
                <div className="flex items-center gap-2 text-sm text-warning bg-warning/5 rounded-md px-3 py-2">
                  <AlertCircle className="h-4 w-4" />
                  This conversation has been escalated. A human agent will follow up.
                </div>
              )}
              <div className="flex gap-2">
                <Input
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder={isEscalated ? "Conversation escalated — start a new chat" : "Ask a question... (Enter to send, Shift+Enter for newline)"}
                  disabled={isStreaming || isEscalated}
                  className="flex-1"
                />
                {isStreaming ? (
                  <Button onClick={handleCancel} variant="destructive">
                    <Square className="h-4 w-4" />
                  </Button>
                ) : (
                  <Button onClick={handleSend} disabled={!input.trim() || isEscalated}>
                    <Send className="h-4 w-4" />
                  </Button>
                )}
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
