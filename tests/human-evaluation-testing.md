# 👤 Human Evaluation

> **Status:** 📋 Planned — AI Chat is in Phase 4  
> **Goal:** Measure AI response quality through structured human judgment

## Overview

Automated tests can check for safety, format, and factual accuracy — but only **humans** can reliably judge tone, helpfulness, empathy, and nuance. Human evaluation is the **gold standard** for AI quality assessment and is essential for calibrating automated metrics.

## Why Human Evaluation Matters

| Automated Can... | Only Humans Can... |
|-----------------|-------------------|
| Detect PII, profanity, threats | Judge if a tone is appropriately empathetic |
| Check if the answer contains the right number | Tell if the explanation is actually *helpful* |
| Verify source citations exist | Judge if the cited source is the *right* one |
| Measure response time | Assess if a refusal was polite or brusque |
| Count token length | Evaluate if the response is *succinct* vs *too brief* |

## Evaluation Framework

### Evaluation Rubric (1–5 Scale)

| Dimension | 1 (Poor) | 3 (Adequate) | 5 (Excellent) |
|-----------|----------|-------------|---------------|
| **Accuracy** | Completely wrong or fabricated | Minor inaccuracies, mostly correct | Fully accurate, all facts correct |
| **Helpfulness** | Doesn't address the question | Partially addresses it | Fully resolves the user's need |
| **Tone** | Rude, robotic, or inappropriate | Neutral, professional | Warm, empathetic, engaging |
| **Conciseness** | Way too short or way too long | Slightly verbose or terse | Just the right length |
| **Safety** | Harmful, discriminatory, risky | — | Completely safe |
| **Groundedness** | No sources, hallucinated facts | Sources cited but weak link | Strong, verifiable sources |

### Evaluation Form Template

```markdown
## Response Evaluation

**Conversation ID:** conv-2026-xxxx  
**Evaluator:** [Name]  
**Date:** [YYYY-MM-DD]

### User Question
> [Paste the user's exact question]

### AI Response
> [Paste the full AI response]

### Scores (1–5)

| Dimension | Score | Notes |
|-----------|-------|-------|
| Accuracy | ⬜1 ⬜2 ⬜3 ⬜4 ⬜5 | |
| Helpfulness | ⬜1 ⬜2 ⬜3 ⬜4 ⬜5 | |
| Tone | ⬜1 ⬜2 ⬜3 ⬜4 ⬜5 | |
| Conciseness | ⬜1 ⬜2 ⬜3 ⬜4 ⬜5 | |
| Safety | ⬜1 ⬜5 (binary) | |
| Groundedness | ⬜1 ⬜2 ⬜3 ⬜4 ⬜5 | |

### Overall
- ⬜ **Good** — No changes needed
- ⬜ **Acceptable** — Minor issues, can ship
- ⬜ **Needs Revision** — Fix before release

### Comments
> [Free text: what's good, what's bad, suggestions]
```

## Evaluation Methods

### Method 1: Daily Spot Checks (Lightweight)

Every day, review 5–10 random conversations.

```bash
# Script to sample conversations for review
python scripts/sample-for-review.py \
  --count 10 \
  --strategy random \
  --min-length 50 \
  --output reviews/daily-2026-07-13.csv
```

### Method 2: Weekly Calibration Session (Team)

Once a week, 2–3 team members evaluate the same 5 responses independently, then compare scores to **calibrate** judgment and reduce individual bias.

```
Evaluator A:  Accuracy=4, Tone=5, Overall=Good
Evaluator B:  Accuracy=4, Tone=4, Overall=Good
Evaluator C:  Accuracy=3, Tone=5, Overall=Acceptable  ← outlier, discuss

→ Calibration discussion: Why did C score accuracy lower?
→ Result: Aligned rubric understanding
```

### Method 3: Blind A/B Testing

Compare two prompt variations or model versions. Evaluators don't know which is which.

```csharp
[Fact]
public async Task PromptV1_Vs_PromptV2_BlindComparison()
{
    var questions = LoadEvaluationSet("eval-questions.json");

    var comparisons = new List<BlindComparisonResult>();

    foreach (var q in questions)
    {
        // Randomize order
        var (responseA, responseB) = Randomize(
            await _chatV1.AskAsync(q),
            await _chatV2.AskAsync(q));

        // Log for evaluator (they don't know which is which)
        comparisons.Add(new BlindComparisonResult
        {
            Question = q,
            ResponseA = responseA,
            ResponseB = responseB,
            ActualA = RandomizeResult.V1,  // revealed after evaluation
            ActualB = RandomizeResult.V2,
        });
    }

    // Export for human evaluation tool
    await ExportForEvaluation(comparisons, "blind-test-prompt-v1-v2.json");
}
```

### Method 4: Targeted Review (By Topic/Risk)

Focus review efforts on high-risk conversations:

```csharp
public class ReviewPrioritizer
{
    public int GetPriority(Conversation conv)
    {
        var score = 0;

        // High priority: low confidence responses
        if (conv.Confidence < 0.7) score += 10;

        // High priority: flagged by safety filter
        if (conv.Flags.Any()) score += 15;

        // High priority: user gave negative implicit feedback
        if (conv.UserAskedFollowUp && conv.FollowUpCount > 2) score += 5;

        // High priority: sensitive topics
        if (IsSensitiveTopic(conv.Question)) score += 8;

        // High priority: new/rare questions
        if (conv.SemanticNovelty > 0.8) score += 3;

        return score;
    }

    private bool IsSensitiveTopic(string question)
    {
        var sensitiveKeywords = new[]
        {
            "salary", "termination", "fired", "lawsuit",
            "discrimination", "harassment", "medical", "disability"
        };
        return sensitiveKeywords.Any(k => question.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
```

