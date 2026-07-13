using AIHelpdesk.Domain.Entities;
using FluentAssertions;

namespace AIHelpdesk.Tests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_ShouldReturnTrue_WhenTokenIsValid()
    {
        var token = TestDataFactory.CreateRefreshToken(Guid.NewGuid());

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenTokenIsRevoked()
    {
        var token = TestDataFactory.CreateRefreshToken(Guid.NewGuid(), isRevoked: true);

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenTokenIsExpired()
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false,
        };

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }
}
