using AIHelpdesk.Contracts.Tickets;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class TicketCategoryServiceTests
{
    private static (TicketCategoryService Service, ApplicationDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);
        return (new TicketCategoryService(context), context);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCategory()
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateTicketCategoryRequest("IT Support", "Tech issues", "High", 8, null));

        result.Name.Should().Be("IT Support");
        result.DefaultPriority.Should().Be("High");
        result.SLAHours.Should().Be(8);
    }

    [Fact]
    public async Task CreateAsync_ShouldDefaultToNormalPriority_WhenPriorityInvalid()
    {
        var (service, _) = CreateService();

        var result = await service.CreateAsync(new CreateTicketCategoryRequest("General", "desc", "NotAPriority", 24, null));

        result.DefaultPriority.Should().Be("Normal");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories_OrderedByName()
    {
        var (service, _) = CreateService();
        await service.CreateAsync(new CreateTicketCategoryRequest("Zeta", "d", "Normal", 24, null));
        await service.CreateAsync(new CreateTicketCategoryRequest("Alpha", "d", "Normal", 24, null));

        var result = await service.GetAllAsync(null);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = CreateService();

        var act = () => service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFields()
    {
        var (service, _) = CreateService();
        var created = await service.CreateAsync(new CreateTicketCategoryRequest("Old Name", "d", "Normal", 24, null));

        var updated = await service.UpdateAsync(created.Id, new UpdateTicketCategoryRequest("New Name", "new desc", "Urgent", 4, null));

        updated.Name.Should().Be("New Name");
        updated.SLAHours.Should().Be(4);
        updated.DefaultPriority.Should().Be("Urgent");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = CreateService();

        var act = () => service.UpdateAsync(Guid.NewGuid(), new UpdateTicketCategoryRequest("n", "d", "Normal", 24, null));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete_AndExcludeFromGetAll()
    {
        var (service, _) = CreateService();
        var created = await service.CreateAsync(new CreateTicketCategoryRequest("Temp", "d", "Normal", 24, null));

        await service.DeleteAsync(created.Id);

        var all = await service.GetAllAsync(null);
        all.Should().NotContain(c => c.Id == created.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = CreateService();

        var act = () => service.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
