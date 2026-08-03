using System.Text;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AIHelpdesk.Tests.Services;

public class CandidateServiceTests
{
    private static (CandidateService Service, ApplicationDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);

        var uploadsPath = Path.Combine(Path.GetTempPath(), "AIHelpdeskTests", Guid.NewGuid().ToString());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Recruitment:UploadPath"] = uploadsPath })
            .Build();

        return (new CandidateService(context, configuration, new ExcelService()), context);
    }

    private static async Task<Guid> SeedUserAsync(ApplicationDbContext context)
    {
        var user = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<Guid> SeedVacancyAsync(ApplicationDbContext context, Guid postedById)
    {
        var vacancy = new JobVacancy { Title = "Backend Engineer", Description = "d", Requirements = "r", PostedById = postedById };
        context.JobVacancies.Add(vacancy);
        await context.SaveChangesAsync();
        return vacancy.Id;
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCandidateInAppliedStage()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);

        var result = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane Doe", "jane@test.com", "0812345678", "LinkedIn"));

        result.Stage.Should().Be("Applied");
        result.FullName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenVacancyNotFound()
    {
        var (service, _) = CreateService();

        var act = () => service.CreateAsync(new CreateCandidateRequest(Guid.NewGuid(), "Jane", "j@test.com", null, null));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task AdvanceStageAsync_ShouldMoveToNextStage()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));

        var result = await service.AdvanceStageAsync(candidate.Id, userId, new AdvanceCandidateStageRequest("Good fit"));

        result.Stage.Should().Be("Screening");
    }

    [Fact]
    public async Task AdvanceStageAsync_ShouldRecordStageHistory()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));

        await service.AdvanceStageAsync(candidate.Id, userId, new AdvanceCandidateStageRequest("notes"));

        var detail = await service.GetByIdAsync(candidate.Id);
        detail.StageHistory.Should().ContainSingle(h => h.FromStage == "Applied" && h.ToStage == "Screening");
    }

    [Fact]
    public async Task AdvanceStageAsync_ShouldThrow_WhenAtHired()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));
        for (int i = 0; i < 5; i++)
            await service.AdvanceStageAsync(candidate.Id, userId, new AdvanceCandidateStageRequest(null));

        var act = () => service.AdvanceStageAsync(candidate.Id, userId, new AdvanceCandidateStageRequest(null));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AdvanceStageAsync_ShouldThrow_WhenRejected()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));
        await service.RejectAsync(candidate.Id, userId, new RejectCandidateRequest("Not a fit"));

        var act = () => service.AdvanceStageAsync(candidate.Id, userId, new AdvanceCandidateStageRequest(null));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RejectAsync_ShouldSetRejectedStage_FromAnyActiveStage()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));
        await service.AdvanceStageAsync(candidate.Id, userId, new AdvanceCandidateStageRequest(null)); // -> Screening

        var result = await service.RejectAsync(candidate.Id, userId, new RejectCandidateRequest("Failed screening"));

        result.Stage.Should().Be("Rejected");
    }

    [Fact]
    public async Task RejectAsync_ShouldThrow_WhenAlreadyHiredOrRejected()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));
        await service.RejectAsync(candidate.Id, userId, new RejectCandidateRequest("reason"));

        var act = () => service.RejectAsync(candidate.Id, userId, new RejectCandidateRequest("reason again"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UploadCvAsync_ShouldReject_DisallowedExtension()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));
        var act = () => service.UploadCvAsync(candidate.Id, userId, "resume.exe", "application/octet-stream", stream);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UploadCvAsync_ShouldReject_OversizedFile()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));

        using var stream = new MemoryStream(new byte[6 * 1024 * 1024]); // 6 MB > 5 MB limit
        var act = () => service.UploadCvAsync(candidate.Id, userId, "resume.pdf", "application/pdf", stream);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UploadCvAsync_ThenDownloadCvAsync_ShouldRoundTrip()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        var candidate = await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));

        using (var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes("cv content")))
        {
            await service.UploadCvAsync(candidate.Id, userId, "resume.pdf", "application/pdf", uploadStream);
        }

        var detail = await service.GetByIdAsync(candidate.Id);
        var documentId = detail.Documents.Single().Id;

        var (downloadStream, contentType, fileName) = await service.DownloadCvAsync(candidate.Id, documentId);
        using var reader = new StreamReader(downloadStream);
        var content = await reader.ReadToEndAsync();

        content.Should().Be("cv content");
        contentType.Should().Be("application/pdf");
        fileName.Should().Be("resume.pdf");
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnCandidatesPerStage()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        await service.CreateAsync(new CreateCandidateRequest(vacancyId, "A", "a@test.com", null, null));
        await service.CreateAsync(new CreateCandidateRequest(vacancyId, "B", "b@test.com", null, null));

        var stats = await service.GetStatsAsync();

        stats.TotalCandidates.Should().Be(2);
        stats.CandidatesPerStage["Applied"].Should().Be(2);
    }

    [Fact]
    public async Task ExportToExcelAsync_ShouldReturnByteArray()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var vacancyId = await SeedVacancyAsync(context, userId);
        await service.CreateAsync(new CreateCandidateRequest(vacancyId, "Jane", "j@test.com", null, null));

        var result = await service.ExportToExcelAsync(null, null);

        result.Should().NotBeEmpty();
    }
}
