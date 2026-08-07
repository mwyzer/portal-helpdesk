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
- [x] Create `AIOptions` configuration class
- [x] Implement embedding generation (`text-embedding-3-small`)
- [x] Implement chat completion with streaming (SSE)
- [x] Implement rate limiting middleware for AI endpoints
- [x] Create health check for AI provider

## Backend — RAG Pipeline

- [x] Create RAG pipeline (embed → search → retrieve → context → generate)
- [x] Create vector search (pgvector similarity queries) — originally raw SQL casting a `text` column to `vector` per row (no index possible); migrated 2026-08-07 to a native `KnowledgeChunk.Embedding vector(1536)` column with an HNSW cosine index (`ix_knowledgechunks_embedding_hnsw`, migration `AddChunkEmbeddingVectorAndDepartmentId`). `DepartmentId` denormalized from `KnowledgeDocument` onto `KnowledgeChunk` so the department filter runs in the same indexed query instead of via join. `SearchAsync` sets `hnsw.ef_search=100` and `hnsw.iterative_scan=relaxed_order` per query (pgvector 0.8.1, confirmed installed via `docker/postgres/Dockerfile`) so a selective department filter can't silently return fewer than `topK` rows. Legacy `EmbeddingJson` text column kept one release as a fallback/audit trail. Verified against the live `portal-helpdesk-postgres-1` container, not just build-tested.
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
- [x] Implement POST `/api/knowledge-documents/search` — fixed 2026-08-07: this standalone endpoint called `SearchAsync` without a `requesterDepartmentId`, so any authenticated user could retrieve chunks from another department's documents directly, bypassing the scoping that `ChatService`'s `/api/ai/chat` path already enforced. Now resolves the caller's `DepartmentId` from `ApplicationDbContext.Users` the same way `ChatService` does before calling `SearchAsync`
- [x] Create background indexing (Task.Run fire-and-forget)
- [x] Implement document status management (Pending → Indexing → Ready → Failed)

## Backend — AI Guardrails

- [x] Create guardrails (system prompt: "answer based only on context")
- [x] Implement permission-aware context filtering — fixed 2026-08-04: `KnowledgeDocument.DepartmentId` (nullable — null means visible to everyone) scopes RAG search results to the requester's department; `ChatService` looks up the user's department and passes it to `KnowledgeBaseService.SearchAsync`. Backend-complete; no frontend UI yet to set a document's department on upload, so all existing/new docs default to public until set via the API directly
- [x] Implement PII stripping — fixed 2026-08-04: `PiiRedactor` regex-redacts emails, Indonesian NIK (16-digit), Indonesian phone numbers, and credit-card-shaped sequences from RAG context before it reaches the system prompt. Applied to retrieved context only, not the user's own message. Best-effort regex approach, not a guarantee of complete PII removal
- [x] Implement system prompt management
- [x] Implement no-context fallback response
- [ ] Implement prompt/response audit logging
- [x] Implement rate limiting (30 req/min/user)

## Backend — Audit & Monitoring

- [x] Create `AIUsageLog` entity
- [x] Create usage tracking (auto-logged on each AI request)
- [x] Implement token counting and cost estimation
- [x] Create admin usage stats endpoint
- [ ] Set daily token budget check

## Database

- [x] Create migration for pgvector extension
- [x] Create migration for KnowledgeDocuments, KnowledgeChunks
- [x] Create migration for ChatSessions, ChatMessages
- [x] Create migration for AIResponses, AIUsageLog
- [x] Create migration for native `Embedding vector(1536)` column + HNSW index + denormalized `KnowledgeChunk.DepartmentId` — `AddChunkEmbeddingVectorAndDepartmentId` (2026-08-07), applied and verified against the live postgres container

## Frontend — AI Chat

- [x] Create ChatPage (main chat interface)
- [x] Create ChatWindow component (message bubbles, timestamps)
- [x] Create ChatInput component (text input, send button)
- [x] Create MessageBubble component (user/AI messages)
- [x] Create SourceCard component (source citations with relevance %)
- [x] Create AITypingIndicator component (animated dots)
- [x] Implement streaming SSE response rendering (token-by-token)
- [x] Create FeedbackButtons component (thumbs up/down)
- [x] Create EscalateButton component
- [x] Create ConversationHistory component (sidebar)
- [x] Implement Enter to send, Ctrl+Enter for newline
- [x] Implement abort/cancel in-progress request
- [x] Create ChatSessionPage (existing session)
- [x] Create ConversationListPage

## Frontend — Knowledge Base

- [x] Create KnowledgeBaseListPage (documents with status badges)
- [x] Create KnowledgeUploadForm (file upload, validation, progress)
- [x] Create KnowledgeUploadPage (in dialog)
- [x] Create KnowledgeDocumentDetailPage
- [x] Create KnowledgeSearchBar (text search)
- [ ] Create DocumentPreview component (inline PDF/TXT preview)
- [ ] Create UsageStats component (admin token usage display)

## Backend Tests

> **Correction:** this section previously read as not started — wrong, and also contradicts `test-coverage-report.md` (dated 2026-07-14), which predates these files and still lists Phase 4 as "0 tests." Real tests exist: `AIServiceTests` (10), `ChatServiceTests` (19), `KnowledgeBaseServiceTests` (16) = 45 tests, all unit-level against an in-memory DbContext (no `WebApplicationFactory`, so "Integration" items below stay unchecked even where the same behavior is unit-tested).

- [x] Unit: RAG pipeline (embed → search → context → response) — covered across `GenerateEmbeddingAsync`, `SearchAsync`, `GenerateChatResponseAsync` tests
- [ ] Unit: Document text extraction (PDF) — untested
- [ ] Unit: Document text extraction (DOCX) — untested
- [x] Unit: Document text extraction (TXT) — `IndexDocumentAsync_ShouldIndexTextFile`
- [ ] Unit: Chunking strategy (token boundary, overlap) — no dedicated boundary/overlap assertion
- [ ] Unit: AI guardrails (permission checks) — untested, matches confirmed gap (not implemented)
- [ ] Unit: AI guardrails (PII stripping) — untested, matches confirmed gap (not implemented)
- [ ] Unit: Rate limiting — untested
- [x] Unit: Token counting — `EstimateTokenCount_*` (4 tests)
- [ ] Integration: Upload → index → search → chat flow — covered at unit level only
- [ ] Integration: Chat session CRUD with messages — covered at unit level only (`SendMessageAsync`/`GetSessionAsync`/`DeleteSessionAsync`)
- [ ] Integration: SSE streaming — streaming callback is unit-tested (`*_ShouldCallOnToken`), but no real SSE/HTTP integration test
- [ ] Integration: Feedback submission — covered at unit level only (`SubmitFeedbackAsync`)

## Frontend Tests

- [ ] Chat input submit
- [ ] Streaming response rendering
- [ ] Source attribution display
- [ ] Feedback submission
- [ ] Document upload validation (file type, size)
- [ ] Knowledge search with results
- [ ] Conversation history navigation
- [ ] Escalate button flow
