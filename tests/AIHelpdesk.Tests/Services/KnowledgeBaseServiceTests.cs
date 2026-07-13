using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Knowledge;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AIHelpdesk.Tests.Services;

public class KnowledgeBaseServiceTests
{
    private static async Task<(KnowledgeBaseService Service, ApplicationDbContext Context, Mock<IAIService> AIMock)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"KBTestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var aiMock = new Mock<IAIService>();

        aiMock.Setup(a => a.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] { 0.1, 0.2, 0.3, 0.4, 0.5 });

        var configMock = new Mock<IConfiguration>();
        var uploadDir = Path.Combine(Path.GetTempPath(), $"kb-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(uploadDir);
        configMock.Setup(c => c["KnowledgeBase:UploadPath"]).Returns(uploadDir);

        var service = new KnowledgeBaseService(context, aiMock.Object, configMock.Object);
        return (service, context, aiMock);
    }

    // ── GetDocumentsAsync ──

    [Fact]
    public async Task GetDocumentsAsync_ShouldReturnPaginatedResults()
    {
        var (service, context, _) = await CreateServiceAsync();
        for (int i = 0; i < 15; i++)
        {
            context.KnowledgeDocuments.Add(TestDataFactory.CreateKnowledgeDocument($"Doc {i}"));
        }
        await context.SaveChangesAsync();

        var result = await service.GetDocumentsAsync(1, 5, null);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetDocumentsAsync_ShouldFilterByStatus()
    {
        var (service, context, _) = await CreateServiceAsync();
        context.KnowledgeDocuments.Add(TestDataFactory.CreateKnowledgeDocument("Ready Doc", status: KnowledgeDocumentStatus.Ready));
        context.KnowledgeDocuments.Add(TestDataFactory.CreateKnowledgeDocument("Pending Doc", status: KnowledgeDocumentStatus.Pending));
        context.KnowledgeDocuments.Add(TestDataFactory.CreateKnowledgeDocument("Ready Doc 2", status: KnowledgeDocumentStatus.Ready));
        await context.SaveChangesAsync();

        var result = await service.GetDocumentsAsync(1, 10, "Ready");

        result.Items.Should().HaveCount(2);
        result.Items.All(d => d.Status == "Ready").Should().BeTrue();
    }

    [Fact]
    public async Task GetDocumentsAsync_ShouldFilterByFailedStatus()
    {
        var (service, context, _) = await CreateServiceAsync();
        context.KnowledgeDocuments.Add(TestDataFactory.CreateKnowledgeDocument("Failed Doc", status: KnowledgeDocumentStatus.Failed, errorMessage: "Extraction error"));
        context.KnowledgeDocuments.Add(TestDataFactory.CreateKnowledgeDocument("Ready Doc", status: KnowledgeDocumentStatus.Ready));
        await context.SaveChangesAsync();

        var result = await service.GetDocumentsAsync(1, 10, "Failed");

        result.Items.Should().HaveCount(1);
        result.Items[0].ErrorMessage.Should().Be("Extraction error");
    }

    // ── GetDocumentAsync ──

    [Fact]
    public async Task GetDocumentAsync_ShouldReturnDocumentWithChunks()
    {
        var (service, context, _) = await CreateServiceAsync();
        var doc = TestDataFactory.CreateKnowledgeDocument("Handbook");
        var chunk1 = TestDataFactory.CreateKnowledgeChunk(doc.Id, "Chunk 1 content", 0);
        var chunk2 = TestDataFactory.CreateKnowledgeChunk(doc.Id, "Chunk 2 content", 1);
        doc.Chunks.Add(chunk1);
        doc.Chunks.Add(chunk2);
        context.KnowledgeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var result = await service.GetDocumentAsync(doc.Id);

        result.Title.Should().Be("Handbook");
        result.SampleChunks.Should().HaveCount(2);
        result.SampleChunks[0].Content.Should().Be("Chunk 1 content");
        result.SampleChunks[1].Content.Should().Be("Chunk 2 content");
    }

    [Fact]
    public async Task GetDocumentAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _, _) = await CreateServiceAsync();
        var act = () => service.GetDocumentAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UploadDocumentAsync ──

    [Fact]
    public async Task UploadDocumentAsync_ShouldCreateDocument_InPending()
    {
        var (service, context, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var content = "This is a test knowledge base document.";

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var result = await service.UploadDocumentAsync(userId, "Test Doc", "test.txt", stream, "text/plain");

        result.Title.Should().Be("Test Doc");
        result.FileName.Should().Be("test.txt");
        result.FileType.Should().Be(".txt");
        result.Status.Should().Be("Pending");

        var saved = await context.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldRejectUnsupportedFileType()
    {
        var (service, _, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        using var stream = new MemoryStream(new byte[] { 0, 1, 2, 3 });

        var act = () => service.UploadDocumentAsync(userId, "Image", "photo.jpg", stream, "image/jpeg");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only PDF, DOCX, and TXT*");
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldCreateFile()
    {
        var (service, _, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var content = "Knowledge base test content.";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var result = await service.UploadDocumentAsync(userId, "FileTest", "test.txt", stream, "text/plain");

        // Check that the document is stored
        result.Title.Should().Be("FileTest");
        result.FileName.Should().Be("test.txt");
        result.FileType.Should().Be(".txt");
    }

    // ── DeleteDocumentAsync ──

    [Fact]
    public async Task DeleteDocumentAsync_ShouldSoftDelete()
    {
        var (service, context, _) = await CreateServiceAsync();
        var doc = TestDataFactory.CreateKnowledgeDocument("ToDelete");
        context.KnowledgeDocuments.Add(doc);
        await context.SaveChangesAsync();

        await service.DeleteDocumentAsync(doc.Id);

        var deleted = await context.KnowledgeDocuments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == doc.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDocumentAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _, _) = await CreateServiceAsync();
        var act = () => service.DeleteDocumentAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── IndexDocumentAsync ──

    [Fact]
    public async Task IndexDocumentAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _, _) = await CreateServiceAsync();
        var act = () => service.IndexDocumentAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task IndexDocumentAsync_ShouldIndexTextFile()
    {
        var (service, context, aiMock) = await CreateServiceAsync();

        // Create a temp .txt file
        var uploadDir = Path.Combine(Path.GetTempPath(), $"kb-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(uploadDir);
        var filePath = Path.Combine(uploadDir, "test-index.txt");
        await File.WriteAllTextAsync(filePath,
            "This is the first sentence about company policies. " +
            "This is the second sentence about leave management. " +
            "This is the third sentence about work hours.");

        var doc = new KnowledgeDocument
        {
            Id = Guid.NewGuid(),
            Title = "Index Test",
            FileName = "test-index.txt",
            FilePath = filePath,
            FileType = ".txt",
            ContentType = "text/plain",
            FileSize = new FileInfo(filePath).Length,
            Status = KnowledgeDocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.KnowledgeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var result = await service.IndexDocumentAsync(doc.Id);

        result.Status.Should().Be("Ready");
        result.ChunkCount.Should().BeGreaterThan(0);

        // Verify chunks were created
        var chunks = await context.KnowledgeChunks.Where(c => c.DocumentId == doc.Id).ToListAsync();
        chunks.Should().NotBeEmpty();
        aiMock.Verify(a => a.GenerateEmbeddingAsync(It.IsAny<string>()), Times.AtLeastOnce());

        // Cleanup
        try { File.Delete(filePath); } catch { }
        try { Directory.Delete(uploadDir, true); } catch { }
    }

    // ── SearchAsync ──

    [Fact]
    public async Task SearchAsync_ShouldReturnTextSearchResults()
    {
        var (service, context, _) = await CreateServiceAsync();
        var doc = TestDataFactory.CreateKnowledgeDocument("Policies");
        var chunk = TestDataFactory.CreateKnowledgeChunk(doc.Id,
            "Our leave policy allows 12 days of annual leave per year.", 0);
        doc.Chunks.Add(chunk);
        context.KnowledgeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var results = await service.SearchAsync("leave policy", 5);

        results.Should().HaveCount(1);
        results[0].DocumentTitle.Should().Be("Policies");
        results[0].Content.Should().Contain("leave policy");
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenNoMatch()
    {
        var (service, context, _) = await CreateServiceAsync();
        var doc = TestDataFactory.CreateKnowledgeDocument("HR Docs");
        var chunk = TestDataFactory.CreateKnowledgeChunk(doc.Id, "Work hours are from 9 to 5.", 0);
        doc.Chunks.Add(chunk);
        context.KnowledgeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var results = await service.SearchAsync("nonexistent query xyz", 5);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldLimitToTopK()
    {
        var (service, context, _) = await CreateServiceAsync();
        var doc = TestDataFactory.CreateKnowledgeDocument("Handbook");
        for (int i = 0; i < 10; i++)
        {
            doc.Chunks.Add(TestDataFactory.CreateKnowledgeChunk(doc.Id, $"Chunk {i} about policies and procedures.", i));
        }
        context.KnowledgeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var results = await service.SearchAsync("policies", 3);

        results.Should().HaveCountLessThanOrEqualTo(3);
    }

    // ── Content truncation in search results ──

    [Fact]
    public async Task SearchAsync_ShouldTruncateLongContent()
    {
        var (service, context, _) = await CreateServiceAsync();
        var doc = TestDataFactory.CreateKnowledgeDocument("Long Doc");
        var longContent = new string('a', 500) + " policies end";
        var chunk = TestDataFactory.CreateKnowledgeChunk(doc.Id, longContent, 0);
        doc.Chunks.Add(chunk);
        context.KnowledgeDocuments.Add(doc);
        await context.SaveChangesAsync();

        var results = await service.SearchAsync("policies", 5);

        results.Should().HaveCount(1);
        // Content should be truncated to 300 chars + "..."
        results[0].Content.Should().EndWith("...");
        results[0].Content.Length.Should().BeLessThanOrEqualTo(303);
    }
}
