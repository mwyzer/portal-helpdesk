import { useState, useRef, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Spinner } from '@/components/ui/spinner';
import { Send, Square, ThumbsUp, ThumbsDown, ArrowLeft, AlertCircle, ArrowUpCircle } from 'lucide-react';

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

export function ChatSessionPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const token = useAuthStore(s => s.accessToken);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [streamingContent, setStreamingContent] = useState('');
  const [streamingMeta, setStreamingMeta] = useState<AIResponseMeta | null>(null);
  const abortRef = useRef<AbortController | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const { data: sessionDetail, isLoading } = useQuery<SessionDetail>({
    queryKey: ['chat-session', sessionId],
    queryFn: () => api.get(`/ai/conversations/${sessionId}`).then(r => r.data),
    enabled: !!sessionId,
  });

  const handleSend = useCallback(async () => {
    if (!input.trim() || isStreaming || !sessionId) return;

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
        body: JSON.stringify({ message, sessionId }),
        signal: controller.signal,
      });

      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const reader = res.body?.getReader();
      if (!reader) throw new Error('No response body');

      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (!line.startsWith('data: ')) continue;
          const data = line.substring(6);
          if (data === '[DONE]' || data === '[CANCELLED]') break;

          try {
            const parsed = JSON.parse(data);
            if (parsed.type === 'token') setStreamingContent(prev => prev + parsed.content);
            if (parsed.type === 'metadata') setStreamingMeta(parsed.metadata);
          } catch { /* skip */ }
        }
      }

      queryClient.invalidateQueries({ queryKey: ['chat-session', sessionId] });
      queryClient.invalidateQueries({ queryKey: ['chat-sessions'] });
    } catch (err: unknown) {
      if (err instanceof Error && err.name !== 'AbortError') {
        console.error('Stream error:', err);
      }
    } finally {
      setIsStreaming(false);
      abortRef.current = null;
    }
  }, [input, isStreaming, sessionId, token, queryClient]);

  const escalateMutation = useMutation({
    mutationFn: async () => {
      if (!sessionId) return;
      return api.post(`/ai/conversations/${sessionId}/escalate`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['chat-session', sessionId] });
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
  const isEscalated = sessionDetail?.status === 'Escalated';

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/ai/chat')} aria-label="Back to chat">
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{sessionDetail?.title ?? 'Loading...'}</h1>
          <p className="text-sm text-muted-foreground">
            {sessionDetail?.messages.length ?? 0} messages
            {isEscalated && <Badge className="ml-2 bg-warning/10 text-warning">Escalated</Badge>}
          </p>
        </div>
      </div>

      <div className="flex flex-col border rounded-lg bg-card h-[calc(100vh-12rem)]">
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {isLoading && <div className="flex justify-center"><Spinner className="h-6 w-6" /></div>}
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
                            <Badge variant="outline" className="text-[10px] px-1 py-0">{(s.relevance * 100).toFixed(0)}%</Badge>
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

        <div className="p-4 border-t space-y-2">
          {!isEscalated && sessionId && (
            <div className="flex justify-end">
              <Button
                variant="ghost" size="sm"
                className="text-warning hover:text-warning hover:bg-warning/5 text-xs"
                onClick={() => { if (confirm('Escalate to human agent?')) escalateMutation.mutate(); }}
                disabled={escalateMutation.isPending}
              >
                <ArrowUpCircle className="h-3.5 w-3.5 mr-1" /> Escalate to Human
              </Button>
            </div>
          )}
          {isEscalated && (
            <div className="flex items-center gap-2 text-sm text-warning bg-warning/5 rounded-md px-3 py-2">
              <AlertCircle className="h-4 w-4" /> This conversation has been escalated.
            </div>
          )}
          <div className="flex gap-2">
            <Input
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={isEscalated ? "Conversation escalated" : "Continue conversation..."}
              disabled={isStreaming || isEscalated}
              className="flex-1"
            />
            {isStreaming ? (
              <Button onClick={() => abortRef.current?.abort()} variant="destructive"><Square className="h-4 w-4" /></Button>
            ) : (
              <Button onClick={handleSend} disabled={!input.trim() || isEscalated}><Send className="h-4 w-4" /></Button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
