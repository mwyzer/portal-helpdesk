using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Excel;
using ClosedXML.Excel;

namespace AIHelpdesk.Infrastructure.Services;

/// <summary>
/// Generic Excel import / export / template service backed by ClosedXML.
/// </summary>
public class ExcelService : IExcelService
{
    // ── Import ──────────────────────────────────────

    public async Task<ExcelImportResult<T>> ImportFromExcelAsync<T>(
        Stream fileStream,
        Func<IReadOnlyDictionary<string, string>, int, (bool Success, T? Item, string? Error)> rowMapper,
        Func<T, Task<bool>>? duplicateDetector = null,
        int sheetIndex = 1)
    {
        var errors = new List<ExcelImportError>();
        var importedItems = new List<T>();
        var successCount = 0;
        var totalRows = 0;

        using var workbook = new XLWorkbook(fileStream);

        if (sheetIndex < 1 || sheetIndex > workbook.Worksheets.Count)
        {
            errors.Add(new ExcelImportError(0, string.Empty,
                $"Worksheet index {sheetIndex} is out of range (available: 1–{workbook.Worksheets.Count})."));
            return new ExcelImportResult<T>(0, 0, errors.Count, errors, importedItems);
        }

        var worksheet = workbook.Worksheet(sheetIndex);
        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1); // skip header row
        if (rows == null)
            return new ExcelImportResult<T>(0, 0, 0, errors, importedItems);

        // Build a header map: column name → 1-based index
        var headerRow = worksheet.RangeUsed()!.RowsUsed().First();
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int col = 1; col <= headerRow.CellCount(); col++)
        {
            var header = headerRow.Cell(col).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(header))
                headerMap[header] = col;
        }

        foreach (var row in rows)
        {
            totalRows++;
            var rowNumber = row.RowNumber();

            try
            {
                // Build a cell-value dictionary keyed by header name
                var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (header, colIndex) in headerMap)
                {
                    rowData[header] = row.Cell(colIndex).GetString()?.Trim() ?? string.Empty;
                }

                var (success, item, error) = rowMapper(rowData, rowNumber);
                if (!success || item == null)
                {
                    errors.Add(new ExcelImportError(rowNumber, string.Empty, error ?? "Row mapping failed."));
                    continue;
                }

                // Duplicate check
                if (duplicateDetector != null && await duplicateDetector(item))
                {
                    errors.Add(new ExcelImportError(rowNumber, string.Empty, "Duplicate row detected."));
                    continue;
                }

                importedItems.Add(item);
                successCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ExcelImportError(rowNumber, string.Empty, ex.Message));
            }
        }

        return new ExcelImportResult<T>(
            totalRows, successCount, errors.Count, errors, importedItems);
    }

    // ── Export ──────────────────────────────────────

    public Task<byte[]> ExportToExcelAsync<T>(
        IReadOnlyList<T> items,
        ExcelExportConfig config,
        Func<T, ExcelColumnDefinition, object?> valueSelector)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(config.SheetName);

        // Header row with styling
        for (int i = 0; i < config.Columns.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = config.Columns[i].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6"); // primary blue
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data rows
        for (int rowIdx = 0; rowIdx < items.Count; rowIdx++)
        {
            for (int colIdx = 0; colIdx < config.Columns.Count; colIdx++)
            {
                var value = valueSelector(items[rowIdx], config.Columns[colIdx]);
                var cell = worksheet.Cell(rowIdx + 2, colIdx + 1);
                SetCellValue(cell, value, config.Columns[colIdx].Format);
            }
        }

        // Auto-fit columns, but respect explicit widths
        worksheet.Columns().AdjustToContents(1, items.Count + 1);
        for (int i = 0; i < config.Columns.Count; i++)
        {
            if (config.Columns[i].Width.HasValue)
                worksheet.Column(i + 1).Width = config.Columns[i].Width.Value;
        }

        // Freeze header row
        worksheet.SheetView.FreezeRows(1);

        // Auto-filter on data range
        if (items.Count > 0)
        {
            var range = worksheet.Range(1, 1, items.Count + 1, config.Columns.Count);
            range.SetAutoFilter();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    // ── Template generation ─────────────────────────

    public Task<byte[]> GenerateTemplateAsync(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<string>? sampleRow = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Header row
        for (int i = 0; i < headers.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Optional sample row (italic, grey background)
        if (sampleRow != null && sampleRow.Count > 0)
        {
            for (int i = 0; i < sampleRow.Count && i < headers.Count; i++)
            {
                var cell = worksheet.Cell(2, i + 1);
                cell.Value = sampleRow[i];
                cell.Style.Font.Italic = true;
                cell.Style.Font.FontColor = XLColor.Gray;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
            }

            // Add a note row below the sample
            worksheet.Cell(3, 1).Value = "⬆ Delete this sample row before importing. All columns are required.";
            worksheet.Cell(3, 1).Style.Font.Italic = true;
            worksheet.Cell(3, 1).Style.Font.FontColor = XLColor.Red;
            if (headers.Count > 1)
                worksheet.Range(3, 1, 3, headers.Count).Merge();
        }

        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    // ── Helpers ─────────────────────────────────────

    private static void SetCellValue(IXLCell cell, object? value, string? format)
    {
        switch (value)
        {
            case null:
                cell.Value = string.Empty;
                break;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = format ?? "yyyy-MM-dd";
                break;
            case DateTimeOffset dto:
                cell.Value = dto.DateTime;
                cell.Style.DateFormat.Format = format ?? "yyyy-MM-dd";
                break;
            case DateOnly d:
                cell.Value = d.ToDateTime(TimeOnly.MinValue);
                cell.Style.DateFormat.Format = format ?? "yyyy-MM-dd";
                break;
            case int i:
                cell.Value = i;
                if (!string.IsNullOrWhiteSpace(format))
                    cell.Style.NumberFormat.Format = format;
                break;
            case long l:
                cell.Value = l;
                if (!string.IsNullOrWhiteSpace(format))
                    cell.Style.NumberFormat.Format = format;
                break;
            case decimal dec:
                cell.Value = (double)dec;
                cell.Style.NumberFormat.Format = format ?? "#,##0.00";
                break;
            case double dbl:
                cell.Value = dbl;
                if (!string.IsNullOrWhiteSpace(format))
                    cell.Style.NumberFormat.Format = format;
                break;
            case bool b:
                cell.Value = b ? "Yes" : "No";
                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }
}
