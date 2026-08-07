using System.Text.Json;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Knowledge;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIHelpdesk.Infrastructure.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly ApplicationDbContext _context;
    private readonly IAIService _ai;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KnowledgeBaseService> _logger;
    private readonly string _uploadPath;

    // Shown to any authenticated user via GET /api/knowledge-documents (no role restriction) --
    // the underlying exception (AI provider error bodies, file paths, etc.) goes to _logger
    // instead, where only operators with log/dashboard access can see it.
    private const string GenericIndexingFailureMessage =
        "Indexing failed due to an AI provider or server error. Contact an administrator for details.";

    public KnowledgeBaseService(ApplicationDbContext context, IAIService ai, IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<KnowledgeBaseService> logger)
    {
        _context = context;
        _ai = ai;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _uploadPath = configuration["KnowledgeBase:UploadPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads", "knowledge");
        Directory.CreateDirectory(_uploadPath);
    }

    public async Task<PagedResult<KnowledgeDocumentResponse>> GetDocumentsAsync(int page, int pageSize, string? status)
    {
        var query = _context.KnowledgeDocuments.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<KnowledgeDocumentStatus>(status, true, out var s))
            query = query.Where(d => d.Status == s);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new KnowledgeDocumentResponse(
                d.Id, d.Title, d.FileName, d.FileType, d.FileSize,
                d.Status.ToString(), d.PageCount, d.ChunkCount, d.ErrorMessage, d.CreatedAt))
            .ToListAsync();

        return new PagedResult<KnowledgeDocumentResponse>(items, total, page, pageSize);
    }

    public async Task<KnowledgeDocumentDetailResponse> GetDocumentAsync(Guid id)
    {
        var doc = await _context.KnowledgeDocuments
            .Include(d => d.Chunks.OrderBy(c => c.ChunkIndex).Take(5))
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException("Document not found");

        return new KnowledgeDocumentDetailResponse(
            doc.Id, doc.Title, doc.FileName, doc.FileType, doc.ContentType, doc.FileSize,
            doc.Status.ToString(), doc.PageCount, doc.ChunkCount, doc.ErrorMessage,
            doc.Chunks.Select(c => new KnowledgeSearchResult(doc.Id, doc.Title, c.Id, c.Content[..Math.Min(200, c.Content.Length)], 0)).ToList(),
            doc.CreatedAt, doc.UpdatedAt);
    }

    public async Task<KnowledgeDocumentResponse> UploadDocumentAsync(Guid userId, string title, string fileName, Stream fileStream, string contentType, Guid? departmentId = null)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".docx" && ext != ".txt")
            throw new InvalidOperationException("Only PDF, DOCX, and TXT files are supported");

        var fileId = Guid.NewGuid();
        var filePath = Path.Combine(_uploadPath, $"{fileId}{ext}");
        await using var fs = File.Create(filePath);
        await fileStream.CopyToAsync(fs);

        var doc = new KnowledgeDocument
        {
            Title = title,
            FileName = fileName,
            FilePath = filePath,
            FileType = ext,
            ContentType = contentType,
            FileSize = new FileInfo(filePath).Length,
            Status = KnowledgeDocumentStatus.Pending,
            CreatedBy = userId,
            DepartmentId = departmentId
        };

        _context.KnowledgeDocuments.Add(doc);
        await _context.SaveChangesAsync();

        // Auto-index in the background. This outlives the HTTP request (and its DI scope +
        // DbContext), so it must resolve its own scope rather than capturing _context/_ai --
        // using the request-scoped instances here throws ObjectDisposedException once the
        // response completes, and previously did so silently (Task.Run has no observer, and the
        // catch block's own save attempt hit the same disposed context), leaving documents stuck
        // at "Indexing" forever with no error ever recorded.
        var docId = doc.Id;
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var scopedAi = scope.ServiceProvider.GetRequiredService<IAIService>();
            var scopedDoc = await scopedContext.KnowledgeDocuments.FindAsync(docId);
            if (scopedDoc == null) return;

            try
            {
                await IndexDocumentInternalAsync(scopedDoc, scopedContext, scopedAi);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-indexing failed for KnowledgeDocument {DocumentId}", docId);
                scopedDoc.Status = KnowledgeDocumentStatus.Failed;
                scopedDoc.ErrorMessage = GenericIndexingFailureMessage;
                scopedDoc.UpdatedAt = DateTime.UtcNow;
                await scopedContext.SaveChangesAsync();
            }
        });

        return Map(doc);
    }

    public async Task DeleteDocumentAsync(Guid id)
    {
        var doc = await _context.KnowledgeDocuments
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException("Document not found");

        // Delete file
        if (File.Exists(doc.FilePath)) File.Delete(doc.FilePath);

        // Delete chunks (cascade)
        _context.KnowledgeChunks.RemoveRange(doc.Chunks);
        doc.IsDeleted = true;
        doc.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<KnowledgeDocumentResponse> IndexDocumentAsync(Guid id)
    {
        var doc = await _context.KnowledgeDocuments.FindAsync(id)
            ?? throw new KeyNotFoundException("Document not found");

        try
        {
            await IndexDocumentInternalAsync(doc, _context, _ai);
        }
        catch (Exception ex)
        {
            // Same "leave the document stuck at Indexing" failure mode as the auto-index path
            // below (just without the disposed-context race, since this one runs synchronously
            // within the request) -- record the failure instead of letting it bubble as a bare 500.
            _logger.LogError(ex, "Manual re-index failed for KnowledgeDocument {DocumentId}", id);
            doc.Status = KnowledgeDocumentStatus.Failed;
            doc.ErrorMessage = GenericIndexingFailureMessage;
            doc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Map(doc);
    }

    public async Task<List<KnowledgeSearchResult>> SearchAsync(string query, int topK, Guid? requesterDepartmentId = null)
    {
        try
        {
            var queryEmbedding = await _ai.GenerateEmbeddingAsync(query);
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(queryEmbedding);

            // hnsw.ef_search/iterative_scan only need to be set for this query, and SET LOCAL only
            // holds for the current transaction -- wrap explicitly so it doesn't leak onto the next
            // query run on this pooled connection.
            await using var tx = await _context.Database.BeginTransactionAsync();
            await _context.Database.ExecuteSqlRawAsync("SET LOCAL hnsw.ef_search = 100;");
            // Guards against selective department filters returning fewer than topK rows, a known
            // HNSW behavior where the ANN search space is exhausted before the filter is satisfied
            // (pgvector >=0.8; see docker/postgres/Dockerfile for the installed version).
            await _context.Database.ExecuteSqlRawAsync("SET LOCAL hnsw.iterative_scan = relaxed_order;");

            var vectorResults = await _context.Database
                .SqlQueryRaw<KnowledgeSearchResult>(
                    @"SELECT kc.""Id"" AS ""ChunkId"", kd.""Id"" AS ""DocumentId"", kd.""Title"" AS ""DocumentTitle"",
                           LEFT(kc.""Content"", 300) AS ""Content"",
                           (1 - (kc.""Embedding"" <=> {0}::vector)) AS ""Relevance""
                    FROM ""KnowledgeChunks"" kc
                    INNER JOIN ""KnowledgeDocuments"" kd ON kc.""DocumentId"" = kd.""Id""
                    WHERE NOT kc.""IsDeleted"" AND NOT kd.""IsDeleted"" AND kc.""Embedding"" IS NOT NULL
                      AND (kc.""DepartmentId"" IS NULL OR kc.""DepartmentId"" = {2})
                    ORDER BY kc.""Embedding"" <=> {0}::vector
                    LIMIT {1}", embeddingJson, topK, requesterDepartmentId)
                .ToListAsync();

            await tx.CommitAsync();
            return vectorResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector search failed, falling back to substring search");
        }

        // Last-resort fallback if the AI provider or pgvector itself is unreachable -- department
        // scoping still applies so this can't be used to bypass the guardrail above.
        var queryLower = query.ToLowerInvariant();
        return await _context.KnowledgeChunks
            .Where(c => c.Content.ToLower().Contains(queryLower)
                && (c.DepartmentId == null || c.DepartmentId == requesterDepartmentId))
            .Take(topK)
            .Select(c => new KnowledgeSearchResult(
                c.Document.Id,
                c.Document.Title,
                c.Id,
                c.Content.Length > 300 ? c.Content.Substring(0, 300) + "..." : c.Content,
                0.5))
            .ToListAsync();
    }

    private async Task IndexDocumentInternalAsync(KnowledgeDocument doc, ApplicationDbContext context, IAIService ai)
    {
        doc.Status = KnowledgeDocumentStatus.Indexing;
        doc.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Extract text
        var text = doc.FileType switch
        {
            ".txt" => await File.ReadAllTextAsync(doc.FilePath),
            ".pdf" => await ExtractPdfText(doc.FilePath),
            ".docx" => await ExtractDocxText(doc.FilePath),
            _ => throw new InvalidOperationException($"Unsupported file type: {doc.FileType}")
        };

        // Remove existing chunks
        var oldChunks = await context.KnowledgeChunks.Where(c => c.DocumentId == doc.Id).ToListAsync();
        context.KnowledgeChunks.RemoveRange(oldChunks);

        // Chunk text (500 char chunks with 100 char overlap)
        var chunks = ChunkText(text, 500, 100);

        // Generate embeddings and save chunks
        int index = 0;
        foreach (var chunk in chunks)
        {
            var embedding = await ai.GenerateEmbeddingAsync(chunk);
            context.KnowledgeChunks.Add(new KnowledgeChunk
            {
                DocumentId = doc.Id,
                Content = chunk,
                ChunkIndex = index++,
                EmbeddingJson = JsonSerializer.Serialize(embedding.ToArray()),
                DepartmentId = doc.DepartmentId
            });
        }

        doc.Status = KnowledgeDocumentStatus.Ready;
        doc.ChunkCount = chunks.Count;
        doc.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // "Embedding" is a native pgvector column with no EF-mapped CLR property (Npgsql's default
        // provider doesn't understand vector without the Pgvector.EntityFrameworkCore package), so
        // it's populated via raw SQL from the EmbeddingJson just written above rather than through
        // the tracked entities. Raw SQL only works against a relational provider, so skip it under
        // the InMemory provider used by unit tests.
        if (context.Database.IsRelational())
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE ""KnowledgeChunks"" SET ""Embedding"" = ""EmbeddingJson""::vector
                   WHERE ""DocumentId"" = {doc.Id} AND ""EmbeddingJson"" IS NOT NULL AND ""EmbeddingJson"" != '[]';");
        }
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text)) return chunks;

        int start = 0;
        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);
            // Try to break at sentence boundary
            if (end < text.Length)
            {
                var lastPeriod = text.LastIndexOf('.', end, end - start);
                if (lastPeriod > start + chunkSize / 2)
                    end = lastPeriod + 1;
            }
            chunks.Add(text[start..end].Trim());

            if (end >= text.Length) break; // reached the end of the text — stop

            var next = end - overlap;
            start = next > start ? next : start + 1; // guarantee forward progress even if overlap >= chunk length
        }
        return chunks;
    }

    private static async Task<string> ExtractPdfText(string filePath)
    {
        // Simple PDF text extraction: search for text between stream/endstream and decode
        // In production, use PdfPig or PdfSharp
        var bytes = await File.ReadAllBytesAsync(filePath);
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        // Very basic extraction — extract readable text segments
        var result = new System.Text.StringBuilder();
        bool inText = false;
        foreach (var line in text.Split('\n'))
        {
            if (line.Contains("BT")) inText = true;
            if (inText && line.Contains("Tj"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"\((.*?)\)\s*Tj");
                if (match.Success) result.AppendLine(match.Groups[1].Value);
            }
            if (line.Contains("ET")) inText = false;
        }
        return result.Length > 0 ? result.ToString() : $"PDF content (binary). Install PdfPig for full extraction.\nFile: {filePath}";
    }

    private static async Task<string> ExtractDocxText(string filePath)
    {
        // Basic DOCX extraction: read zip & parse document.xml
        // In production, use DocumentFormat.OpenXml
        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
            var entry = archive.GetEntry("word/document.xml");
            if (entry == null) return "No document.xml found in DOCX";

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var xml = await reader.ReadToEndAsync();

            // Strip XML tags
            var result = System.Text.RegularExpressions.Regex.Replace(xml, "<[^>]+>", " ");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
            return result;
        }
        catch
        {
            return $"DOCX content (binary). Install DocumentFormat.OpenXml for full extraction.\nFile: {filePath}";
        }
    }

    private static KnowledgeDocumentResponse Map(KnowledgeDocument d) => new(
        d.Id, d.Title, d.FileName, d.FileType, d.FileSize,
        d.Status.ToString(), d.PageCount, d.ChunkCount, d.ErrorMessage, d.CreatedAt);
}
