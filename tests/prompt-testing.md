# ✍️ Prompt Testing

> **Status:** 📋 Planned — AI Chat is in Phase 4  
> **Scope:** Prompt templates, composition, output validation

## Overview

Prompt testing evaluates the **instructions sent to the LLM** — not the model itself. A well-tested prompt produces consistent, safe, and helpful outputs across a wide range of inputs. Prompt quality directly determines the quality of the AI helpdesk experience.

## Why Test Prompts?

Prompts are the **only deterministic part** of an LLM pipeline. Models are black boxes; prompts are your code. Testing prompts catches:

- Ambiguous instructions that confuse the model
- Missing context that leads to incorrect answers
- Format drift where outputs stop following a schema
- Injections that bypass system-level guardrails
- Tone shifts (professional → casual, polite → rude)

```
┌─────────────────────────────────────────────────────┐
│                 Prompt = Instructions                │
│                                                     │
│  ┌──────────┐   ┌──────────┐   ┌──────────────────┐ │
│  │  System  │ + │  Context │ + │  User Question   │ │
│  │  Prompt  │   │  (RAG)   │   │                  │ │
│  └──────────┘   └──────────┘   └──────────────────┘ │
│        │              │                  │          │
│        └──────────────┴──────────────────┘          │
│                        │                            │
│                    ▼                                │
│               LLM Response                          │
└─────────────────────────────────────────────────────┘
```

## Prompt Testing Levels

### Level 1: Static Prompt Validation

Test the prompt **before** it reaches the LLM — cheap, fast, deterministic.

```csharp
[Fact]
public void SystemPrompt_ContainsRequiredElements()
{
    var prompt = _promptBuilder.BuildSystemPrompt();

    // Role definition
    prompt.Should().Contain("HR assistant");
    prompt.Should().Contain("AIHelpdesk");

    // Behavioral constraints
    prompt.Should().Contain("do not fabricate");
    prompt.Should().Contain("cite sources");

    // Tone guidance
    prompt.Should().ContainAny("professional", "helpful", "polite");

    // Scope boundaries
    prompt.Should().Contain("only answer questions about company policies");
}

[Fact]
public void SystemPrompt_DoesNotExceedTokenLimit()
{
    var prompt = _promptBuilder.BuildSystemPrompt();
    var tokenCount = _tokenizer.CountTokens(prompt);

    // Reserve enough for context + user question + response
    tokenCount.Should().BeLessThan(1000); // out of ~4096 total
}

[Fact]
public void RagPrompt_FormatsContextCorrectly()
{
    var context = new List<DocumentChunk>
    {
        new("Annual leave: 12 days per year.", "Leave Policy v2.1"),
        new("Sick leave: 14 days per year.", "Leave Policy v2.1"),
    };

    var prompt = _promptBuilder.BuildRagPrompt(context, "leave days?");

    prompt.Should().Contain("Source: Leave Policy v2.1");
    prompt.Should().Contain("Annual leave: 12 days");
    prompt.Should().Contain("Sick leave: 14 days");
    prompt.Should().Contain("leave days?");

    // Context should appear BEFORE the question
    var contextIndex = prompt.IndexOf("Leave Policy v2.1");
    var questionIndex = prompt.IndexOf("leave days?");
    contextIndex.Should().BeLessThan(questionIndex);
}
```

### Level 2: Prompt Variable Injection

Test that dynamic variables are properly injected and escaped.

```csharp
[Fact]
public void Prompt_InjectEmployeeNameCorrectly()
{
    var prompt = _promptBuilder.BuildGreetingPrompt(
        employeeName: "Alice",
        department: "Engineering",
        pendingActions: 5);

    prompt.Should().Contain("Alice");
    prompt.Should().Contain("Engineering");
    prompt.Should().Contain("5 pending actions");
}

[Fact]
public void Prompt_EscapesSpecialCharacters()
{
    // User names could contain curly braces, newlines, etc.
    var prompt = _promptBuilder.BuildGreetingPrompt(
        employeeName: "O'Brien {SQL}",
        department: "R&D\nNewline",
        pendingActions: 0);

    // Should not break prompt formatting
    prompt.Should().NotContain("{SQL}");  // escaped or removed
    prompt.Should().NotContain("\n");     // replaced or escaped
}

[Fact]
public void Prompt_HandlesNullValues_Gracefully()
{
    var prompt = _promptBuilder.BuildGreetingPrompt(
        employeeName: null!,
        department: null!,
        pendingActions: 0);

    prompt.Should().NotContain("null");
    prompt.Should().ContainAny("Employee", "Hello"); // fallback
}
```

