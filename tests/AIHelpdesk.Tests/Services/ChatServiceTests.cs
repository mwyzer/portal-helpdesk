using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Chat;
using AIHelpdesk.Contracts.Knowledge;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Domain.Common;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AIHelpdesk.Tests.Services;

public class ChatServiceTests
{
    private static async Task<(ChatService Service, ApplicationDbContext Context, Mock<IAIService> AIMock, Mock<IKnowledgeBaseService> KBMock)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ChatTestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var aiMock = new Mock<IAIService>();
        var kbMock = new Mock<IKnowledgeBaseService>();

        aiMock.Setup(a => a.EstimateTokenCount(It.IsAny<string>())).Returns((string s) => s.Length / 4);
        kbMock.Setup(k => k.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<KnowledgeSearchResult>
            {
                new(Guid.NewGuid(), "Handbook", Guid.NewGuid(), "Related content snippet for the query.", 0.85)
            });

        var service = new ChatService(context, aiMock.Object, kbMock.Object);
        return (service, context, aiMock, kbMock);
    }

    // ── SendMessageAsync ──

    [Fact]
    public async Task SendMessageAsync_ShouldCreateNewSession_WhenNoSessionId()
    {
        var (service, context, aiMock, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();

        aiMock.Setup(a => a.GenerateChatResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<(string, string)>>(), It.IsAny<Action<string>>()))
            .ReturnsAsync("Here is the answer.");

        var request = new SendMessageRequest(null, "What is the leave policy?");
        var result = await service.SendMessageAsync(userId, request);

        result.Title.Should().Be("What is the leave policy?");
        result.Status.Should().Be("Active");
        result.Messages.Should().HaveCount(2); // user + assistant
        result.Messages[0].Role.Should().Be("user");
        result.Messages[0].Content.Should().Be("What is the leave policy?");
        result.Messages[1].Role.Should().Be("assistant");
        result.Messages[1].Content.Should().Be("Here is the answer.");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldAppendToExistingSession()
    {
        var (service, context, aiMock, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var session = TestDataFactory.CreateChatSession(userId);
        context.ChatSessions.Add(session);
        await context.SaveChangesAsync();

        aiMock.Setup(a => a.GenerateChatResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<(string, string)>>(), It.IsAny<Action<string>>()))
            .ReturnsAsync("Second answer.");

        var request = new SendMessageRequest(session.Id, "Follow-up question?");
        var result = await service.SendMessageAsync(userId, request);

        result.Id.Should().Be(session.Id);
        result.Messages.Should().HaveCount(2); // 1 user + 1 assistant for this round
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrow_WhenSessionNotFound()
    {
        var (service, _, _, _) = await CreateServiceAsync();
        var request = new SendMessageRequest(Guid.NewGuid(), "Hello?");

        var act = () => service.SendMessageAsync(Guid.NewGuid(), request);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldTruncateTitle_WhenLongMessage()
    {
        var (service, _, aiMock, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();

        aiMock.Setup(a => a.GenerateChatResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<(string, string)>>(), It.IsAny<Action<string>>()))
            .ReturnsAsync("Short answer.");

        var longMessage = new string('x', 100);
        var request = new SendMessageRequest(null, longMessage);
        var result = await service.SendMessageAsync(userId, request);

        result.Title.Should().Be(new string('x', 50) + "...");
        result.Title.Length.Should().Be(53); // 50 chars + "..."
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSaveAIResponseMetadata()
    {
        var (service, context, aiMock, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        aiMock.Setup(a => a.GenerateChatResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<(string, string)>>(), It.IsAny<Action<string>>()))
            .ReturnsAsync("Answer.");

        var request = new SendMessageRequest(null, "Question?");
        var result = await service.SendMessageAsync(userId, request);

        var aiResponse = await context.AIResponses.FirstOrDefaultAsync();
        aiResponse.Should().NotBeNull();
        aiResponse!.ModelUsed.Should().Be("gpt-4o-mini");
        aiResponse!.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldLogUsage()
    {
        var (service, context, aiMock, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        aiMock.Setup(a => a.GenerateChatResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<(string, string)>>(), It.IsAny<Action<string>>()))
            .ReturnsAsync("Answer.");

        var request = new SendMessageRequest(null, "Question?");
        await service.SendMessageAsync(userId, request);

        var usageLog = await context.AIUsageLogs.FirstOrDefaultAsync();
        usageLog.Should().NotBeNull();
        usageLog!.UserId.Should().Be(userId);
        usageLog.Endpoint.Should().Be("chat/completions");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldCallOnToken()
    {
        var (service, _, aiMock, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var tokens = new List<string>();

        aiMock.Setup(a => a.GenerateChatResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<(string, string)>>(), It.IsAny<Action<string>>()))
            .Callback<string, string, List<(string, string)>, Action<string>?>((_, _, _, onToken) =>
            {
                onToken?.Invoke("token1");
                onToken?.Invoke("token2");
            })
            .ReturnsAsync("token1token2");

        var request = new SendMessageRequest(null, "Q?");
        await service.SendMessageAsync(userId, request, onToken: t => tokens.Add(t));

        tokens.Should().Equal("token1", "token2");
    }

    // ── GetSessionAsync ──

    [Fact]
    public async Task GetSessionAsync_ShouldReturnSession()
    {
        var (service, context, _, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var session = TestDataFactory.CreateChatSession(userId, title: "My Chat");
        var msg = TestDataFactory.CreateChatMessage(session.Id, content: "Hello");
        session.Messages.Add(msg);
        context.ChatSessions.Add(session);
        await context.SaveChangesAsync();

        var result = await service.GetSessionAsync(session.Id, userId);

        result.Id.Should().Be(session.Id);
        result.Title.Should().Be("My Chat");
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Content.Should().Be("Hello");
    }

    [Fact]
    public async Task GetSessionAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _, _, _) = await CreateServiceAsync();
        var act = () => service.GetSessionAsync(Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetSessionsAsync ──

    [Fact]
    public async Task GetSessionsAsync_ShouldReturnPaginated()
    {
        var (service, context, _, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        for (int i = 0; i < 12; i++)
        {
            context.ChatSessions.Add(TestDataFactory.CreateChatSession(userId, $"Chat {i}"));
        }
        await context.SaveChangesAsync();

        var result = await service.GetSessionsAsync(userId, 1, 5);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(12);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetSessionsAsync_ShouldReturnOnlyUserSessions()
    {
        var (service, context, _, _) = await CreateServiceAsync();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        context.ChatSessions.Add(TestDataFactory.CreateChatSession(userId1, "User 1 Chat"));
        context.ChatSessions.Add(TestDataFactory.CreateChatSession(userId1, "User 1 Chat 2"));
        context.ChatSessions.Add(TestDataFactory.CreateChatSession(userId2, "User 2 Chat"));
        await context.SaveChangesAsync();

        var result = await service.GetSessionsAsync(userId1, 1, 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    // ── DeleteSessionAsync ──

    [Fact]
    public async Task DeleteSessionAsync_ShouldSoftDelete()
    {
        var (service, context, _, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var session = TestDataFactory.CreateChatSession(userId);
        context.ChatSessions.Add(session);
        await context.SaveChangesAsync();

        await service.DeleteSessionAsync(session.Id, userId);

        var deleted = await context.ChatSessions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == session.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _, _, _) = await CreateServiceAsync();
        var act = () => service.DeleteSessionAsync(Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldThrow_WhenNotOwner()
    {
        var (service, context, _, _) = await CreateServiceAsync();
        var ownerId = Guid.NewGuid();
        var session = TestDataFactory.CreateChatSession(ownerId);
        context.ChatSessions.Add(session);
        await context.SaveChangesAsync();

        var act = () => service.DeleteSessionAsync(session.Id, Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateSessionAsync ──

    [Fact]
    public async Task UpdateSessionAsync_ShouldUpdateTitle()
    {
        var (service, context, _, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var session = TestDataFactory.CreateChatSession(userId, "Old Title");
        context.ChatSessions.Add(session);
        await context.SaveChangesAsync();

        var result = await service.UpdateSessionAsync(session.Id, userId, new UpdateSessionRequest("New Title"));

        result.Title.Should().Be("New Title");
        context.ChangeTracker.Clear();
        var updated = await context.ChatSessions.FindAsync(session.Id);
        updated!.Title.Should().Be("New Title");
    }

    // ── SubmitFeedbackAsync ──

    [Fact]
    public async Task SubmitFeedbackAsync_ShouldSetScore()
    {
        var (service, context, _, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var session = TestDataFactory.CreateChatSession(userId);
        var message = TestDataFactory.CreateChatMessage(session.Id, ChatMessageRole.Assistant);
        session.Messages.Add(message);
        context.ChatSessions.Add(session);
        var aiResponse = TestDataFactory.CreateAIResponse(message.Id);
        context.AIResponses.Add(aiResponse);
        await context.SaveChangesAsync();

        await service.SubmitFeedbackAsync(message.Id, userId, new SubmitFeedbackRequest(1, "Helpful!"));

        context.ChangeTracker.Clear();
        var updated = await context.AIResponses.FindAsync(aiResponse.Id);
        updated!.FeedbackScore.Should().Be(1);
        updated.FeedbackComment.Should().Be("Helpful!");
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ShouldThrow_WhenMessageNotFound()
    {
        var (service, _, _, _) = await CreateServiceAsync();
        var act = () => service.SubmitFeedbackAsync(Guid.NewGuid(), Guid.NewGuid(),
            new SubmitFeedbackRequest(1, null));
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── EscalateSessionAsync ──

    [Fact]
    public async Task EscalateSessionAsync_ShouldSetEscalated()
    {
        var (service, context, _, _) = await CreateServiceAsync();
        var userId = Guid.NewGuid();
        var session = TestDataFactory.CreateChatSession(userId);
        context.ChatSessions.Add(session);
        await context.SaveChangesAsync();

        var result = await service.EscalateSessionAsync(session.Id, userId);

        result.Status.Should().Be("Escalated");
        context.ChangeTracker.Clear();
        var updated = await context.ChatSessions.FindAsync(session.Id);
        updated!.Status.Should().Be(ChatSessionStatus.Escalated);
    }

    [Fact]
    public async Task EscalateSessionAsync_ShouldThrow_WhenNotFound()
    {
        var (service, _, _, _) = await CreateServiceAsync();
        var act = () => service.EscalateSessionAsync(Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
