using AIHelpdesk.Contracts.Tickets;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class EscalationServiceTests
{
    private static (EscalationService Service, ApplicationDbContext Context) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);
        return (new EscalationService(context), context);
    }

    // GetEscalationsAsync/GetPendingAsync join through Escalation.Ticket (required nav) and
    // Escalation.EscalatedBy (required nav), so both need real rows to appear in results.
    private static async Task<(Ticket Ticket, Guid UserId)> SeedTicketAsync(ApplicationDbContext context, Guid? departmentId = null)
    {
        var user = TestDataFactory.CreateUser($"{Guid.NewGuid()}@test.com");
        context.Users.Add(user);

        var category = new TicketCategory { Name = "IT", Description = "IT", DefaultPriority = TicketPriority.Normal, SLAHours = 24 };
        context.TicketCategories.Add(category);

        var ticket = new Ticket
        {
            Title = "t",
            Description = "d",
            CategoryId = category.Id,
            Category = category,
            Priority = TicketPriority.Normal,
            Status = TicketStatus.Open,
            AssignedToId = user.Id,
            AssignedTo = user,
            SubmittedById = user.Id,
            SubmittedBy = user,
            DepartmentId = departmentId
        };
        context.Tickets.Add(ticket);

        await context.SaveChangesAsync();
        return (ticket, user.Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateEscalation_AndMarkTicketEscalated()
    {
        var (service, context) = CreateService();
        var (ticket, userId) = await SeedTicketAsync(context);

        var result = await service.CreateAsync(ticket.Id, userId, new CreateEscalationRequest("Needs manager attention", null));

        result.Status.Should().Be("Pending");
        result.Reason.Should().Be("Needs manager attention");

        var updatedTicket = await context.Tickets.FindAsync(ticket.Id);
        updatedTicket!.EscalatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptAsync_ShouldSetStatusAccepted_AndAssignee()
    {
        var (service, context) = CreateService();
        var (ticket, userId) = await SeedTicketAsync(context);
        var escalation = await service.CreateAsync(ticket.Id, userId, new CreateEscalationRequest("reason", null));
        var acceptedBy = Guid.NewGuid();

        await service.AcceptAsync(escalation.Id, acceptedBy);

        var updated = await context.Escalations.FindAsync(escalation.Id);
        updated!.Status.Should().Be(EscalationStatus.Accepted);
        updated.AssignedToId.Should().Be(acceptedBy);
    }

    [Fact]
    public async Task ResolveAsync_ShouldSetStatusResolved_AndResolvedAt()
    {
        var (service, context) = CreateService();
        var (ticket, userId) = await SeedTicketAsync(context);
        var escalation = await service.CreateAsync(ticket.Id, userId, new CreateEscalationRequest("reason", null));

        await service.ResolveAsync(escalation.Id, userId);

        var updated = await context.Escalations.FindAsync(escalation.Id);
        updated!.Status.Should().Be(EscalationStatus.Resolved);
        updated.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeclineAsync_ShouldSetStatusDeclined()
    {
        var (service, context) = CreateService();
        var (ticket, userId) = await SeedTicketAsync(context);
        var escalation = await service.CreateAsync(ticket.Id, userId, new CreateEscalationRequest("reason", null));

        await service.DeclineAsync(escalation.Id, userId);

        var updated = await context.Escalations.FindAsync(escalation.Id);
        updated!.Status.Should().Be(EscalationStatus.Declined);
    }

    [Theory]
    [InlineData("Accept")]
    [InlineData("Resolve")]
    [InlineData("Decline")]
    public async Task StatusChange_ShouldThrow_WhenEscalationNotFound(string action)
    {
        var (service, _) = CreateService();
        var missingId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Func<Task> act = action switch
        {
            "Accept" => () => service.AcceptAsync(missingId, userId),
            "Resolve" => () => service.ResolveAsync(missingId, userId),
            _ => () => service.DeclineAsync(missingId, userId),
        };

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOnlyPendingEscalations_ForDepartment()
    {
        var (service, context) = CreateService();
        var departmentId = Guid.NewGuid();
        var (ticket1, userId) = await SeedTicketAsync(context, departmentId);
        var (ticket2, _) = await SeedTicketAsync(context, departmentId);
        var (otherDeptTicket, _) = await SeedTicketAsync(context, Guid.NewGuid());

        var pending = await service.CreateAsync(ticket1.Id, userId, new CreateEscalationRequest("pending one", null));
        var toResolve = await service.CreateAsync(ticket2.Id, userId, new CreateEscalationRequest("resolved one", null));
        await service.CreateAsync(otherDeptTicket.Id, userId, new CreateEscalationRequest("other dept", null));
        await service.ResolveAsync(toResolve.Id, userId);

        var result = await service.GetPendingAsync(departmentId);

        result.Should().ContainSingle(e => e.Id == pending.Id);
    }
}
