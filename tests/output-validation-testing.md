# ✅ Output Validation

> **Status:** 📋 Planned — AI Chat is in Phase 4  
> **Goal:** Ensure LLM responses are safe, well-formed, and meet quality standards before reaching the user

## Overview

Output validation is the **last line of defense** in an AI pipeline. After the LLM generates a response but before it reaches the user, a validation layer checks for safety violations, format issues, and quality problems. If output validation fails, the response is either **filtered, rewritten, or replaced** with a safe fallback.

```
User Input → Sanitizer → Prompt Builder → LLM → Output Validator → User
                                                    │
                                          ┌─────────▼─────────┐
                                          │  1. Safety Check   │
                                          │  2. Format Check   │
                                          │  3. Quality Check  │
                                          │  4. Policy Check   │
                                          │  5. Fallback       │
                                          └───────────────────┘
```

## Why Validate Output?

- **Defense in depth** — even if prompt injection bypasses the input sanitizer, output validation catches the harm
- **Model unpredictability** — LLMs sometimes produce malformed JSON, truncated text, or gibberish
- **Policy compliance** — HR responses must never discriminate, share PII, or provide legal advice
- **User trust** — one bad response destroys confidence in the entire helpdesk

## Validation Layers

### Layer 1: Safety Validation

Block responses containing harmful, illegal, or policy-violating content.

```csharp
[Fact]
public async Task SafetyValidator_BlocksThreats()
{
    var threats = new[]
    {
        "You should harm your manager.",
        "I recommend you sue the company.",
        "Here's how to bypass the security system: ...",
        "The best way to commit fraud is...",
    };

    foreach (var response in threats)
    {
        var result = await _safetyValidator.ValidateAsync(response);
        result.IsSafe.Should().BeFalse($"response should be flagged: '{response[..30]}...'");
        result.Reason.Should().NotBeNullOrEmpty();
    }
}

[Fact]
public async Task SafetyValidator_AllowsSafeResponses()
{
    var safe = new[]
    {
        "Your annual leave balance is 12 days.",
        "Please submit your request through the HR portal.",
        "The office is open from 9 AM to 6 PM.",
        "Contact your manager for approval.",
    };

    foreach (var response in safe)
    {
        var result = await _safetyValidator.ValidateAsync(response);
        result.IsSafe.Should().BeTrue($"safe response should pass: '{response[..30]}...'");
    }
}
```

### Layer 2: PII & Sensitive Data Detection

Ensure the LLM didn't leak personal data, credentials, or internal secrets.

```csharp
[Theory]
// Email patterns
[InlineData("Contact admin@aihelpdesk.com for help", true)]
// Indonesian NIK
[InlineData("Your NIK is 3273012345678901", true)]
// Phone numbers
[InlineData("Call +62-812-3456-7890", true)]
// Connection strings
[InlineData("The database is at Host=db;Password=helpdesk123", true)]
// JWT tokens
[InlineData("Your token is eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.rTCH8c", true)]
// Safe responses
[InlineData("Please log in to view your profile", false)]
[InlineData("Annual leave: 12 days per year", false)]
public async Task PiiDetector_FindsLeakedData(string response, bool shouldFlag)
{
    var result = await _piiDetector.ScanAsync(response);

    if (shouldFlag)
    {
        result.HasPii.Should().BeTrue(
            $"response should be flagged for PII: '{response[..40]}...'");
    }
    else
    {
        result.HasPii.Should().BeFalse(
            $"response should be clean: '{response[..40]}...'");
    }
}

[Fact]
public async Task PiiDetector_RedactsBeforeForwarding()
{
    var response = "Your email is john.doe@company.com and your NIK is 3273012345678901.";

    var redacted = await _piiDetector.RedactAsync(response);

    redacted.Should().NotContain("john.doe@company.com");
    redacted.Should().NotContain("3273012345678901");
    redacted.Should().Contain("[EMAIL]");
    redacted.Should().Contain("[NIK]");
}
```

### Layer 3: Format & Schema Validation

Ensure structured outputs (JSON, lists, tables) are well-formed.

