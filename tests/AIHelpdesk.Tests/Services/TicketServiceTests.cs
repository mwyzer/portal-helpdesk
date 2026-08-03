using System.Text;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Tickets;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AIHelpdesk.Tests.Services;

public class TicketServiceTests
{
    private static (TicketService Service, ApplicationDbContext Context, Mock<IAIService> AiMock) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);

        var uploadsPath = Path.Combine(Path.GetTempPath(), "AIHelpdeskTests", Guid.NewGuid().ToString());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Uploads:Path"] = uploadsPath })
            .Build();

        var aiMock = new Mock<IAIService>();
        var service = new TicketService(context, configuration, aiMock.Object, new ExcelService(), NullLogger<TicketService>.Instance);
        return (service, context, aiMock);
    }

    private static async Task<TicketCategory> SeedCategoryAsync(ApplicationDbContext context, int slaHours = 24)
    {
        var category = new TicketCategory { Name = "IT Support", Description = "IT", DefaultPriority = TicketPriority.Normal, SLAHours = slaHours };
        context.TicketCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    // GetByIdAsync .Include()s several required (non-nullable) navigations — e.g. AssignedTo,
    // SubmittedBy — which EF translates as inner joins. A ticket referencing a userId with no
    // matching User row silently disappears from query results, so tests must seed a real user.
    private static async Task<Guid> SeedUserAsync(ApplicationDbContext context)
    {
        var user = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    // ─────────── CreateAsync ───────────

    [Fact]
    public async Task CreateAsync_ShouldSetSLADeadline_FromCategorySLAHours()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context, slaHours: 8);
        var userId = await SeedUserAsync(context);

        var result = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "Printer broken", "Won't print", null, null));

        result.SLADeadline.Should().NotBeNull();
        result.SLADeadline!.Value.Should().BeCloseTo(DateTime.UtcNow.AddHours(8), TimeSpan.FromMinutes(1));
        result.Priority.Should().Be("Normal"); // falls back to category default
        result.Status.Should().Be("Open");
    }

    [Fact]
    public async Task CreateAsync_ShouldUseRequestedPriority_WhenProvided()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);

        var result = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "Urgent issue", "desc", null, "Urgent"));

        result.Priority.Should().Be("Urgent");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCategoryNotFound()
    {
        var (service, _, _) = CreateService();

        var act = () => service.CreateAsync(Guid.NewGuid(), new CreateTicketRequest(Guid.NewGuid(), "t", "d", null, null));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─────────── Status transitions ───────────

    [Fact]
    public async Task ResolveAsync_ShouldSetStatusAndResolvedAt()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));

        var resolved = await service.ResolveAsync(created.Id, userId);

        resolved.Status.Should().Be("Resolved");
        resolved.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CloseAsync_ShouldSetStatusAndClosedAt()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));
        await service.ResolveAsync(created.Id, userId);

        var closed = await service.CloseAsync(created.Id, userId);

        closed.Status.Should().Be("Closed");
        closed.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReopenAsync_ShouldRevertToReopenedStatus()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));
        await service.ResolveAsync(created.Id, userId);

        var reopened = await service.ReopenAsync(created.Id, userId);

        reopened.Status.Should().Be("Reopened");
    }

    // ─────────── Comments ───────────

    [Fact]
    public async Task AddCommentAsync_ShouldAppendComment()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));

        var result = await service.AddCommentAsync(created.Id, userId, new CreateTicketCommentRequest("Looking into it", false));

        result.Comments.Should().ContainSingle(c => c.Content == "Looking into it" && !c.IsInternal);
    }

    // ─────────── Attachments ───────────

    [Fact]
    public async Task UploadAttachmentAsync_ShouldReject_DisallowedExtension()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("malicious"));
        var act = () => service.UploadAttachmentAsync(created.Id, userId, "virus.exe", "application/octet-stream", stream);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UploadAttachmentAsync_ShouldReject_OversizedFile()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));

        using var stream = new MemoryStream(new byte[11 * 1024 * 1024]); // 11 MB > 10 MB limit
        var act = () => service.UploadAttachmentAsync(created.Id, userId, "big.pdf", "application/pdf", stream);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UploadAttachmentAsync_ThenDownloadAttachmentAsync_ShouldRoundTrip()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));

        var content = Encoding.UTF8.GetBytes("hello world");
        using (var uploadStream = new MemoryStream(content))
        {
            await service.UploadAttachmentAsync(created.Id, userId, "notes.txt", "text/plain", uploadStream);
        }

        var detail = await service.GetByIdAsync(created.Id);
        var attachmentId = detail.Attachments.Single().Id;

        var (downloadStream, contentType, fileName) = await service.DownloadAttachmentAsync(created.Id, attachmentId);
        using var reader = new StreamReader(downloadStream);
        var downloaded = await reader.ReadToEndAsync();

        downloaded.Should().Be("hello world");
        contentType.Should().Be("text/plain");
        fileName.Should().Be("notes.txt");
    }

    [Fact]
    public async Task DeleteAttachmentAsync_ShouldRemoveAttachment()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));

        using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("data")))
        {
            await service.UploadAttachmentAsync(created.Id, userId, "notes.txt", "text/plain", uploadStream);
        }

        var detail = await service.GetByIdAsync(created.Id);
        var attachmentId = detail.Attachments.Single().Id;

        await service.DeleteAttachmentAsync(created.Id, attachmentId, userId);

        var afterDelete = await service.GetByIdAsync(created.Id);
        afterDelete.Attachments.Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadAttachmentAsync_ShouldThrow_WhenAttachmentNotFound()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));

        var act = () => service.DownloadAttachmentAsync(created.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─────────── Excel Export ───────────

    [Fact]
    public async Task ExportToExcelAsync_ShouldReturnByteArray()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));

        var result = await service.ExportToExcelAsync(null, null, null);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExportToExcelAsync_ShouldFilterByStatus()
    {
        var (service, context, _) = CreateService();
        var category = await SeedCategoryAsync(context);
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateTicketRequest(category.Id, "t", "d", null, null));
        await service.ResolveAsync(created.Id, userId);

        var resolvedOnly = await service.ExportToExcelAsync(null, "Resolved", null);
        var openOnly = await service.ExportToExcelAsync(null, "Open", null);

        using var resolvedWorkbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(resolvedOnly));
        using var openWorkbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(openOnly));

        resolvedWorkbook.Worksheets.First().RangeUsed()!.RowCount().Should().Be(2); // header + 1 resolved ticket
        openWorkbook.Worksheets.First().RangeUsed()!.RowCount().Should().Be(1); // header only, no open tickets
    }

    // ─────────── AI Suggestion ───────────

    [Fact]
    public async Task GetAISuggestionAsync_ShouldReturnDefault_WhenNoCategoriesExist()
    {
        var (service, _, _) = CreateService();

        var result = await service.GetAISuggestionAsync(new CreateTicketRequest(Guid.NewGuid(), "t", "d", null, null));

        result.SuggestedCategory.Should().Be("General Support");
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public async Task GetAISuggestionAsync_ShouldParseAIResponse_AndMatchCategory()
    {
        var (service, context, aiMock) = CreateService();
        await SeedCategoryAsync(context);
        aiMock.Setup(a => a.GenerateChatResponseAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<(string Role, string Content)>>(), null))
            .ReturnsAsync("""{"category":"IT Support","priority":"High","reason":"Sounds technical","confidence":0.9}""");

        var result = await service.GetAISuggestionAsync(new CreateTicketRequest(Guid.NewGuid(), "Server down", "Cannot connect", null, null));

        result.SuggestedCategory.Should().Be("IT Support");
        result.SuggestedPriority.Should().Be("High");
        result.Confidence.Should().Be(0.9);
    }

    [Fact]
    public async Task GetAISuggestionAsync_ShouldFallBackToFirstCategory_WhenAIThrows()
    {
        var (service, context, aiMock) = CreateService();
        await SeedCategoryAsync(context);
        aiMock.Setup(a => a.GenerateChatResponseAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<(string Role, string Content)>>(), null))
            .ThrowsAsync(new HttpRequestException("network down"));

        var result = await service.GetAISuggestionAsync(new CreateTicketRequest(Guid.NewGuid(), "t", "d", null, null));

        result.SuggestedCategory.Should().Be("IT Support");
        result.Confidence.Should().Be(0.0);
    }
}