### Level 3: Output Format Validation

Test that the LLM's response follows the expected format/schema.

```csharp
[Fact]
public async Task Chat_OutputFollowsJsonSchema()
{
    // Use function-calling / structured output mode
    var response = await _chatService.AskStructuredAsync(
        "How many annual leave days?");

    response.Should().BeOfType<ChatStructuredResponse>();
    response.Answer.Should().NotBeNullOrEmpty();
    response.Sources.Should().NotBeNull();
    response.Confidence.Should().BeInRange(0, 1);
}

[Fact]
public async Task Chat_ReturnsValidJson_WhenAskedForStructuredData()
{
    var response = await _chatService.AskAsync(
        "List all leave types as JSON");

    // The answer should contain parseable JSON
    var jsonStart = response.Answer.IndexOf('{');
    var jsonEnd = response.Answer.LastIndexOf('}') + 1;

    if (jsonStart >= 0 && jsonEnd > jsonStart)
    {
        var json = response.Answer[jsonStart..jsonEnd];
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();
    }
}
```

### Level 4: Prompt Versioning & Regression

Track prompt changes like code. Every prompt change goes through tests.

```csharp
// Store prompts as versioned templates
public static class PromptTemplates
{
    public const string V1_0_System = """
        You are an HR assistant for AIHelpdesk...
        """;

    public const string V1_1_System = """
        You are a helpful HR and office assistant for AIHelpdesk.
        Only answer questions based on provided context.
        If unsure, say "I don't have that information."
        """;
}

[Fact]
public void PromptV1_1_IsStrongerThan_V1_0()
{
    // Metric: % of "I don't know" responses for unknown questions
    var unknownQuestions = new[]
    {
        "What is the capital of Mars?",
        "Tell me a joke",
        "Who will win the World Cup?",
    };

    var v1_0RefusalRate = EvaluateRefusalRate(PromptTemplates.V1_0_System, unknownQuestions);
    var v1_1RefusalRate = EvaluateRefusalRate(PromptTemplates.V1_1_System, unknownQuestions);

    v1_1RefusalRate.Should().BeGreaterOrEqualTo(v1_0RefusalRate,
        "newer prompt should refuse unknown questions at least as often");
}
```

## Prompt Composition Architecture

```
┌────────────────────────────────────────────┐
│           PromptBuilder                     │
│                                             │
│  BuildSystemPrompt()                        │
│    → Template: "You are an HR assistant..." │
│                                             │
│  BuildRagPrompt(context, question)          │
│    → System + Context + Question            │
│                                             │
│  BuildGreetingPrompt(name, dept, actions)   │
│    → System + Personalized greeting         │
│                                             │
│  BuildApprovalPrompt(request, policy)       │
│    → System + Policy context + Request      │
└────────────────────────────────────────────┘
```

## Prompt Testing Checklist

| Check | Test |
|-------|------|
| ✅ Role defined | System prompt states assistant's purpose |
| ✅ Boundaries set | Prompt says "only answer about company policies" |
| ✅ Citations required | Prompt says "cite the source document" |
| ✅ Hallucination guard | Prompt says "if you don't know, say so" |
| ✅ Tone specified | "Professional, helpful, concise" |
| ✅ Token budget | System prompt < 25% of context window |
| ✅ Variable escaping | Names with special chars don't break format |
| ✅ Language | Prompt is in the same language as the audience |
| ✅ No contradictory instructions | e.g., "be concise" vs "be thorough" |
| ✅ Fallback behavior | What to say when no context is found |

## CI Integration

```yaml
prompt-tests:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - run: dotnet test --filter "Category=Prompt"
    - name: Validate prompt changes
      run: |
        # Check if prompt templates changed
        git diff origin/main -- '**/Prompts/**' | \
          python scripts/validate-prompt-changes.py
```

## Related Files

- `src/AIHelpdesk.Application/Services/PromptBuilder.cs` — (to be built in Phase 4)
- `tests/AIHelpdesk.Tests/Services/PromptBuilderTests.cs` — (to be built)
- `tests/AI-specific-testing.md` — Overall AI testing strategy
- `tests/hallucination-testing.md` — Hallucination detection
- `tests/grounding-testing.md` — Grounding & retrieval quality
- `tests/prompt-injection-testing.md` — Injection resistance
