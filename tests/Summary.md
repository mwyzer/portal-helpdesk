# 🧪 Testing Suite — Summary

This folder contains the complete testing strategy for the AI Helpdesk application. It has two parts:

- **20 markdown guides** — each covering a different testing discipline
- **`AIHelpdesk.Tests/`** — the actual xUnit test project with executable test code

---

## Directory Map

```
tests/
├── Summary.md                                  ← you are here
├── accessibility-testing.md                    # ♿ WCAG 2.1 AA compliance
├── AI-specific-testing.md                      # 🤖 LLM evaluation, RAG quality, safety
├── api-testing.md                              # 🌐 HTTP endpoint validation
├── authentication-and-authorization-testing.md # 🔐 JWT, roles, 401/403 gating
├── component-testing.md                        # 🧩 React component isolation (Vitest + RTL)
├── contract-testing.md                         # 📄 API contract (DTOs, OpenAPI, Pact)
├── database-testing.md                         # 🗄️ Schema, migrations, EF Core (PostgreSQL)
├── grounding-testing.md                        # ⚓ AI response anchored in source docs
├── hallucination-testing.md                    # 🌀 Detecting & preventing LLM fabrication
├── human-evaluation-testing.md                 # 👤 Human judgment rubrics for AI quality
├── integration-testing.md                      # 🔗 Controller → DB (WebApplicationFactory + Testcontainers)
├── output-validation-testing.md                # ✅ AI response safety/format/policy pipeline
├── performance-testing.md                      # ⚡ Load, throughput, latency (k6 / NBomber)
├── prompt-injection-testing.md                 # 💉 OWASP LLM01 — prompt injection defense
├── prompt-testing.md                           # ✍️ Prompt template consistency & safety
├── regression-testing.md                       # 🔄 Re-run strategy on every change
├── resilience-and-failure-testing.md           # 🛟 Chaos engineering (Simmy, Polly, Toxiproxy)
├── security-testing.md                         # 🛡️ OWASP Top 10, dependency scanning, ZAP
├── smoke-testing.md                            # 💨 < 60s health check after deploy
├── visual-regression-testing.md                # 🖼️ Screenshot diffs (Playwright)
└── AIHelpdesk.Tests/                           # 🧪 xUnit test project (executable tests)
```

---

## Quick Reference

| # | File | Phase | Status | Key Tools |
|---|------|-------|--------|-----------|
| 1 | `accessibility-testing.md` | All | 📋 Planned | axe-core, WCAG 2.1 AA |
| 2 | `AI-specific-testing.md` | 4 | 📋 Planned | LLM eval, RAG, pgvector |
| 3 | `api-testing.md` | All | 🟡 Partial | Swagger, HTTP files |
| 4 | `auth-and-authorization-testing.md` | 1–2 | 🟡 Partial | JWT, Playwright login |
| 5 | `component-testing.md` | All | 📋 Planned | Vitest, React Testing Library, MSW |
| 6 | `contract-testing.md` | All | 📋 Planned | OpenAPI, Pact |
| 7 | `database-testing.md` | All | 🟡 Partial | EF Core, Testcontainers, Respawn |
| 8 | `grounding-testing.md` | 4 | 📋 Planned | RAG pipeline |
| 9 | `hallucination-testing.md` | 4 | 📋 Planned | LLM evaluation |
| 10 | `human-evaluation-testing.md` | 4 | 📋 Planned | Rubrics, Likert scales |
| 11 | `integration-testing.md` | All | 📋 Planned | WebApplicationFactory, Testcontainers |
| 12 | `output-validation-testing.md` | 4 | 📋 Planned | Safety/format/policy checks |
| 13 | `performance-testing.md` | 7 | 📋 Planned | k6 / NBomber |
| 14 | `prompt-injection-testing.md` | 4 | 📋 Planned | OWASP LLM01 |
| 15 | `prompt-testing.md` | 4 | 📋 Planned | Prompt templates |
| 16 | `regression-testing.md` | All | 📋 Planned | Smoke → E2E → Integration → Unit pyramid |
| 17 | `resilience-and-failure-testing.md` | 7 | 📋 Planned | Simmy, Polly, Toxiproxy |
| 18 | `security-testing.md` | 7 | 📋 Planned | OWASP, Dependabot, ZAP |
| 19 | `smoke-testing.md` | All | 🟡 Partial | Playwright E2E |
| 20 | `visual-regression-testing.md` | All | 🟡 Partial | Playwright screenshots |

**Status key:** ✅ Done · 🟡 Partial · 📋 Planned

---

## What's Actually Running Today

| Suite | Type | Tests | Status |
|-------|------|-------|--------|
| `frontend/tests/e2e/` | E2E (Playwright) | 17 tests | ✅ All passing |
| `tests/AIHelpdesk.Tests/` | Unit (xUnit) | ~2 tests | 🟡 Placeholder |

---

## Per-Phase Testing Focus

| Phase | Primary Tests |
|-------|---------------|
| **Phase 1** (Foundation) | Auth, API, Database, Smoke, Contract |
| **Phase 2** (HR Admin) | Auth, API, Integration, Component |
| **Phase 3** (Secretary) | API, Integration, Component, Contract |
| **Phase 4** (AI Chat) | AI-specific, Prompt, Hallucination, Grounding, Prompt Injection, Output Validation, Human Evaluation |
| **Phase 5** (Ticketing) | Performance, Resilience, Integration |
| **Phase 6** (Recruitment) | Same as Phase 2–3 pattern |
| **Phase 7** (Hardening) | Security, Performance, Resilience, Accessibility, Visual Regression, Regression (full suite) |
