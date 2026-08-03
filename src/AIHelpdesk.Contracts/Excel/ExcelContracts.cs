namespace AIHelpdesk.Contracts.Excel;

/// <summary>
/// Result of an Excel import operation.
/// </summary>
public record ExcelImportResult<T>(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IList<ExcelImportError> Errors,
    IList<T> ImportedItems
);

/// <summary>
/// A single row-level import error.
/// </summary>
public record ExcelImportError(
    int Row,
    string Column,
    string Message
);

/// <summary>
/// Column definition for Excel export operations.
/// </summary>
public record ExcelColumnDefinition(
    string Header,
    string PropertyName,
    int? Width = null,
    string? Format = null
);

/// <summary>
/// Configuration for an Excel export operation.
/// </summary>
public record ExcelExportConfig(
    string SheetName,
    IReadOnlyList<ExcelColumnDefinition> Columns
);
