using System.Text.Json;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Knowledge;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AIHelpdesk.Infrastructure.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly ApplicationDbContext _context;
    private readonly IAIService _ai;
    private readonly string _uploadPath;

    public KnowledgeBaseService(ApplicationDbContext context, IAIService ai, IConfiguration configuration)
    {
        _context = context;
        _ai = ai;
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

    public async Task<KnowledgeDocumentResponse> UploadDocumentAsync(Guid userId, string title, string fileName, Stream fileStream, string contentType)
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
            CreatedBy = userId
        };

        _context.KnowledgeDocuments.Add(doc);
        await _context.SaveChangesAsync();

        // Auto-index
        _ = Task.Run(async () =>
        {
            try
            {
                await IndexDocumentInternalAsync(doc);
            }
            catch (Exception ex)
            {
                doc.Status = KnowledgeDocumentStatus.Failed;
                doc.ErrorMessage = ex.Message;
                doc.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
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
        await IndexDocumentInternalAsync(doc);
        return Map(doc);
    }

    public async Task<List<KnowledgeSearchResult>> SearchAsync(string query, int topK)
    {
        // For now, use a simple text-based search since vector column is not in EF model.
        // Full vector search can be enabled after pgvector extension is set up in production.
        var queryLower = query.ToLowerInvariant();
        var results = await _context.KnowledgeChunks
            .Where(c => c.Content.ToLower().Contains(queryLower))
            .Take(topK)
            .Select(c => new KnowledgeSearchResult(
                c.Document.Id,
                c.Document.Title,
                c.Id,
                c.Content.Length > 300 ? c.Content.Substring(0, 300) + "..." : c.Content,
                0.5))
            .ToListAsync();

        // If AI embeddings are configured, do proper vector search
        try
        {
            var queryEmbedding = await _ai.GenerateEmbeddingAsync(query);
            var embeddingJson = System.Text.Json.JsonSerializer.Serialize(queryEmbedding);

            var vectorResults = await _context.Database
                .SqlQueryRaw<KnowledgeSearchResult>(
                    @"SELECT kc.""Id"" AS ""ChunkId"", kd.""Id"" AS ""DocumentId"", kd.""Title"" AS ""DocumentTitle"",
                           LEFT(kc.""Content"", 300) AS ""Content"",
                           (1 - ({0}::vector <=> kc.""EmbeddingJson""::vector)) AS ""Relevance""
                    FROM ""KnowledgeChunks"" kc
                    INNER JOIN ""KnowledgeDocuments"" kd ON kc.""DocumentId"" = kd.""Id""
                    WHERE NOT kc.""IsDeleted"" AND NOT kd.""IsDeleted""
                    ORDER BY {0}::vector <=> kc.""EmbeddingJson""::vector
                    LIMIT {1}", embeddingJson, topK)
                .ToListAsync();

            if (vectorResults.Count > 0)
                return vectorResults;
        }
        catch
        {
            // Fall back to text search results if vector search fails
        }

        return results;
    }

    private async Task IndexDocumentInternalAsync(KnowledgeDocument doc)
    {
        doc.Status = KnowledgeDocumentStatus.Indexing;
        doc.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Extract text
        var text = doc.FileType switch
        {
            ".txt" => await File.ReadAllTextAsync(doc.FilePath),
            ".pdf" => await ExtractPdfText(doc.FilePath),
            ".docx" => await ExtractDocxText(doc.FilePath),
            _ => throw new InvalidOperationException($"Unsupported file type: {doc.FileType}")
        };

        // Remove existing chunks
        var oldChunks = await _context.KnowledgeChunks.Where(c => c.DocumentId == doc.Id).ToListAsync();
        _context.KnowledgeChunks.RemoveRange(oldChunks);

        // Chunk text (500 char chunks with 100 char overlap)
        var chunks = ChunkText(text, 500, 100);

        // Generate embeddings and save chunks
        int index = 0;
        foreach (var chunk in chunks)
        {
            var embedding = await _ai.GenerateEmbeddingAsync(chunk);
            _context.KnowledgeChunks.Add(new KnowledgeChunk
            {
                DocumentId = doc.Id,
                Content = chunk,
                ChunkIndex = index++,
                EmbeddingJson = JsonSerializer.Serialize(embedding.ToArray())
            });
        }

        doc.Status = KnowledgeDocumentStatus.Ready;
        doc.ChunkCount = chunks.Count;
        doc.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
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
            start = end - overlap;
            if (start < 0) start = 0;
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
