using System.Text.RegularExpressions;

namespace AIHelpdesk.Infrastructure.Services;

/// <summary>
/// Best-effort regex-based redaction of common PII patterns from text before it is sent to an
/// external AI provider. Not a guarantee of complete PII removal — a defense-in-depth measure,
/// not a substitute for restricting what goes into the knowledge base in the first place.
/// </summary>
public static partial class PiiRedactor
{
    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

        // Order matters: phone numbers (10-13 digits) are a subset of the credit-card digit-count
        // range (13-19), so phones must be redacted first or the credit-card pattern swallows them.
        text = EmailRegex().Replace(text, "[REDACTED-EMAIL]");
        text = NikRegex().Replace(text, "[REDACTED-ID]");
        text = IndonesianPhoneRegex().Replace(text, "[REDACTED-PHONE]");
        text = CreditCardRegex().Replace(text, "[REDACTED-CARD]");

        return text;
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex EmailRegex();

    // Indonesian NIK: exactly 16 consecutive digits.
    [GeneratedRegex(@"(?<!\d)\d{16}(?!\d)")]
    private static partial Regex NikRegex();

    // Credit/debit card numbers: 13-19 digits, optionally grouped in blocks with spaces or dashes.
    [GeneratedRegex(@"(?<!\d)(?:\d[ -]?){13,19}(?<=\d)")]
    private static partial Regex CreditCardRegex();

    // Indonesian mobile numbers: 08xxxxxxxxx or +628xxxxxxxxx (8-11 digits after the 8).
    [GeneratedRegex(@"(?<!\d)(?:\+?62|0)8\d{8,11}(?!\d)")]
    private static partial Regex IndonesianPhoneRegex();
}