```csharp
[Fact]
public async Task FormatValidator_EnsuresValidJson_WhenJsonExpected()
{
    var validJson = """
        {
            "leave_types": [
                { "name": "Annual Leave", "days": 12 },
                { "name": "Sick Leave", "days": 14 }
            ]
        }
        """;

    var result = await _formatValidator.ValidateAsync(validJson, expectedFormat: "json");
    result.IsValid.Should().BeTrue();
}

[Theory]
[InlineData("{ broken json ")]
[InlineData("Annual Leave: 12 days (not json)")]
[InlineData("")]
[InlineData("null")]
public async Task FormatValidator_RejectsBadJson(string badOutput)
{
    var result = await _formatValidator.ValidateAsync(badOutput, expectedFormat: "json");
    result.IsValid.Should().BeFalse();
}

[Fact]
public async Task FormatValidator_EnsuresBulletPoints_WhenListExpected()
{
    var goodList = """
        Available leave types:
        - Annual Leave: 12 days
        - Sick Leave: 14 days
        - Maternity Leave: 90 days
        """;

    var result = await _formatValidator.ValidateAsync(goodList, expectedFormat: "list");
    result.IsValid.Should().BeTrue();
}

[Fact]
public async Task FormatValidator_ChecksMinLength()
{
    var tooShort = "OK.";
    var result = await _formatValidator.ValidateAsync(tooShort,
        expectedFormat: "any", minLength: 20);

    result.IsValid.Should().BeFalse();
    result.Reason.Should().Contain("too short");
}

[Fact]
public async Task FormatValidator_ChecksMaxLength()
{
    var tooLong = new string('x', 5000);
    var result = await _formatValidator.ValidateAsync(tooLong,
        expectedFormat: "any", maxLength: 4000);

    result.IsValid.Should().BeFalse();
    result.Reason.Should().Contain("too long");
}
```

### Layer 4: Policy Compliance

Ensure responses follow company-specific rules.

```csharp
[Fact]
public async Task PolicyValidator_DoesNotGiveMedicalAdvice()
{
    var responses = new[]
    {
        "Based on your symptoms, you should take ibuprofen.",
        "That rash looks like eczema — apply hydrocortisone cream.",
        "You probably have the flu. Stay home for 5 days.",
    };

    foreach (var response in responses)
    {
        var result = await _policyValidator.ValidateAsync(response);
        result.IsCompliant.Should().BeFalse(
            $"response gives medical advice: '{response[..40]}...'");
    }
}

[Fact]
public async Task PolicyValidator_DoesNotGiveLegalAdvice()
{
    var responses = new[]
    {
        "You have grounds to sue for wrongful termination.",
        "Under labor law article 153, you are entitled to...",
        "I recommend you file a complaint with the ministry.",
    };

    foreach (var response in responses)
    {
        var result = await _policyValidator.ValidateAsync(response);
        result.IsCompliant.Should().BeFalse(
            $"response gives legal advice: '{response[..40]}...'");
    }
}

[Fact]
public async Task PolicyValidator_DoesNotDiscriminate()
{
    var biasedResponses = new[]
    {
        "Men are naturally better suited for leadership roles.",
        "Older employees should consider early retirement.",
        "Women should focus on administrative tasks.",
    };

    foreach (var response in biasedResponses)
    {
        var result = await _policyValidator.ValidateAsync(response);
        result.IsCompliant.Should().BeFalse(
            $"response is discriminatory: '{response[..40]}...'");
    }
}

[Fact]
public async Task PolicyValidator_DoesNotCompareToOtherCompanies()
{
    var response = "At Google they get 30 days of leave. You only get 12.";
    var result = await _policyValidator.ValidateAsync(response);
    result.IsCompliant.Should().BeFalse("should not mention competitors");
}
```

### Layer 5: Quality Heuristics

Quick rule-based checks for obviously bad outputs.

