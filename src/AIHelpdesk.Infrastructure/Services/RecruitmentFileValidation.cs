namespace AIHelpdesk.Infrastructure.Services;

/// <summary>
/// Shared upload validation for recruitment documents (CVs, candidate self-uploads) --
/// used by both CandidateService (staff upload) and CandidatePortalService (candidate
/// self-upload) so the two paths can't silently drift apart.
/// </summary>
public static class RecruitmentFileValidation
{
    public static readonly HashSet<string> AllowedDocumentExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx" };

    public const long MaxDocumentSizeBytes = 5 * 1024 * 1024; // 5 MB

    public static void EnsureValid(string fileName, Stream fileStream)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedDocumentExtensions.Contains(extension))
            throw new InvalidOperationException("Only PDF and DOCX files are allowed");

        if (fileStream.CanSeek && fileStream.Length > MaxDocumentSizeBytes)
            throw new InvalidOperationException("File exceeds the maximum allowed size of 5 MB");
    }
}
