using AIHelpdesk.Contracts.ActionItems;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class ActionItemServiceTests
{
    private static async Task<(ActionItemService Service, ApplicationDbContext Context)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new ActionItemService(context);
        return (service, context);
    }

    [Fact]
    public async Task CreateActionItemAsync_ShouldCreate()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser(email: "a@test.com", fullName: "Alice");
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        var request = new CreateActionItemRequest(
            meeting.Id, "Review PR", "Check PR #42",
            user.Id, DateTime.UtcNow.AddDays(2), "High");

        var result = await service.CreateActionItemAsync(request);

        result.Title.Should().Be("Review PR");
        result.Priority.Should().Be("High");
        result.Status.Should().Be("Open");
        result.AssignedToName.Should().Be("Alice");
        result.MeetingId.Should().Be(meeting.Id);
    }

    [Fact]
    public async Task CreateActionItemAsync_ShouldHandleNullMeeting()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser(fullName: "Bob");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var request = new CreateActionItemRequest(
            null, "Standalone Item", "No meeting",
            user.Id, DateTime.UtcNow.AddDays(1), "Low");

        var result = await service.CreateActionItemAsync(request);

        result.Title.Should().Be("Standalone Item");
        result.MeetingId.Should().BeNull();
        result.Priority.Should().Be("Low");
    }

    [Fact]
    public async Task GetMyActionItemsAsync_ShouldReturnUserItems()
    {
        var (service, context) = await CreateServiceAsync();
        var alice = TestDataFactory.CreateUser(fullName: "Alice");
        var bob = TestDataFactory.CreateUser(fullName: "Bob");
        context.Users.AddRange(alice, bob);
        var meeting = TestDataFactory.CreateMeeting(alice.Id);
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        context.ActionItems.Add(TestDataFactory.CreateActionItem(meeting.Id, alice.Id, "Alice's task"));
        context.ActionItems.Add(TestDataFactory.CreateActionItem(meeting.Id, bob.Id, "Bob's task"));
        context.ActionItems.Add(TestDataFactory.CreateActionItem(meeting.Id, alice.Id, "Alice's task 2"));
        await context.SaveChangesAsync();

        var result = await service.GetMyActionItemsAsync(alice.Id, 1, 10, null);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(i => i.AssignedToName.Should().Be("Alice"));
    }

    [Fact]
    public async Task GetMyActionItemsAsync_ShouldFilterByStatus()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        context.ActionItems.Add(TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Open", status: ActionItemStatus.Open));
        context.ActionItems.Add(TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Done", status: ActionItemStatus.Completed));
        await context.SaveChangesAsync();

        var result = await service.GetMyActionItemsAsync(user.Id, 1, 10, "Open");

        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Open");
    }

    [Fact]
    public async Task GetOverdueActionItemsAsync_ShouldReturnOverdue()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        context.ActionItems.Add(TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Overdue", dueDate: DateTime.UtcNow.AddDays(-2), status: ActionItemStatus.Open));
        context.ActionItems.Add(TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Future", dueDate: DateTime.UtcNow.AddDays(5), status: ActionItemStatus.Open));
        context.ActionItems.Add(TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Overdue Done", dueDate: DateTime.UtcNow.AddDays(-1), status: ActionItemStatus.Completed));
        await context.SaveChangesAsync();

        var result = await service.GetOverdueActionItemsAsync(user.Id);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Overdue");
    }

    [Fact]
    public async Task UpdateActionItemAsync_ShouldUpdateFields()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser(fullName: "Alice");
        var newAssignee = TestDataFactory.CreateUser(fullName: "Bob");
        context.Users.AddRange(user, newAssignee);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        var item = TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Old", priority: ActionItemPriority.Low);
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        var request = new UpdateActionItemRequest(
            "Updated Task", "New description", newAssignee.Id,
            DateTime.UtcNow.AddDays(7), "Urgent");

        var result = await service.UpdateActionItemAsync(item.Id, request);

        result.Title.Should().Be("Updated Task");
        result.Priority.Should().Be("Urgent");
        result.AssignedToName.Should().Be("Bob");
    }

    [Fact]
    public async Task CompleteActionItemAsync_ShouldMarkCompleted()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        var item = TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Task", status: ActionItemStatus.Open);
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        var result = await service.CompleteActionItemAsync(item.Id, user.Id);

        result.Status.Should().Be("Completed");
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteActionItemAsync_ShouldThrow_WhenNotAssignee()
    {
        var (service, context) = await CreateServiceAsync();
        var alice = TestDataFactory.CreateUser();
        var bob = TestDataFactory.CreateUser();
        context.Users.AddRange(alice, bob);
        var meeting = TestDataFactory.CreateMeeting(alice.Id);
        context.Meetings.Add(meeting);
        var item = TestDataFactory.CreateActionItem(meeting.Id, alice.Id, "Alice's task");
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        await service.Invoking(s => s.CompleteActionItemAsync(item.Id, bob.Id))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CancelActionItemAsync_ShouldMarkCancelled()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        var item = TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Cancel me", status: ActionItemStatus.Open);
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        var result = await service.CancelActionItemAsync(item.Id);

        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task GetActionItemByIdAsync_ShouldReturnItem()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser(fullName: "Alice");
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        var item = TestDataFactory.CreateActionItem(meeting.Id, user.Id, "Find me");
        context.ActionItems.Add(item);
        await context.SaveChangesAsync();

        var result = await service.GetActionItemByIdAsync(item.Id);

        result.Title.Should().Be("Find me");
        result.AssignedToName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetActionItemByIdAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = await CreateServiceAsync();

        await service.Invoking(s => s.GetActionItemByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}
