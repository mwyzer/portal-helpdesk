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

    [Fact]
    public async Task CreateSlotAsync_ShouldCreateOpenSlot()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var vacancyId = (await context.Candidates.FindAsync(candidateId))!.JobVacancyId;

        var result = await service.CreateSlotAsync(new CreateInterviewSlotRequest(
            interviewerId, vacancyId, DateTime.UtcNow.AddDays(1), 60, "Video"));

        result.Status.Should().Be("Open");
        result.Type.Should().Be("Video");
    }

    [Fact]
    public async Task CreateSlotAsync_ShouldThrow_WhenConflictsWithExistingInterview()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var vacancyId = (await context.Candidates.FindAsync(candidateId))!.JobVacancyId;
        var start = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        await service.CreateAsync(new CreateInterviewRequest(candidateId, interviewerId, start, 60, "Video"));

        var act = () => service.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, start.AddMinutes(30), 60, "Phone"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetSlotsAsync_ShouldFilterByStatus()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var vacancyId = (await context.Candidates.FindAsync(candidateId))!.JobVacancyId;
        var slot = await service.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(1), 60, "Video"));
        await service.CancelSlotAsync(slot.Id);
        await service.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(2), 60, "Phone"));

        var openSlots = await service.GetSlotsAsync(vacancyId, null, "Open");

        openSlots.Should().ContainSingle().Which.Type.Should().Be("Phone");
    }

    [Fact]
    public async Task CancelSlotAsync_ShouldThrow_WhenNotOpen()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var vacancyId = (await context.Candidates.FindAsync(candidateId))!.JobVacancyId;
        var slot = await service.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(1), 60, "Video"));
        await service.CancelSlotAsync(slot.Id);

        var act = () => service.CancelSlotAsync(slot.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BookSlotAsync_ShouldCreateInterviewAndMarkSlotBooked()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var vacancyId = (await context.Candidates.FindAsync(candidateId))!.JobVacancyId;
        var slot = await service.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(1), 60, "Video"));

        var interview = await service.BookSlotAsync(slot.Id, candidateId);

        interview.Status.Should().Be("Scheduled");
        interview.CandidateId.Should().Be(candidateId);
        var slots = await service.GetSlotsAsync(vacancyId, null, "Booked");
        slots.Should().ContainSingle(s => s.Id == slot.Id);
    }

    [Fact]
    public async Task BookSlotAsync_ShouldThrow_WhenSlotNotOpen()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var vacancyId = (await context.Candidates.FindAsync(candidateId))!.JobVacancyId;
        var slot = await service.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(1), 60, "Video"));
        await service.BookSlotAsync(slot.Id, candidateId);

        var act = () => service.BookSlotAsync(slot.Id, candidateId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BookSlotAsync_ShouldThrow_WhenCandidateFromDifferentVacancy()
    {
        var (service, context) = CreateService();
        var (candidateId, interviewerId) = await SeedCandidateAsync(context);
        var vacancyId = (await context.Candidates.FindAsync(candidateId))!.JobVacancyId;
        var slot = await service.CreateSlotAsync(new CreateInterviewSlotRequest(interviewerId, vacancyId, DateTime.UtcNow.AddDays(1), 60, "Video"));

        var otherPoster = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.Add(otherPoster);
        var otherVacancy = new JobVacancy { Title = "Other Role", Description = "d", Requirements = "r", PostedById = otherPoster.Id };
        context.JobVacancies.Add(otherVacancy);
        var otherCandidate = new Candidate { JobVacancyId = otherVacancy.Id, JobVacancy = otherVacancy, FullName = "Other", Email = "o@test.com" };
        context.Candidates.Add(otherCandidate);
        await context.SaveChangesAsync();

        var act = () => service.BookSlotAsync(slot.Id, otherCandidate.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
