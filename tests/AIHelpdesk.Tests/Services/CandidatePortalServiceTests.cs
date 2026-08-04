using System.Text;
using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AIHelpdesk.Tests.Services;

public class CandidatePortalServiceTests
{
    private static (CandidatePortalService Service, ApplicationDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);

        var uploadsPath = Path.Combine(Path.GetTempPath(), "AIHelpdeskTests", Guid.NewGuid().ToString());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestOnlySigningKeyThatIsLongEnough1234567890!",
                ["Jwt:Issuer"] = "AIHelpdeskTests",
                ["Jwt:Audience"] = "AIHelpdeskTests",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Recruitment:UploadPath"] = uploadsPath,
            })
            .Build();
        var tokenService = new TokenService(configuration);
        var interviewService = new InterviewService(context);

        return (new CandidatePortalService(context, tokenService, interviewService, configuration), context);
    }

    private static async Task<(Guid CandidateId, Guid VacancyId, Guid InterviewerId)> SeedCandidateAsync(
        ApplicationDbContext context, string email = "jane@test.com")
    {
        var poster = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        var interviewer = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.AddRange(poster, interviewer);

        var vacancy = new JobVacancy { Title = "Engineer", Description = "d", Requirements = "r", PostedById = poster.Id };
        context.JobVacancies.Add(vacancy);

        var candidate = new Candidate { JobVacancyId = vacancy.Id, JobVacancy = vacancy, FullName = "Jane Doe", Email = email };
        context.Candidates.Add(candidate);

        await context.SaveChangesAsync();
        return (candidate.Id, vacancy.Id, interviewer.Id);
    }

    /// <summary>Activates a candidate account via the real ActivateAccountAsync flow (not a shortcut) using the SAME service/context the test is already using.</summary>
    private static async Task<CandidateAccount> SeedActivatedAccountAsync(
        CandidatePortalService service, ApplicationDbContext context, Guid candidateId, string password = "P@ssw0rd123")
    {
        context.CandidateAccounts.Add(new CandidateAccount
        {
            CandidateId = candidateId,
            IsActive = false,
            SetupToken = "test-setup-token",
            SetupTokenExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        await service.ActivateAccountAsync(new CandidatePortalActivateRequest("test-setup-token", password));
        return await context.CandidateAccounts.FirstAsync(a => a.CandidateId == candidateId);
    }

    [Fact]
    public async Task ActivateAccountAsync_ShouldSetPasswordAndReturnTokens()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);
        context.CandidateAccounts.Add(new CandidateAccount
        {
            CandidateId = candidateId, IsActive = false, SetupToken = "tok", SetupTokenExpiresAt = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        var result = await service.ActivateAccountAsync(new CandidatePortalActivateRequest("tok", "P@ssw0rd123"));

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.Profile.CandidateId.Should().Be(candidateId);
        var account = await context.CandidateAccounts.FirstAsync(a => a.CandidateId == candidateId);
        account.IsActive.Should().BeTrue();
        account.SetupToken.Should().BeNull();
    }

    [Fact]
    public async Task ActivateAccountAsync_ShouldThrow_WhenTokenExpired()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);
        context.CandidateAccounts.Add(new CandidateAccount
        {
            CandidateId = candidateId, IsActive = false, SetupToken = "tok", SetupTokenExpiresAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var act = () => service.ActivateAccountAsync(new CandidatePortalActivateRequest("tok", "P@ssw0rd123"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ActivateAccountAsync_ShouldThrow_WhenTokenUnknown()
    {
        var (service, _) = CreateService();

        var act = () => service.ActivateAccountAsync(new CandidatePortalActivateRequest("nope", "P@ssw0rd123"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_ForValidCredentials()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);
        await SeedActivatedAccountAsync(service, context, candidateId);

        var result = await service.LoginAsync(new CandidatePortalLoginRequest("jane@test.com", "P@ssw0rd123"), "127.0.0.1");

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.Profile.Email.Should().Be("jane@test.com");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_ForWrongPassword()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);
        await SeedActivatedAccountAsync(service, context, candidateId);

        var act = () => service.LoginAsync(new CandidatePortalLoginRequest("jane@test.com", "WrongPassword"), null);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenAccountNotActivated()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);
        context.CandidateAccounts.Add(new CandidateAccount { CandidateId = candidateId, IsActive = false });
        await context.SaveChangesAsync();

        var act = () => service.LoginAsync(new CandidatePortalLoginRequest("jane@test.com", "whatever"), null);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRotateToken()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);
        await SeedActivatedAccountAsync(service, context, candidateId);
        var login = await service.LoginAsync(new CandidatePortalLoginRequest("jane@test.com", "P@ssw0rd123"), null);

        var result = await service.RefreshTokenAsync(new CandidatePortalRefreshRequest(login.AccessToken, login.RefreshToken), null);

        result.RefreshToken.Should().NotBe(login.RefreshToken);
        var oldToken = await context.CandidatePortalRefreshTokens.FirstAsync(t => t.Token == login.RefreshToken);
        oldToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyStatusAsync_ShouldReturnStageAndVacancyTitle()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);

        var result = await service.GetMyStatusAsync(candidateId);

        result.JobVacancyTitle.Should().Be("Engineer");
        result.Stage.Should().Be("Applied");
    }

    [Fact]
    public async Task UploadMyDocumentAsync_ShouldStoreWithNullUploadedBy()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));

        var result = await service.UploadMyDocumentAsync(candidateId, "resume.pdf", "application/pdf", stream);

        result.FileName.Should().Be("resume.pdf");
        var doc = await context.CandidateDocuments.FirstAsync(d => d.Id == result.Id);
        doc.UploadedById.Should().BeNull();
    }

    [Fact]
    public async Task UploadMyDocumentAsync_ShouldReject_DisallowedExtension()
    {
        var (service, context) = CreateService();
        var (candidateId, _, _) = await SeedCandidateAsync(context);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("data"));

        var act = () => service.UploadMyDocumentAsync(candidateId, "resume.exe", "application/octet-stream", stream);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldOnlyReturnOpenFutureSlotsForOwnVacancy()
    {
        var (service, context) = CreateService();
        var (candidateId, vacancyId, interviewerId) = await SeedCandidateAsync(context);
        var interviewService = new InterviewService(context);
        var openSlot = await interviewService.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(1), 60, "Video"));
        var pastSlot = await interviewService.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(3), 60, "Phone"));
        await interviewService.CancelSlotAsync(pastSlot.Id);

        var result = await service.GetAvailableSlotsAsync(candidateId);

        result.Should().ContainSingle(s => s.SlotId == openSlot.Id);
    }

    [Fact]
    public async Task BookSlotAsync_ShouldCreateInterview()
    {
        var (service, context) = CreateService();
        var (candidateId, vacancyId, interviewerId) = await SeedCandidateAsync(context);
        var interviewService = new InterviewService(context);
        var slot = await interviewService.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(1), 60, "Video"));

        var result = await service.BookSlotAsync(candidateId, slot.Id);

        result.Status.Should().Be("Scheduled");
        var interviews = await service.GetMyInterviewsAsync(candidateId);
        interviews.Should().ContainSingle(i => i.Id == result.Id);
    }
}
