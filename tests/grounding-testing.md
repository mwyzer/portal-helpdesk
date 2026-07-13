# ⚓ Grounding Testing

> **Status:** 📋 Planned — AI Chat / RAG is in Phase 4  
> **Goal:** Ensure AI responses are anchored in real, authoritative source documents

## Overview

**Grounding** is the property that an AI response is backed by verifiable source content. A grounded response doesn't just sound correct — its claims can be **traced back** to specific passages in knowledge base documents.

Grounding testing verifies two things:
1. **Retrieval quality** — does the RAG pipeline surface the right documents?
2. **Faithfulness** — does the LLM accurately reflect what the documents say, without adding or distorting?

```
User: "How many annual leave days?"

  ┌─────────────┐     ┌─────────────┐     ┌────────────┐
  │  Knowledge  │────▶│   Retrieve  │────▶│    LLM     │
  │    Base     │     │  Top-K docs │     │  Generate  │
  │             │     │             │     │  response  │
  └─────────────┘     └─────────────┘     └─────┬──────┘
                                                │
                                    ┌───────────▼───────────┐
                                    │ "You have 12 days of  │
                                    │  annual leave per year│
                                    │  [Leave Policy §3.1]" │
                                    └───────────────────────┘
                                         ▲
                                         │
                                    GROUNDED in
                                    Leave Policy §3.1
```

## Grounding Test Layers

### Layer 1: Retrieval Quality

Does the system find the right documents for a given query?

```csharp
[Fact]
public async Task Retrieve_ReturnsRelevantDocument_ForLeaveQuestion()
{
    // Seed knowledge base
    await SeedDocuments(
        ("Leave Policy", "Annual leave: 12 days. Sick leave: 14 days."),
        ("Parking Policy", "Parking available on B1 and B2."),
        ("IT Policy", "Password must be 12 characters.")
    );

    var chunks = await _ragService.RetrieveAsync(
        "How many vacation days do I get?", topK: 3);

    // Top result should be Leave Policy, not Parking
    chunks.Should().NotBeEmpty();
    chunks.First().DocumentTitle.Should().Be("Leave Policy");
    chunks.First().Content.Should().Contain("12 days");
}

[Theory]
[InlineData("leave", "Leave Policy")]
[InlineData("parking", "Parking Policy")]
[InlineData("password", "IT Policy")]
public async Task Retrieve_RoutesToCorrectDocument(string query, string expectedDoc)
{
    await SeedDocuments(
        ("Leave Policy", "Annual leave: 12 days."),
        ("Parking Policy", "Parking on B1, B2."),
        ("IT Policy", "Password: 12 chars minimum.")
    );

    var chunks = await _ragService.RetrieveAsync(query, topK: 1);

    chunks.Should().HaveCount(1);
    chunks.First().DocumentTitle.Should().Be(expectedDoc);
}
```

### Layer 2: Retrieval Precision & Recall

Quantitative metrics calculated over a test corpus.

```csharp
public class RetrievalMetrics
{
    // Known: for question Q, the correct documents are D_correct
    [Fact]
    public async Task Retrieve_HasHighPrecisionAtK()
    {
        var testQueries = new[]
        {
            new { Q = "annual leave days",   CorrectDocIds = new[] { "doc-1" } },
            new { Q = "sick leave policy",    CorrectDocIds = new[] { "doc-2" } },
            new { Q = "work from home rules", CorrectDocIds = new[] { "doc-3" } },
        };

        double totalPrecision = 0;
        double totalRecall = 0;

        foreach (var test in testQueries)
        {
            var retrieved = await _ragService.RetrieveAsync(test.Q, topK: 5);
            var retrievedIds = retrieved.Select(r => r.DocumentId).ToList();

            // Precision@5 = relevant / retrieved
            var relevant = retrievedIds.Intersect(test.CorrectDocIds).Count();
            totalPrecision += (double)relevant / retrievedIds.Count;

            // Recall@5 = relevant / total relevant
            totalRecall += (double)relevant / test.CorrectDocIds.Length;
        }

        var avgPrecision = totalPrecision / testQueries.Length;
        var avgRecall = totalRecall / testQueries.Length;

        avgPrecision.Should().BeGreaterThan(0.7);  // ≥ 70% precision
        avgRecall.Should().BeGreaterThan(0.8);     // ≥ 80% recall
    }
}
```

### Layer 3: Context Relevance Scoring

Not every retrieved chunk is useful. Measure relevance:

```csharp
[Fact]
public async Task RetrievedChunks_AreRelevantToQuery()
{
    var query = "What is the dress code?";
    var chunks = await _ragService.RetrieveAsync(query, topK: 5);

    // Use a relevance scoring model (or another LLM call) to evaluate
    foreach (var chunk in chunks)
    {
        var relevanceScore = await _relevanceScorer.ScoreAsync(query, chunk.Content);
        relevanceScore.Should().BeGreaterThan(0.5,
            $"chunk '{chunk.Content[..50]}...' should be relevant to '{query}'");
    }
}

[Fact]
public async Task Retrieval_DoesNotReturnNoise()
{
    var query = "annual leave policy";
    var chunks = await _ragService.RetrieveAsync(query, topK: 5);

    // No chunk should be about completely unrelated topics
    var irrelevantKeywords = new[] { "parking", "elevator", "vending machine" };

    foreach (var chunk in chunks)
    {
        foreach (var keyword in irrelevantKeywords)
        {
            chunk.Content.Should().NotContain(keyword,
                $"retrieved chunk should not contain '{keyword}' for query '{query}'");
        }
    }
}
```