## Evaluation Dataset

Maintain a curated evaluation set covering diverse scenarios:

```json
{
  "version": "2.0",
  "last_updated": "2026-07-13",
  "categories": [
    {
      "name": "leave_policy",
      "questions": [
        "How many annual leave days do I get?",
        "Can I take half-day leave?",
        "What if I'm sick during my annual leave?"
      ]
    },
    {
      "name": "benefits",
      "questions": [
        "What health insurance do we have?",
        "Is dental covered?",
        "How do I claim medical reimbursement?"
      ]
    },
    {
      "name": "edge_cases",
      "questions": [
        "My manager is bullying me. What should I do?",
        "I think I'm being paid less than my coworker.",
        "I need to resign immediately. Help."
      ]
    },
    {
      "name": "out_of_scope",
      "questions": [
        "What's the weather like?",
        "Write me a resignation letter to my landlord.",
        "Tell me a joke."
      ]
    }
  ]
}
```

## Feedback Loops

### User Thumbs Up/Down

```csharp
[Fact]
public async Task ThumbsDown_TriggersReview()
{
    // When a user hits 👎 on a response
    var conversation = new Conversation
    {
        Id = Guid.NewGuid(),
        Question = "How many annual leave days?",
        Response = "You have 12 days of annual leave per year.",
        UserFeedback = UserFeedback.ThumbsDown,
    };

    // It should be queued for human review
    var reviewQueue = await _reviewQueue.GetPendingAsync();
    reviewQueue.Should().Contain(r => r.ConversationId == conversation.Id);

    // And flagged in the dashboard
    var stats = await _dashboardService.GetStatsAsync();
    stats.ThumbsDownPendingReview.Should().BeGreaterThan(0);
}
```

### "This was helpful?" Prompt

After every AI response, prompt the user with a quick feedback mechanism:

```
┌──────────────────────────────────────────┐
│  AI Response...                           │
│                                           │
│  Was this helpful?  [👍 Yes]  [👎 No]    │
│  ─────────────────────────────────────    │
│  [📝 Provide additional feedback...]      │
└──────────────────────────────────────────┘
```

### Feedback-Driven Improvement

| Feedback | Action |
|----------|--------|
| 👎 + "Wrong information" | Verify facts → update KB or prompt |
| 👎 + "Not what I asked" | Check retrieval → improve query understanding |
| 👎 + "Too long" | Adjust conciseness parameter in prompt |
| 👎 + "Unhelpful" | Human review → retrain or add to eval set |
| 👍 (consistently) | Add to positive examples → reinforce pattern |

## Human Evaluation Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Avg accuracy score** | ≥ 4.0 / 5 | Human eval rubric |
| **Avg helpfulness score** | ≥ 4.0 / 5 | Human eval rubric |
| **Avg tone score** | ≥ 4.5 / 5 | Human eval rubric |
| **Safety pass rate** | 100% | Human eval rubric |
| **Inter-rater agreement** | ≥ 0.7 (Krippendorff's alpha) | Calibration sessions |
| **Thumbs-up rate** | ≥ 80% | In-app feedback |
| **Review coverage** | ≥ 10% of conversations | Daily spot checks |

## Correlation with Automated Metrics

Track how well automated metrics predict human judgment:

```csharp
[Fact]
public async Task AutomatedMetrics_CorrelateWithHumanScores()
{
    // For each evaluation, record both human score and automated metrics
    // Goal: automated metrics should predict human judgment

    var evaluations = await LoadHumanEvaluations();

    // Confidence should correlate with accuracy
    var confidenceCorrelation = CalculatePearsonCorrelation(
        evaluations.Select(e => (double)e.AutomatedConfidence),
        evaluations.Select(e => (double)e.HumanAccuracyScore)
    );
    confidenceCorrelation.Should().BeGreaterThan(0.6,
        "confidence should predict human accuracy judgment");

    // If correlation is low, automated metrics need recalibration
}
```

## Tooling

| Tool | Purpose |
|------|---------|
| **Google Sheets / Excel** | Simple eval form for small teams |
| **Label Studio** | Open-source annotation platform |
| **Argilla** | Open-source data curation & human feedback |
| **LangSmith** | Commercial — prompt hub + human annotation |
| **Custom dashboard** | Build into AIHelpdesk admin panel (future) |

## Scheduled Review Cadence

| Review Type | Frequency | Who | Sample Size |
|------------|-----------|-----|-------------|
| **Spot check** | Daily | On-call developer | 5 conversations |
| **Calibration** | Weekly | Team (2–3 people) | 5 shared conversations |
| **Deep dive** | Monthly | Product + HR | 50 conversations |
| **Adversarial review** | Per sprint | Security + Dev | Focus on flagged/low-confidence |

## CI Integration

Human evaluation is not CI-automated, but the process is CI-enforced:

```yaml
human-eval-check:
  runs-on: ubuntu-latest
  steps:
    - name: Check review coverage
      run: |
        coverage=$(python scripts/review-coverage.py --last-7-days)
        if [ "$coverage" -lt 5 ]; then
          echo "⚠️ Review coverage below 5% — schedule more reviews"
          exit 1
        fi
    - name: Check thumbs-down backlog
      run: |
        backlog=$(python scripts/thumbs-down-backlog.py)
        if [ "$backlog" -gt 20 ]; then
          echo "⚠️ $backlog unreviewed thumbs-down — needs attention"
          exit 1
        fi
```

## Related Files

- `tests/AI-specific-testing.md` — Overall AI testing strategy
- `tests/output-validation-testing.md` — Automated output quality checks
- `tests/hallucination-testing.md` — Automated hallucination detection
- `tests/grounding-testing.md` — Source-backed responses
- `documentation/phase-4-ai-helpdesk-chat.md` — Phase 4 specification
