# Phase 4 — AI Helpdesk Chat & Knowledge Base

**Tech Stack:** React (TypeScript) + ASP.NET Core Web API + PostgreSQL (pgvector) + OpenAI/Azure OpenAI API

**Prerequisite:** Phase 1 (Foundation) must be complete.

---

## 1. Overview

Phase 4 delivers the core AI functionality: a conversational helpdesk chat that answers employee questions using internal knowledge base documents. Uses RAG (Retrieval-Augmented Generation) with pgvector for semantic search.

**Goal:** Employees can ask questions in natural language and receive AI-generated answers grounded in company documents.

---

## 2. Deliverables

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | AI Chat interface | Conversational UI with streaming responses |
| 2 | Knowledge base management | Upload PDF/DOCX/TXT, indexing, chunking |
| 3 | RAG pipeline | Document → chunk → embed → vector store → retrieve → generate |
| 4 | Source attribution | Show which documents were used for each answer |
| 5 | AI feedback | Thumbs up/down on responses for quality tracking |
| 6 | Human escalation | Transfer chat to human agent when AI cannot answer |
| 7 | Conversation history | Persistent chat sessions per user |
| 8 | AI guardrails | Permission-aware answers, no unauthorized data access |

---

## 3. New Database Tables

| Table | Key Columns | Description |
|-------|-------------|-------------|
| `KnowledgeDocuments` | Id, Title, FileName, FilePath, FileType, ContentType, FileSize, Status (Pending/Indexing/Ready/Failed), PageCount, ErrorMessage | Uploaded documents |
| `KnowledgeChunks` | Id, DocumentId, Content, ChunkIndex, Embedding (vector(1536)), Metadata (JSON) | Vector-indexed chunks |
| `ChatSessions` | Id, UserId, Title, Status (Active/Resolved/Escalated), CreatedAt, UpdatedAt | Per-user chat sessions |
| `ChatMessages` | Id, SessionId, Role (User/Assistant/System), Content, Sources (JSON), CreatedAt | Individual messages |
| `AIResponses` | Id, MessageId, ModelUsed, PromptTokens, CompletionTokens, TotalTokens, LatencyMs, FeedbackScore, FeedbackComment | Usage tracking + feedback |
| `AIUsageLog` | Id, UserId, Endpoint, TokensUsed, Cost, CreatedAt | Billing/usage audit |

All tables include: `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`.

### KnowledgeDocument Status Flow
```
Pending → Indexing → Ready
                  ↓
                 Failed
```

---

## 4. API Endpoints

