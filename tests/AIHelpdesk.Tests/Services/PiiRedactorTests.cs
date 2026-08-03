using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;

namespace AIHelpdesk.Tests.Services;

public class PiiRedactorTests
{
    [Fact]
    public void Redact_ShouldRedactEmailAddresses()
    {
        var result = PiiRedactor.Redact("Contact John at john.doe@company.com for details.");

        result.Should().NotContain("john.doe@company.com");
        result.Should().Contain("[REDACTED-EMAIL]");
    }

    [Fact]
    public void Redact_ShouldRedactIndonesianNIK()
    {
        var result = PiiRedactor.Redact("Employee NIK is 3201234567890123 on file.");

        result.Should().NotContain("3201234567890123");
        result.Should().Contain("[REDACTED-ID]");
    }

    [Fact]
    public void Redact_ShouldRedactCreditCardNumbers()
    {
        var result = PiiRedactor.Redact("Card on file: 4111 1111 1111 1111.");

        result.Should().NotContain("4111 1111 1111 1111");
        result.Should().Contain("[REDACTED-CARD]");
    }

    [Fact]
    public void Redact_ShouldRedactIndonesianPhoneNumbers()
    {
        var result = PiiRedactor.Redact("Reach the employee at 081234567890 anytime.");

        result.Should().NotContain("081234567890");
        result.Should().Contain("[REDACTED-PHONE]");
    }

    [Fact]
    public void Redact_ShouldRedactInternationalFormatIndonesianPhoneNumbers()
    {
        var result = PiiRedactor.Redact("International: +6281234567890.");

        result.Should().NotContain("81234567890");
        result.Should().Contain("[REDACTED-PHONE]");
    }

    [Fact]
    public void Redact_ShouldLeaveNormalTextUnchanged()
    {
        const string text = "The annual leave policy allows 12 days per year for all employees.";

        var result = PiiRedactor.Redact(text);

        result.Should().Be(text);
    }

    [Fact]
    public void Redact_ShouldHandleNullAndEmpty()
    {
        PiiRedactor.Redact(null).Should().Be(string.Empty);
        PiiRedactor.Redact(string.Empty).Should().Be(string.Empty);
    }

    [Fact]
    public void Redact_ShouldRedactMultiplePatternsInSameText()
    {
        var result = PiiRedactor.Redact("Email jane@test.com or call 081234567890 for NIK 3201234567890123.");

        result.Should().Contain("[REDACTED-EMAIL]");
        result.Should().Contain("[REDACTED-PHONE]");
        result.Should().Contain("[REDACTED-ID]");
    }
}
