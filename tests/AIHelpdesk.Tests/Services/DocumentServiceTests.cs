using AIHelpdesk.Contracts.Documents;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class DocumentServiceTests
{
    private static async Task<(DocumentService Service, ApplicationDbContext Context)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new DocumentService(context);
        return (service, context);
    }

    // ─────────── Templates ───────────

    [Fact]
    public async Task CreateTemplateAsync_ShouldCreateTemplate()
    {
        var (service, context) = await CreateServiceAsync();

        var request = new CreateDocumentTemplateRequest(
            "Leave Letter", "LL", "Leave",
            "Dear {manager},\nI, {employee_name}, request leave on {date}.",
            "employee_name,date,manager");

        var result = await service.CreateTemplateAsync(request);

        result.Name.Should().Be("Leave Letter");
        result.Code.Should().Be("LL");
        result.Category.Should().Be("Leave");
        result.IsActive.Should().BeTrue();
        result.ContentTemplate.Should().Contain("{employee_name}");
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnAll()
    {
        var (service, context) = await CreateServiceAsync();
        context.DocumentTemplates.Add(TestDataFactory.CreateDocumentTemplate(name: "Template A", category: "HR"));
        context.DocumentTemplates.Add(TestDataFactory.CreateDocumentTemplate(name: "Template B", category: "Finance"));
        await context.SaveChangesAsync();

        var result = await service.GetTemplatesAsync(null);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldFilterByCategory()
    {
        var (service, context) = await CreateServiceAsync();
        context.DocumentTemplates.Add(TestDataFactory.CreateDocumentTemplate(name: "A", category: "HR"));
        context.DocumentTemplates.Add(TestDataFactory.CreateDocumentTemplate(name: "B", category: "Finance"));
        await context.SaveChangesAsync();

        var result = await service.GetTemplatesAsync("HR");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("A");
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldUpdate()
    {
        var (service, context) = await CreateServiceAsync();
        var template = TestDataFactory.CreateDocumentTemplate(name: "Old Name");
        context.DocumentTemplates.Add(template);
        await context.SaveChangesAsync();

        var request = new UpdateDocumentTemplateRequest(
            "New Name", "NN", "General", "New content", "x,y", false);

        var result = await service.UpdateTemplateAsync(template.Id, request);

        result.Name.Should().Be("New Name");
        result.Code.Should().Be("NN");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTemplateAsync_ShouldSoftDelete()
    {
        var (service, context) = await CreateServiceAsync();
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        await context.SaveChangesAsync();

        await service.DeleteTemplateAsync(template.Id);

        var deleted = await context.DocumentTemplates.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == template.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetTemplateByIdAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = await CreateServiceAsync();

        await service.Invoking(s => s.GetTemplateByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    // ─────────── Document Requests ───────────

    [Fact]
    public async Task CreateDocumentRequestAsync_ShouldCreateDraft()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser(fullName: "Alice");
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate(name: "Leave Letter", code: "LL");
        context.DocumentTemplates.Add(template);
        await context.SaveChangesAsync();

        var request = new CreateDocumentRequestRequest(template.Id, "My Leave", "Need a day off");
        var result = await service.CreateDocumentRequestAsync(employee.Id, request);

        result.Title.Should().Be("My Leave");
        result.Status.Should().Be("Draft");
        result.TemplateName.Should().Be("Leave Letter");
    }

    [Fact]
    public async Task GetDocumentRequestsAsync_ShouldReturnPaginated()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        await context.SaveChangesAsync();

        for (int i = 0; i < 12; i++)
        {
            context.DocumentRequests.Add(TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, $"Request {i}"));
        }
        await context.SaveChangesAsync();

        var result = await service.GetDocumentRequestsAsync(employee.Id, 1, 5, null);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(12);
    }

    [Fact]
    public async Task GetDocumentRequestsAsync_ShouldFilterByStatus()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        await context.SaveChangesAsync();

        context.DocumentRequests.Add(TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, "Draft", DocumentRequestStatus.Draft));
        context.DocumentRequests.Add(TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, "Review", DocumentRequestStatus.Review));
        context.DocumentRequests.Add(TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, "Review 2", DocumentRequestStatus.Review));
        await context.SaveChangesAsync();

        var result = await service.GetDocumentRequestsAsync(employee.Id, 1, 10, "Review");

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateDraftAsync_ShouldGenerateDraftContent()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser(fullName: "Alice");
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate(
            contentTemplate: "Dear Manager, I {employee_name} request leave on {date}.");
        context.DocumentTemplates.Add(template);
        await context.SaveChangesAsync();

        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, status: DocumentRequestStatus.Draft);
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        var result = await service.GenerateDraftAsync(docRequest.Id);

        result.ContentDraft.Should().Contain("Alice");
        result.Status.Should().Be("AiDraftReady");
    }

    [Fact]
    public async Task SubmitForReviewAsync_ShouldChangeStatus()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        await context.SaveChangesAsync();

        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, status: DocumentRequestStatus.AiDraftReady);
        docRequest.ContentDraft = "draft content";
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        var result = await service.SubmitForReviewAsync(docRequest.Id);

        result.Status.Should().Be("Review");
    }

    [Fact]
    public async Task SubmitForReviewAsync_ShouldThrow_WhenNotDraftReady()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, status: DocumentRequestStatus.Draft);
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        await service.Invoking(s => s.SubmitForReviewAsync(docRequest.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Document must have a draft before submitting for review");
    }

    [Fact]
    public async Task ApproveDocumentAsync_ShouldApprove()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        var reviewer = TestDataFactory.CreateUser();
        context.Users.AddRange(employee, reviewer);
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, status: DocumentRequestStatus.Review);
        docRequest.ContentDraft = "draft";
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        var result = await service.ApproveDocumentAsync(docRequest.Id, reviewer.Id);

        result.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task ApproveDocumentAsync_ShouldThrow_WhenNotInReview()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, status: DocumentRequestStatus.Draft);
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        await service.Invoking(s => s.ApproveDocumentAsync(docRequest.Id, Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Document is not in review status");
    }

    [Fact]
    public async Task RejectDocumentAsync_ShouldRejectWithReason()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, status: DocumentRequestStatus.Review);
        docRequest.ContentDraft = "draft";
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        var result = await service.RejectDocumentAsync(docRequest.Id, "Needs revision");

        result.Status.Should().Be("Rejected");
        result.RejectionReason.Should().Be("Needs revision");
    }

    [Fact]
    public async Task GenerateFinalAsync_ShouldGenerateLetterNumber()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate(name: "Leave Letter", code: "LL");
        context.DocumentTemplates.Add(template);
        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, status: DocumentRequestStatus.Approved);
        docRequest.ContentDraft = "Final content";
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        var result = await service.GenerateFinalAsync(docRequest.Id);

        result.Status.Should().Be("Generated");
        result.LetterNumber.Should().NotBeNull();
        result.LetterNumber.Should().MatchRegex(@"^\d{3}/LL/MGR/\d{4}$");
        result.ContentFinal.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateFinalAsync_ShouldThrow_WhenNotApproved()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser();
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate();
        context.DocumentTemplates.Add(template);
        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, status: DocumentRequestStatus.Draft);
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        await service.Invoking(s => s.GenerateFinalAsync(docRequest.Id))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Document must be approved before final generation");
    }

    [Fact]
    public async Task GetDocumentRequestByIdAsync_ShouldReturnDetail()
    {
        var (service, context) = await CreateServiceAsync();
        var employee = TestDataFactory.CreateUser(fullName: "Alice");
        context.Users.Add(employee);
        var template = TestDataFactory.CreateDocumentTemplate(name: "Leave Letter");
        context.DocumentTemplates.Add(template);
        var docRequest = TestDataFactory.CreateDocumentRequest(employee.Id, template.Id, "My Request");
        context.DocumentRequests.Add(docRequest);
        await context.SaveChangesAsync();

        var result = await service.GetDocumentRequestByIdAsync(docRequest.Id);

        result.Title.Should().Be("My Request");
        result.EmployeeName.Should().Be("Alice");
        result.TemplateName.Should().Be("Leave Letter");
    }
}