### AI Chat

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/ai/chat` | All | Send message, get AI response (streaming) |
| GET | `/api/ai/conversations` | All | List user's chat sessions |
| GET | `/api/ai/conversations/{id}` | All | Get session with messages |
| DELETE | `/api/ai/conversations/{id}` | All | Delete session |
| PUT | `/api/ai/conversations/{id}` | All | Update session title |
| POST | `/api/ai/responses/{id}/feedback` | All | Submit feedback (like/dislike + comment) |
| POST | `/api/ai/conversations/{id}/escalate` | All | Escalate to human agent |

### Knowledge Base

| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| GET | `/api/knowledge-documents` | All | List documents (filterable by status) |
| POST | `/api/knowledge-documents` | Secretary, HR Admin, Super Admin | Upload document |
| GET | `/api/knowledge-documents/{id}` | All | Document detail |
| DELETE | `/api/knowledge-documents/{id}` | Admin | Delete document + chunks |
| POST | `/api/knowledge-documents/{id}/index` | Admin | Re-index document |
| POST | `/api/knowledge-documents/search` | All | Semantic search across documents |

---

## 5. Frontend Pages

| Route | Page | Access |
|-------|------|--------|
| `/ai/chat` | ChatPage (main interface) | All |
| `/ai/chat/:sessionId` | ChatSessionPage (existing session) | All (owner) |
| `/ai/conversations` | ConversationListPage | All |
| `/knowledge-base` | KnowledgeBaseListPage | All |
| `/knowledge-base/upload` | KnowledgeUploadPage | Secretary, HR Admin |
| `/knowledge-base/:id` | KnowledgeDocumentDetailPage | All |

### New Components

| Component | Description |
|-----------|-------------|
| `ChatWindow` | Full chat UI with message bubbles, timestamps |
| `ChatInput` | Text input with send button, file attachment support |
| `MessageBubble` | User/AI message with source citations |
| `SourceCard` | Source document reference (title, snippet, relevance %) |
| `AITypingIndicator` | Animated dots while AI generates response |
| `FeedbackButtons` | Thumbs up/down with optional comment |
| `EscalateButton` | Escalate to human agent |
| `KnowledgeUploadForm` | Drag-drop upload, file validation, progress bar |
| `KnowledgeSearchBar` | Search across knowledge base |
| `DocumentPreview` | Inline preview for PDF/TXT |
| `ConversationHistory` | Sidebar list of past sessions |
| `UsageStats` | Token usage, cost display (admin only) |

---

## 6. Business Logic

### RAG Pipeline
1. User sends question via POST `/api/ai/chat`
2. Question is embedded using OpenAI `text-embedding-ada-002` or `text-embedding-3-small`
3. pgvector similarity search: `SELECT * FROM knowledge_chunks ORDER BY embedding <=> {query_embedding} LIMIT 5`
4. Retrieved chunks (context) + user question sent to LLM
5. LLM generates answer with source citations
6. Response streamed back to client via SSE (Server-Sent Events)
7. Usage logged to `AIUsageLog`

### Document Processing
1. File uploaded → saved to disk/blob storage
2. Background job extracts text:
   - PDF: `PdfPig` or `PdfSharp`
   - DOCX: `DocumentFormat.OpenXml` or `MarkdownConverter`
   - TXT: direct read
3. Text split into chunks (512 tokens with 64 overlap)
4. Each chunk embedded and stored in pgvector
5. Document status updated to Ready

### AI Guardrails (implemented in `AIGuardrailService`)
- Check user permissions before answering role-specific queries
- Strip PII from context before sending to LLM
- Add system prompt: "Answer based only on provided context"
- If no relevant context found: "I cannot find information about this in the knowledge base"
- Log all prompts and responses for audit
- Rate limit: max 30 requests per minute per user

### Streaming Response
- Backend: SSE endpoint `/api/ai/chat` streams token by token
- Frontend: `EventSource` or `fetch` with `ReadableStream`
- AbortController for cancel-in-progress

---

## 7. Implementation Steps

### Step 1: Infrastructure
- Add pgvector extension to PostgreSQL (Docker Compose + migration)
- Set `POSTGRES_EXTENSIONS=pgvector` in postgres container
- Create vector column migration

### Step 2: Backend — AI Service Core
- Create `IAIService` abstraction (OpenAI/Azure OpenAI implementation)
- Implement embedding generation
- Implement chat completion with streaming
- Create `AIOptions` configuration class (endpoint, key, model names, temperature)
- Implement rate limiting middleware for AI endpoints

### Step 3: Backend — RAG Pipeline
- Create `RagService`: embed → search → retrieve → build context → generate
- Create `VectorSearchService`: pgvector similarity search queries
- Implement chunking strategy (token-based, with overlap)
- Implement document text extraction (PDF, DOCX, TXT)

### Step 4: Backend — Chat Module
- Create `ChatSessionController` + service
- Create `ChatMessage` repository
- Implement POST `/api/ai/chat` with SSE streaming
- Implement conversation history (list + detail + delete)
- Implement feedback submission
- Implement escalation (creates ticket in Phase 5)

### Step 5: Backend — Knowledge Base Module
- Create `KnowledgeDocumentController` + service
- Implement file upload with validation (size, type, virus scan)
- Implement background indexing (Hangfire/Quartz job)
- Implement semantic search endpoint
- Implement document deletion (cascade chunks)

### Step 6: Backend — Audit & Monitoring
- Create `AIUsageLog` service
- Implement token counting and cost estimation
- Create admin endpoint for usage stats
- Create health check endpoint for AI provider

### Step 7: Frontend — AI Chat
- ChatPage with ChatWindow, ChatInput, MessageBubble
- Streaming response display (token-by-token)
- Source attribution cards below AI messages
- Feedback buttons on each AI response
- Escalate button
- Conversation history sidebar
- Keyboard shortcuts (Enter to send, Ctrl+Enter for newline)

### Step 8: Frontend — Knowledge Base
- Knowledge base list with status badges (Pending → Indexing → Ready → Failed)
- Upload form with drag-drop, progress bar
- Document detail page with search preview
- Semantic search bar with results

### Step 9: Backend — Tests
- Unit: RAG pipeline (embed → search → context → response)
- Unit: Document text extraction (PDF, DOCX, TXT)
- Unit: Chunking strategy (token boundary, overlap)
- Unit: AI guardrails (permission checks, PII stripping)
- Unit: Rate limiting
- Integration: Upload → index → search → chat flow
- Integration: Chat session CRUD with messages
- Integration: SSE streaming

### Step 10: Frontend — Tests
- Chat input submit/stream rendering
- Source attribution display
- Feedback submission
- Document upload validation (file type, size)
- Knowledge search with results
- Conversation history navigation

---

## 8. CI/CD Additions

- Add AI provider connection string to GitHub Actions secrets
- Add integration test step with Testcontainers (pgvector)
- Add vector DB health check in deployment pipeline

---

## 9. Seed Data

- No default documents (users upload their own)
- Sample system prompts configured in `appsettings.json`:
  - `SystemPrompt`: "You are an AI Helpdesk assistant for an internal company portal..."
  - `NoContextResponse`: "I cannot find relevant information in the knowledge base..."

---

## 10. Acceptance Criteria

| # | Criteria |
|---|----------|
| 1 | Employee can start a new chat session and ask questions |
| 2 | AI responses stream in real-time (token by token) |
| 3 | AI answers are grounded in knowledge base documents |
| 4 | Source documents are shown alongside each answer |
| 5 | Uploaded documents (PDF, DOCX, TXT) are indexed and searchable |
| 6 | Semantic search returns relevant results across documents |
| 7 | User can provide feedback (like/dislike) on AI responses |
| 8 | User can escalate to human agent when AI cannot answer |
| 9 | Rate limiting prevents abuse (max 30 req/min/user) |
| 10 | AI cannot access information it shouldn't based on user role |
| 11 | All AI interactions are logged for audit |
| 12 | Admin can view usage statistics (tokens, costs) |

---

## 11. Estimated Effort

| Area | Estimated Days |
|------|:--------------:|
| AI service infrastructure | 2 days |
| RAG pipeline (embedding + search + context) | 3 days |
| Chat module (backend) | 3 days |
| Knowledge base module (backend) | 3 days |
| Document processing (extraction + chunking) | 3 days |
| AI guardrails + audit | 2 days |
| AI Chat frontend (with streaming) | 4 days |
| Knowledge base frontend | 2 days |
| Backend tests | 3 days |
| Frontend tests | 2 days |
| **Total** | **~27 days** |

---

## 12. Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| AI API cost | Set daily token budget; cache common queries |
| Response latency | Streaming masks latency; optimize chunk retrieval |
| Poor answer quality | Feedback loop + guardrails; human oversight |
| Sensitive data leak via context | PII stripping; permission-aware filtering |
| pgvector performance on large corpus | Indexing (IVFFlat/HNSW); periodic reindexing |
| File upload abuse | Validate file type, size limit (10MB), scan for malware |
