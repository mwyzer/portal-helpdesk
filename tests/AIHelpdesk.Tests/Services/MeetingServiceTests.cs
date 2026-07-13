using AIHelpdesk.Contracts.Meetings;
using AIHelpdesk.Contracts.ActionItems;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class MeetingServiceTests
{
    private static async Task<(MeetingService Service, ApplicationDbContext Context)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var service = new MeetingService(context);
        return (service, context);
    }

    [Fact]
    public async Task CreateMeetingAsync_ShouldCreateMeeting()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser(email: "org@test.com", fullName: "Organizer");
        context.Users.Add(organizer);
        await context.SaveChangesAsync();

        var request = new CreateMeetingRequest(
            "Sprint Planning", DateTime.UtcNow.Date.AddDays(1),
            new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0),
            "Room A", null, "Plan the sprint");

        var result = await service.CreateMeetingAsync(organizer.Id, request);

        result.Title.Should().Be("Sprint Planning");
        result.Status.Should().Be("Scheduled");
        result.Location.Should().Be("Room A");
        result.ParticipantCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateMeetingAsync_ShouldThrow_WhenEndTimeBeforeStartTime()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser();
        context.Users.Add(organizer);
        await context.SaveChangesAsync();

        var request = new CreateMeetingRequest(
            "Bad Meeting", DateTime.UtcNow.Date,
            new TimeSpan(10, 0, 0), new TimeSpan(9, 0, 0),
            null, null, null);

        await service.Invoking(s => s.CreateMeetingAsync(organizer.Id, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("End time must be after start time");
    }

    [Fact]
    public async Task GetMeetingsAsync_ShouldReturnPaginatedResults()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser();
        context.Users.Add(organizer);
        for (int i = 0; i < 8; i++)
        {
            context.Meetings.Add(TestDataFactory.CreateMeeting(organizer.Id, title: $"Meeting {i}"));
        }
        await context.SaveChangesAsync();

        var result = await service.GetMeetingsAsync(1, 5, null, null, null);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(8);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetMeetingsAsync_ShouldFilterByStatus()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser();
        context.Users.Add(organizer);
        context.Meetings.Add(TestDataFactory.CreateMeeting(organizer.Id, title: "Scheduled", status: MeetingStatus.Scheduled));
        context.Meetings.Add(TestDataFactory.CreateMeeting(organizer.Id, title: "Completed", status: MeetingStatus.Completed));
        context.Meetings.Add(TestDataFactory.CreateMeeting(organizer.Id, title: "Scheduled 2", status: MeetingStatus.Scheduled));
        await context.SaveChangesAsync();

        var result = await service.GetMeetingsAsync(1, 10, null, null, "Scheduled");

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(m => m.Status.Should().Be("Scheduled"));
    }

    [Fact]
    public async Task GetMeetingByIdAsync_ShouldReturnDetail()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser(email: "org@test.com", fullName: "Alice");
        var employee = TestDataFactory.CreateUser(email: "emp@test.com", fullName: "Bob");
        context.Users.AddRange(organizer, employee);
        await context.SaveChangesAsync();

        var meeting = TestDataFactory.CreateMeeting(organizer.Id, title: "Sync");
        context.Meetings.Add(meeting);

        var participant = TestDataFactory.CreateMeetingParticipant(meeting.Id, employee.Id);
        context.MeetingParticipants.Add(participant);

        var note = TestDataFactory.CreateMeetingNote(meeting.Id, "Notes", "Some content");
        context.MeetingNotes.Add(note);

        var actionItem = TestDataFactory.CreateActionItem(meeting.Id, employee.Id, "Task 1");
        context.ActionItems.Add(actionItem);

        await context.SaveChangesAsync();

        var result = await service.GetMeetingByIdAsync(meeting.Id);

        result.Title.Should().Be("Sync");
        result.OrganizerName.Should().Be("Alice");
        result.Participants.Should().HaveCount(1);
        result.Participants[0].EmployeeName.Should().Be("Bob");
        result.MeetingNotes.Should().HaveCount(1);
        result.ActionItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMeetingByIdAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _) = await CreateServiceAsync();

        await service.Invoking(s => s.GetMeetingByIdAsync(Guid.NewGuid()))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateMeetingAsync_ShouldUpdateFields()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser(fullName: "Alice");
        context.Users.Add(organizer);
        var meeting = TestDataFactory.CreateMeeting(organizer.Id, title: "Old Title");
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        var request = new UpdateMeetingRequest(
            "New Title", DateTime.UtcNow.Date.AddDays(2),
            new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0),
            "Room B", "https://meet.link", "Updated description");

        var result = await service.UpdateMeetingAsync(meeting.Id, request);

        result.Title.Should().Be("New Title");
        result.Location.Should().Be("Room B");
        result.MeetingLink.Should().Be("https://meet.link");
    }

    [Fact]
    public async Task DeleteMeetingAsync_ShouldSoftDelete()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser();
        context.Users.Add(organizer);
        var meeting = TestDataFactory.CreateMeeting(organizer.Id);
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        await service.DeleteMeetingAsync(meeting.Id);

        var deleted = await context.Meetings.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == meeting.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task AddParticipantAsync_ShouldAddParticipant()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser();
        var employee = TestDataFactory.CreateUser(email: "emp@test.com", fullName: "Bob");
        context.Users.AddRange(organizer, employee);
        var meeting = TestDataFactory.CreateMeeting(organizer.Id);
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        var request = new AddParticipantRequest(employee.Id, "Attendee", true);
        var result = await service.AddParticipantAsync(meeting.Id, request);

        result.EmployeeName.Should().Be("Bob");
        result.Role.Should().Be("Attendee");
        result.IsRequired.Should().BeTrue();
        result.AttendanceStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task RemoveParticipantAsync_ShouldSoftDelete()
    {
        var (service, context) = await CreateServiceAsync();
        var organizer = TestDataFactory.CreateUser();
        var employee = TestDataFactory.CreateUser(email: "bob@test.com");
        context.Users.AddRange(organizer, employee);
        var meeting = TestDataFactory.CreateMeeting(organizer.Id);
        context.Meetings.Add(meeting);
        var participant = TestDataFactory.CreateMeetingParticipant(meeting.Id, employee.Id);
        context.MeetingParticipants.Add(participant);
        await context.SaveChangesAsync();

        await service.RemoveParticipantAsync(meeting.Id, participant.Id);

        var deleted = await context.MeetingParticipants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == participant.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetTodayMeetingsAsync_ShouldReturnTodayMeetings()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser(email: "u@test.com", fullName: "User");
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id, title: "Today Sync", date: DateTime.UtcNow.Date);
        context.Meetings.Add(meeting);
        // Old meeting — should NOT appear
        context.Meetings.Add(TestDataFactory.CreateMeeting(user.Id, title: "Old Meeting", date: DateTime.UtcNow.Date.AddDays(-5)));
        await context.SaveChangesAsync();

        var result = await service.GetTodayMeetingsAsync(user.Id);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Today Sync");
    }

    [Fact]
    public async Task GetUpcomingMeetingsAsync_ShouldReturnNext7Days()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser(email: "u@test.com");
        context.Users.Add(user);
        context.Meetings.Add(TestDataFactory.CreateMeeting(user.Id, title: "In 3 days", date: DateTime.UtcNow.Date.AddDays(3)));
        context.Meetings.Add(TestDataFactory.CreateMeeting(user.Id, title: "Past", date: DateTime.UtcNow.Date.AddDays(-1)));
        context.Meetings.Add(TestDataFactory.CreateMeeting(user.Id, title: "Too far", date: DateTime.UtcNow.Date.AddDays(10)));
        await context.SaveChangesAsync();

        var result = await service.GetUpcomingMeetingsAsync(user.Id);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("In 3 days");
    }

    [Fact]
    public async Task AddNoteAsync_ShouldCreateNote()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser(fullName: "Alice");
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        await context.SaveChangesAsync();

        var request = new CreateMeetingNoteRequest("Action Items", "1. Review PR\n2. Update docs");
        var result = await service.AddNoteAsync(meeting.Id, user.Id, request);

        result.Title.Should().Be("Action Items");
        result.Content.Should().Contain("Review PR");
        result.IsAISummary.Should().BeFalse();
        result.CreatedByName.Should().Be("Alice");
    }

    [Fact]
    public async Task UpdateNoteAsync_ShouldUpdateFields()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        var note = TestDataFactory.CreateMeetingNote(meeting.Id, "Old Title", "Old content");
        context.MeetingNotes.Add(note);
        await context.SaveChangesAsync();

        var request = new UpdateMeetingNoteRequest("New Title", "New content");
        var result = await service.UpdateNoteAsync(meeting.Id, note.Id, request);

        result.Title.Should().Be("New Title");
        result.Content.Should().Be("New content");
    }

    [Fact]
    public async Task DeleteNoteAsync_ShouldSoftDelete()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        var meeting = TestDataFactory.CreateMeeting(user.Id);
        context.Meetings.Add(meeting);
        var note = TestDataFactory.CreateMeetingNote(meeting.Id);
        context.MeetingNotes.Add(note);
        await context.SaveChangesAsync();

        await service.DeleteNoteAsync(meeting.Id, note.Id);

        var deleted = await context.MeetingNotes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == note.Id);
        deleted!.IsDeleted.Should().BeTrue();
    }
}
