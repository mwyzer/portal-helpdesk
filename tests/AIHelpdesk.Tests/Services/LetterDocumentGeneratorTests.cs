using System.Text;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;

namespace AIHelpdesk.Tests.Services;

public class LetterDocumentGeneratorTests
{
    [Fact]
    public void GeneratePdf_ShouldProduceValidPdfBytes()
    {
        var bytes = LetterDocumentGenerator.GeneratePdf(
            "Surat Keterangan Kerja", "001/LL/MGR/2026", new DateTime(2026, 8, 4), "This is the letter body.");

        bytes.Should().NotBeEmpty();
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void GeneratePdf_ShouldHandleLongBody_AcrossMultiplePages()
    {
        var longBody = string.Join("\n", Enumerable.Repeat(
            "This is a long paragraph that should wrap across multiple lines and eventually force a page break in the generated PDF document.",
            60));

        var act = () => LetterDocumentGenerator.GeneratePdf("Title", "001/X/2026", DateTime.UtcNow, longBody);

        act.Should().NotThrow();
    }

    [Fact]
    public void GeneratePdf_ShouldNotThrow_WhenLetterNumberIsNull()
    {
        var act = () => LetterDocumentGenerator.GeneratePdf("Title", null, DateTime.UtcNow, "Body text.");

        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateDocx_ShouldProduceValidZipBasedDocx()
    {
        var bytes = LetterDocumentGenerator.GenerateDocx(
            "Surat Keterangan Kerja", "001/LL/MGR/2026", new DateTime(2026, 8, 4), "This is the letter body.");

        bytes.Should().NotBeEmpty();
        Encoding.ASCII.GetString(bytes, 0, 2).Should().Be("PK"); // DOCX is a zip archive
    }

    [Fact]
    public void GenerateDocx_ShouldNotThrow_WhenLetterNumberIsNull()
    {
        var act = () => LetterDocumentGenerator.GenerateDocx("Title", null, DateTime.UtcNow, "Body text.");

        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateDocx_ShouldHandleMultiParagraphBody()
    {
        var body = "First paragraph.\nSecond paragraph.\nThird paragraph.";

        var bytes = LetterDocumentGenerator.GenerateDocx("Title", "001/X/2026", DateTime.UtcNow, body);

        bytes.Should().NotBeEmpty();
    }
}