### Layer 4: Faithfulness — Does the LLM stick to the context?

The LLM must not add information not present in the retrieved context.

```csharp
[Fact]
public async Task GeneratedAnswer_OnlyUsesProvidedContext()
{
    // Provide ONLY this context (no other documents)
    var context = "Annual leave: Employees are entitled to 12 days per calendar year.";
    var question = "How many annual leave days do I get?";

    var answer = await _llmService.GenerateGroundedAsync(question, context);

    // Must contain what's in context
    answer.Should().Contain("12 days");

    // Must NOT contain fabricated details
    answer.Should().NotContain("carry over");        // not in context
    answer.Should().NotContain("probation");          // not in context
    answer.Should().NotContain("approval required");  // not in context

    // Must NOT mention documents not provided
    answer.Should().NotContain("According to");      // no fake citations
}

[Fact]
public async Task GeneratedAnswer_DoesNotDistortNumbers()
{
    var context = "Sick leave: 14 days per year. Requires medical certificate after 2 days.";
    var question = "How many sick leave days?";

    var answer = await _llmService.GenerateGroundedAsync(question, context);

    // Exact number from context
    answer.Should().Contain("14");

    // Must NOT hallucinate different numbers
    answer.Should().NotContain("10");
    answer.Should().NotContain("30");
    answer.Should().NotContain("unlimited");
}
```

### Layer 5: NLI-Based Faithfulness (Natural Language Inference)

Use an NLI model to check each claim in the answer against the source:

```csharp
[Fact]
public async Task EachClaim_IsEntailedBySource()
{
    var context = "Annual leave: 12 days. Must be approved by manager 1 week in advance.";
    var question = "How do I take annual leave?";

    var answer = await _llmService.GenerateGroundedAsync(question, context);

    // Break answer into individual claims
    var claims = _claimSplitter.Split(answer);

    foreach (var claim in claims)
    {
        // Use a pretrained NLI model to check entailment
        var result = await _nliService.PredictAsync(context, claim);

        // Each claim should be entailed by (or at least not contradict) the context
        result.Label.Should().BeOneOf(
            ["entailment", "neutral"],
            $"claim '{claim}' should not contradict the source");

        if (result.Label == "neutral" && result.Score < 0.3)
        {
            // Flag for review: possible hallucination
            _logger.Warning($"Low-confidence claim: '{claim}' (score={result.Score})");
        }
    }
}
```

## Grounding Test Data

### Seed Knowledge Base for Tests

```csharp
private async Task SeedDocuments(params (string title, string content)[] docs)
{
    foreach (var (title, content) in docs)
    {
        var embedding = await _embeddingService.GenerateAsync(content);
        _context.KnowledgeDocuments.Add(new KnowledgeDocument
        {
            Title = title,
            Content = content,
            Embedding = new Vector(embedding),
            Version = "1.0",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
    }
    await _context.SaveChangesAsync();
}
```

### Minimum Test Corpus

| Document | Content | Test Queries |
|----------|---------|-------------|
| Leave Policy v2.1 | Annual leave 12d, Sick leave 14d, Carryover 5d | "annual leave", "sick days", "carryover" |
| Dress Code | Business casual, No jeans on Friday | "dress code", "what to wear" |
| WFH Policy | Eligible roles: Engineering, Design. 2 days/week | "work from home", "remote work" |
| Benefits Overview | Health insurance, BPJS, Parking subsidy | "benefits", "insurance", "parking" |

## Grounding Metrics Dashboard

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Precision@5** | ≥ 70% | % of retrieved docs that are relevant |
| **Recall@5** | ≥ 80% | % of all relevant docs that were retrieved |
| **MRR (Mean Reciprocal Rank)** | ≥ 0.8 | Avg position of first relevant doc |
| **Faithfulness Score** | ≥ 90% | % of claims entailed by context (NLI) |
| **Source Attribution Rate** | ≥ 95% | % of answers citing at least 1 source |
| **Hallucination Rate** | ≤ 3% | % of answers with unsupported claims |

## Common Grounding Failures

| Failure | Cause | Fix |
|---------|-------|-----|
| Retrieved wrong doc | Embedding didn't capture query intent | Finetune embeddings on domain data |
| Retrieved irrelevant chunk | Chunk too large or too small | Adjust chunk size (500-1000 tokens) |
| LLM added details not in context | Prompt doesn't emphasize grounding | Add "Only use provided context" to prompt |
| LLM paraphrased incorrectly | Context too complex | Simplify/restructure knowledge base content |
| Outdated document prioritized | No recency weighting | Add `DateTime` boost to retrieval |
| Ambiguous query matched wrong doc | Single poor query | Query expansion, hyDE, or multi-query retrieval |

## CI Integration

```yaml
grounding:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - run: dotnet test --filter "Category=Grounding"
    - name: Check metrics
      run: |
        python scripts/eval-grounding.py \
          --precision-threshold 0.7 \
          --recall-threshold 0.8
```

## Related Files

- `tests/AI-specific-testing.md` — Overall AI testing strategy
- `tests/hallucination-testing.md` — Fabrication detection
- `tests/prompt-testing.md` — Prompt composition & instructions
- `src/AIHelpdesk.Infrastructure/Services/RagService.cs` — Retrieval pipeline (Phase 4)
- `src/AIHelpdesk.Infrastructure/Services/EmbeddingService.cs` — Embedding generation
