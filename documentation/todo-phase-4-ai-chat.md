# Phase 4 — AI Helpdesk Chat & Knowledge Base — TODO Checklist

## Infrastructure

- [x] Add pgvector extension to PostgreSQL (Docker Compose + migration)
- [x] Set pgvector image in postgres container (pgvector/pgvector:pg17)
- [x] Configure AI provider options (OpenAI/Azure OpenAI) — appsettings.json
- [ ] Add AI provider connection string to GitHub secrets

## Backend — AI Service Core

- [x] Create `IAIService` abstraction / interface
- [x] Create OpenAI implementation
- [ ] Create Azure OpenAI implementation
- [ ] Create `AIOptions` configuration class
- [x] Implement embedding generation (`text-embedding-3-small`)
- [x] Implement chat completion with streaming (SSE)
- [ ] Implement rate limiting middleware for AI endpoints
- [ ] Create health check for AI provider

## Backend — RAG Pipeline

- [x] Create RAG pipeline (embed → search → retrieve → context → generate)
- [x] Create vector search (pgvector similarity queries via raw SQL)
- [x] Implement chunking strategy (500 char with 100 overlap)
- [x] Create document text extraction service (PDF — basic)
- [x] Create document text extraction service (DOCX — basic)
- [x] Create document text extraction service (TXT)
- [x] Implement context building from retrieved chunks

## Backend — Chat Module

- [x] Create `ChatSession` entity
- [x] Create `ChatMessage` entity
- [x] Create `AIResponse` entity
- [x] Create `AIChatController` + service
- [x] Implement POST `/api/ai/chat`
- [x] Implement GET `/api/ai/conversations` (list user sessions)
- [x] Implement GET `/api/ai/conversations/{id}` (session detail with messages)
- [x] Implement DELETE `/api/ai/conversations/{id}`
- [x] Implement PUT `/api/ai/conversations/{id}` (rename title)
- [x] Implement POST `/api/ai/responses/{id}/feedback`
- [x] Implement POST `/api/ai/conversations/{id}/escalate`
- [x] Implement conversation history persistence

## Backend — Knowledge Base

- [x] Create `KnowledgeDocument` entity
- [x] Create `KnowledgeChunk` entity (with embedding storage)
- [x] Create `KnowledgeBaseController` + service
- [x] Implement file upload with validation (type, size)
- [x] Implement document listing with status filtering
- [x] Implement document detail
- [x] Implement document deletion (cascade chunks)
- [x] Implement POST `/api/knowledge-documents/{id}/index`
- [x] Implement POST `/api/knowledge-documents/search`
- [x] Create background indexing (Task.Run fire-and-forget)
- [x] Implement document status management (Pending → Indexing → Ready → Failed)

## Backend — AI Guardrails

- [x] Create guardrails (system prompt: "answer based only on context")
- [ ] Implement permission-aware context filtering
- [ ] Implement PII stripping
- [x] Implement system prompt management
- [x] Implement no-context fallback response
- [ ] Implement prompt/response audit logging
- [ ] Implement rate limiting (30 req/min/user)

## Backend — Audit & Monitoring

- [x] Create `AIUsageLog` entity
- [x] Create usage tracking (auto-logged on each AI request)
- [x] Implement token counting and cost estimation
- [ ] Create admin usage stats endpoint
- [ ] Set daily token budget check

## Database

- [x] Create migration for pgvector extension
- [x] Create migration for KnowledgeDocuments, KnowledgeChunks
- [x] Create migration for ChatSessions, ChatMessages
- [x] Create migration for AIResponses, AIUsageLog

## Frontend — AI Chat

- [x] Create ChatPage (main chat interface)
- [x] Create ChatWindow component (message bubbles, timestamps)
- [x] Create ChatInput component (text input, send button)
- [x] Create MessageBubble component (user/AI messages)
- [x] Create SourceCard component (source citations with relevance %)
- [x] Create AITypingIndicator component (animated dots)
- [ ] Implement streaming SSE response rendering (token-by-token)
- [x] Create FeedbackButtons component (thumbs up/down)
- [ ] Create EscalateButton component
- [x] Create ConversationHistory component (sidebar)
- [x] Implement Enter to send, Ctrl+Enter for newline
- [ ] Implement abort/cancel in-progress request
- [ ] Create ChatSessionPage (existing session)
- [ ] Create ConversationListPage

## Frontend — Knowledge Base

- [x] Create KnowledgeBaseListPage (documents with status badges)
- [x] Create KnowledgeUploadForm (file upload, validation, progress)
- [x] Create KnowledgeUploadPage (in dialog)
- [ ] Create KnowledgeDocumentDetailPage
- [x] Create KnowledgeSearchBar (text search)
- [ ] Create DocumentPreview component (inline PDF/TXT preview)
- [ ] Create UsageStats component (admin token usage display)

## Backend Tests

- [ ] Unit: RAG pipeline (embed → search → context → response)
- [ ] Unit: Document text extraction (PDF)
- [ ] Unit: Document text extraction (DOCX)
- [ ] Unit: Document text extraction (TXT)
- [ ] Unit: Chunking strategy (token boundary, overlap)
- [ ] Unit: AI guardrails (permission checks)
- [ ] Unit: AI guardrails (PII stripping)
- [ ] Unit: Rate limiting
- [ ] Unit: Token counting
- [ ] Integration: Upload → index → search → chat flow
- [ ] Integration: Chat session CRUD with messages
- [ ] Integration: SSE streaming
- [ ] Integration: Feedback submission

## Frontend Tests

- [ ] Chat input submit
- [ ] Streaming response rendering
- [ ] Source attribution display
- [ ] Feedback submission
- [ ] Document upload validation (file type, size)
- [ ] Knowledge search with results
- [ ] Conversation history navigation
- [ ] Escalate button flow
