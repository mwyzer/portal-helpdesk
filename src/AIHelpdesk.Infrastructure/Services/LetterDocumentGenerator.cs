using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace AIHelpdesk.Infrastructure.Services;

/// <summary>
/// Renders a generated HR/Secretary letter (title, letter number, date, body) to PDF or DOCX bytes.
/// </summary>
public static class LetterDocumentGenerator
{
    private const double PageMargin = 40;

    public static byte[] GeneratePdf(string title, string? letterNumber, DateTime date, string body)
    {
        using var document = new PdfDocument();
        var titleFont = new XFont("Arial", 14, XFontStyle.Bold);
        var metaFont = new XFont("Arial", 10, XFontStyle.Regular);
        var bodyFont = new XFont("Arial", 11, XFontStyle.Regular);

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        double contentWidth = page.Width - 2 * PageMargin;
        double y = PageMargin;

        y = DrawWrapped(ref page, ref gfx, document, title, titleFont, contentWidth, y) + 6;
        if (!string.IsNullOrWhiteSpace(letterNumber))
            y = DrawLine(ref page, ref gfx, document, $"No: {letterNumber}", metaFont, contentWidth, y);
        y = DrawLine(ref page, ref gfx, document, date.ToString("dd MMMM yyyy"), metaFont, contentWidth, y) + 16;

        foreach (var paragraph in body.Replace("\r\n", "\n").Split('\n'))
        {
            y = DrawWrapped(ref page, ref gfx, document, paragraph, bodyFont, contentWidth, y) + 8;
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static double DrawLine(ref PdfPage page, ref XGraphics gfx, PdfDocument document, string text, XFont font, double width, double y)
    {
        EnsureSpace(ref page, ref gfx, document, ref y, font.Height);
        gfx.DrawString(text, font, XBrushes.Black, new XRect(PageMargin, y, width, font.Height + 4), XStringFormats.TopLeft);
        return y + font.Height + 4;
    }

    private static double DrawWrapped(ref PdfPage page, ref XGraphics gfx, PdfDocument document, string text, XFont font, double width, double y)
    {
        if (string.IsNullOrWhiteSpace(text)) return y + font.Height;

        foreach (var line in WrapText(gfx, text, font, width))
        {
            EnsureSpace(ref page, ref gfx, document, ref y, font.Height);
            gfx.DrawString(line, font, XBrushes.Black, new XRect(PageMargin, y, width, font.Height + 4), XStringFormats.TopLeft);
            y += font.Height + 4;
        }

        return y;
    }

    private static void EnsureSpace(ref PdfPage page, ref XGraphics gfx, PdfDocument document, ref double y, double lineHeight)
    {
        if (y + lineHeight <= page.Height - PageMargin) return;

        page = document.AddPage();
        gfx = XGraphics.FromPdfPage(page);
        y = PageMargin;
    }

    private static IEnumerable<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;

        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (gfx.MeasureString(candidate, font).Width > maxWidth && line.Length > 0)
            {
                yield return line;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        if (line.Length > 0) yield return line;
    }

    public static byte[] GenerateDocx(string title, string? letterNumber, DateTime date, string body)
    {
        using var stream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var docBody = mainPart.Document.AppendChild(new Body());

            docBody.AppendChild(new Paragraph(new Run(
                new RunProperties(new Bold(), new FontSize { Val = "28" }),
                new Text(title))));

            if (!string.IsNullOrWhiteSpace(letterNumber))
                docBody.AppendChild(new Paragraph(new Run(new Text($"No: {letterNumber}"))));

            docBody.AppendChild(new Paragraph(new Run(new Text(date.ToString("dd MMMM yyyy")))));
            docBody.AppendChild(new Paragraph()); // spacer

            foreach (var paragraph in body.Replace("\r\n", "\n").Split('\n'))
            {
                docBody.AppendChild(new Paragraph(new Run(
                    new Text(paragraph) { Space = SpaceProcessingModeValues.Preserve })));
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
}
