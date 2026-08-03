using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class InterviewServiceTests
{
    private static (InterviewService Service, ApplicationDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);
        return (new InterviewService(context), context);
    }

    private static async Task<(Guid CandidateId, Guid InterviewerId)> SeedCandidateAsync(ApplicationDbContext context)
    {
        var poster = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        var interviewer = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.AddRange(poster, interviewer);

        var vacancy = new JobVacancy { Title = "Engineer", Description = "d", Requirements = "r", PostedById = poster.Id };
        context.JobVacancies.Add(vacancy);

        var candidate = new Candidate { JobVacancyId = vacancy.Id, JobVacancy = vacancy, FullName = "Jane", Email = "j@test.com" };
        context.Candidates.Add(candidate);

        await context.SaveChangesAsync();
        return (candidate.Id, interviewer.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldScheduleInterview()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);

        var result = await service.CreateAsync(new CreateInterviewRequest(
            candidateId, interviewerId, DateTime.UtcNow.AddDays(1), 60, "Video"));

        result.Status.Should().Be("Scheduled");
        result.Type.Should().Be("Video");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenConflictingTimeForSameInterviewer()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        await service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, start, 60, "Video"));

        // Overlaps: existing is 10:00-11:00, new one starts 10:30
        var act = () => service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, start.AddMinutes(30), 60, "Phone"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenNoTimeOverlap()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        await service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, start, 60, "Video"));

        // Existing is 10:00-11:00, new one starts at 11:00 (back-to-back, no overlap)
        var act = () => service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, start.AddHours(1), 60, "Phone"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_ForInvalidType()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);

        var act = () => service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, DateTime.UtcNow.AddDays(1), 60, "NotAType"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CompleteAsync_ShouldSetFeedbackAndRating()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var created = await service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, DateTime.UtcNow.AddDays(1), 60, "Video"));

        var result = await service.CompleteAsync(created.Id, new CompleteInterviewRequest("Great candidate", 4, "Yes"));

        result.Status.Should().Be("Completed");
        result.Rating.Should().Be(4);
        result.Recommendation.Should().Be("Yes");
    }

    [Fact]
    public async Task CancelAsync_ShouldSetCancelledStatus()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var created = await service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, DateTime.UtcNow.AddDays(1), 60, "Video"));

        var result = await service.CancelAsync(created.Id);

        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CompleteAsync_ShouldThrow_WhenAlreadyCancelled()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var created = await service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, DateTime.UtcNow.AddDays(1), 60, "Video"));
        await service.CancelAsync(created.Id);

        var act = () => service.CompleteAsync(created.Id, new CompleteInterviewRequest("f", 3, "Yes"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetUpcomingAsync_ShouldReturnOnlyNext7Days()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var withinWindow = await service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, DateTime.UtcNow.AddDays(2), 60, "Video"));
        await service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, DateTime.UtcNow.AddDays(20), 60, "Phone"));

        var result = await service.GetUpcomingAsync(null);

        result.Should().ContainSingle(i => i.Id == withinWindow.Id);
    }
}
