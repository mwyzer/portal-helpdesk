using AIHelpdesk.Contracts.Recruitment;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class JobVacancyServiceTests
{
    private static (JobVacancyService Service, ApplicationDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);
        return (new JobVacancyService(context), context);
    }

    private static async Task<Guid> SeedUserAsync(ApplicationDbContext context)
    {
        var user = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateDraftVacancy()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);

        var result = await service.CreateAsync(userId, new CreateJobVacancyRequest(
            "Backend Engineer", "We need a backend engineer", "5+ years C#", null, null, 2));

        result.Status.Should().Be("Draft");
        result.Title.Should().Be("Backend Engineer");
        result.OpeningsCount.Should().Be(2);
    }

    [Fact]
    public async Task PublishAsync_ShouldTransitionDraftToPublished()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateJobVacancyRequest("Title", "d", "r", null, null, 1));

        var result = await service.PublishAsync(created.Id);

        result.Status.Should().Be("Published");
        result.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_ShouldThrow_WhenNotDraft()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateJobVacancyRequest("Title", "d", "r", null, null, 1));
        await service.PublishAsync(created.Id);

        var act = () => service.PublishAsync(created.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CloseAsync_ShouldSetClosedStatus_WhenOpeningsNotFilled()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateJobVacancyRequest("Title", "d", "r", null, null, 2));
        await service.PublishAsync(created.Id);

        var result = await service.CloseAsync(created.Id);

        result.Status.Should().Be("Closed");
    }

    [Fact]
    public async Task CloseAsync_ShouldSetFilledStatus_WhenHiredMeetsOpenings()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateJobVacancyRequest("Title", "d", "r", null, null, 1));
        await service.PublishAsync(created.Id);

        context.Candidates.Add(new Candidate
        {
            JobVacancyId = created.Id,
            FullName = "Jane",
            Email = "jane@test.com",
            Stage = CandidateStage.Hired
        });
        await context.SaveChangesAsync();

        var result = await service.CloseAsync(created.Id);

        result.Status.Should().Be("Filled");
    }

    [Fact]
    public async Task CloseAsync_ShouldThrow_WhenNotPublished()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateJobVacancyRequest("Title", "d", "r", null, null, 1));

        var act = () => service.CloseAsync(created.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenClosedOrFilled()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var created = await service.CreateAsync(userId, new CreateJobVacancyRequest("Title", "d", "r", null, null, 1));
        await service.PublishAsync(created.Id);
        await service.CloseAsync(created.Id);

        var act = () => service.UpdateAsync(created.Id, new UpdateJobVacancyRequest("New", "d", "r", null, null, 1));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByStatus()
    {
        var (service, context) = CreateService();
        var userId = await SeedUserAsync(context);
        var draft = await service.CreateAsync(userId, new CreateJobVacancyRequest("Draft One", "d", "r", null, null, 1));
        var toPublish = await service.CreateAsync(userId, new CreateJobVacancyRequest("Published One", "d", "r", null, null, 1));
        await service.PublishAsync(toPublish.Id);

        var result = await service.GetAllAsync(1, 10, "Published", null);

        result.Items.Should().ContainSingle(v => v.Id == toPublish.Id);
        result.Items.Should().NotContain(v => v.Id == draft.Id);
    }
}
