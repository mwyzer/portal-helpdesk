using AIHelpdesk.Contracts.Excel;

namespace AIHelpdesk.Application.Interfaces;

/// <summary>
/// Generic Excel import / export / template service.
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Imports rows from an Excel stream and maps each valid row to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Domain DTO produced per row.</typeparam>
    /// <param name="fileStream">The uploaded .xlsx stream.</param>
    /// <param name="rowMapper">
    ///   Receives a read-only dictionary of column-header → cell-string-value and the 1-based row number.
    ///   Return (true, item, null) for success, or (false, default, "error message") for a row error.
    /// </param>
    /// <param name="duplicateDetector">
    ///   Optional — receives every successfully-mapped T; return true when a duplicate is found.
    /// </param>
    /// <param name="sheetIndex">1-based worksheet index. Defaults to 1.</param>
    Task<ExcelImportResult<T>> ImportFromExcelAsync<T>(
        Stream fileStream,
        Func<IReadOnlyDictionary<string, string>, int, (bool Success, T? Item, string? Error)> rowMapper,
        Func<T, Task<bool>>? duplicateDetector = null,
        int sheetIndex = 1);

    /// <summary>
    /// Exports a collection of items to an Excel byte array using the supplied column definitions.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="items">The data rows.</param>
    /// <param name="config">Sheet name and column definitions.</param>
    /// <param name="valueSelector">Selector that returns the value for a given column definition.</param>
    Task<byte[]> ExportToExcelAsync<T>(
        IReadOnlyList<T> items,
        ExcelExportConfig config,
        Func<T, ExcelColumnDefinition, object?> valueSelector);

    /// <summary>
    /// Generates a blank template workbook with bold headers (and optional sample row) for use as an import guide.
    /// </summary>
    /// <param name="sheetName">Worksheet name.</param>
    /// <param name="headers">Header labels for each column.</param>
    /// <param name="sampleRow">Optional sample data (strings) to hint at expected formats.</param>
    Task<byte[]> GenerateTemplateAsync(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<string>? sampleRow = null);
}