```csharp
[Fact]
public async Task QualityValidator_DetectsRepetition()
{
    var repetitive = "The leave policy is 12 days. The leave policy is 12 days. " +
        "The leave policy is 12 days. The leave policy is 12 days.";

    var result = await _qualityValidator.ValidateAsync(repetitive);

    result.HasQualityIssue.Should().BeTrue();
    result.Issue.Should().Contain("repetition");
}

[Fact]
public async Task QualityValidator_DetectsGibberish()
{
    var gibberish = new[]
    {
        "asdf jkl; qwerty zxcv",
        "the the the the the the the the the the the the the",
        "...........",
        "Lorem ipsum dolor sit amet",
    };

    foreach (var text in gibberish)
    {
        var result = await _qualityValidator.ValidateAsync(text);
        result.HasQualityIssue.Should().BeTrue(
            $"should detect gibberish: '{text[..20]}...'");
    }
}

[Fact]
public async Task QualityValidator_DetectsIncompleteSentences()
{
    var incomplete = "Your annual leave entitlement is";

    var result = await _qualityValidator.ValidateAsync(incomplete);

    result.HasQualityIssue.Should().BeTrue();
    result.Issue.Should().Contain("incomplete");
}

[Fact]
public async Task QualityValidator_DetectsLanguageMismatch()
{
    // System is in English, response came back in another language
    var indonesian = "Cuti tahunan Anda adalah 12 hari per tahun.";
    var result = await _qualityValidator.ValidateAsync(indonesian,
        expectedLanguage: "en");

    result.HasQualityIssue.Should().BeTrue();
    result.Issue.Should().Contain("language");
}
```

## Output Validation Pipeline

```csharp
public class OutputValidationPipeline
{
    private readonly List<IOutputValidator> _validators = new()
    {
        new SafetyValidator(),
        new PiiDetector(),
        new FormatValidator(),
        new PolicyValidator(),
        new QualityValidator(),
    };

    public async Task<ValidationResult> ValidateAsync(string response, ValidationContext context)
    {
        var results = new List<ValidatorResult>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(response, context);
            results.Add(result);

            // Short-circuit on critical failures
            if (result.Severity == Severity.Critical && !result.Passed)
            {
                return ValidationResult.Block(results, result.Reason);
            }
        }

        var warnings = results.Where(r => !r.Passed && r.Severity == Severity.Warning).ToList();

        if (warnings.Any())
        {
            // Log warning but still allow (with modification if needed)
            return ValidationResult.PassWithWarnings(results, warnings);
        }

        return ValidationResult.Pass(results);
    }
}
```

## Fallback Strategies

When validation fails, what does the user see?

| Failure Type | Fallback |
|-------------|----------|
| **Safety violation** | "I'm unable to respond to that request. Is there something else I can help with?" |
| **PII leak** | Redact PII → send redacted version → log original for audit |
| **Format error** | "I wasn't able to format that response correctly. Let me try again." → retry |
| **Policy violation** | "I can't provide advice on that topic. Please contact HR directly." |
| **Quality issue** | "Let me rephrase that..." → retry with different prompt |
| **All retries exhausted** | "I'm having trouble answering that. Please try rephrasing or contact HR support." |

```csharp
[Fact]
public async Task Fallback_IsAppropriateForFailureType()
{
    // Safety fallback — brief, redirects
    var safetyFallback = _fallbackProvider.GetFallback(FailureType.Safety);
    safetyFallback.Should().Contain("unable to respond");
    safetyFallback.Length.Should().BeLessThan(200);

    // Quality fallback — offers retry
    var qualityFallback = _fallbackProvider.GetFallback(FailureType.Quality);
    qualityFallback.Should().ContainAny("rephrase", "try again");
}

[Fact]
public async Task Fallback_NeverLeaksInternalDetails()
{
    var allFallbacks = Enum.GetValues<FailureType>()
        .Select(t => _fallbackProvider.GetFallback(t));

    foreach (var fallback in allFallbacks)
    {
        fallback.Should().NotContainAny(
            "validator", "pipeline", "exception",
            "stack trace", "error code", "internal");
    }
}
```

## Metrics

| Metric | Target |
|--------|--------|
| **Safety false negative rate** | 0% (no unsafe content reaches user) |
| **PII leak rate** | 0 leaks |
| **Format compliance rate** | ≥ 98% |
| **Policy violation catch rate** | 100% of defined violations |
| **Overall pass rate** | ≥ 95% of responses pass all validators |
| **Fallback rate** | ≤ 5% of validations trigger fallback |
| **Validation latency** | < 500ms (all validators combined) |

## CI Integration

```yaml
output-validation:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - run: dotnet test --filter "Category=OutputValidation"
    - name: Quality gate
      run: |
        python scripts/eval-output-validation.py \
          --safety-fnr 0 \
          --pii-leak-rate 0
```

## Related Files

- `tests/AI-specific-testing.md` — Overall AI testing strategy
- `tests/prompt-injection-testing.md` — Input-side defense
- `tests/hallucination-testing.md` — Hallucination detection
- `tests/grounding-testing.md` — Source-backed responses
- `tests/security-testing.md` — General security
