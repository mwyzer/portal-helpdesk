using AIHelpdesk.Contracts.Auth;
using FluentAssertions;

namespace AIHelpdesk.Tests.Contracts;

public class AuthContractsTests
{
    [Fact]
    public void LoginRequest_ShouldBeRecordType()
    {
        var request = new LoginRequest("test@example.com", "Password123!");

        request.Email.Should().Be("test@example.com");
        request.Password.Should().Be("Password123!");
    }

    [Fact]
    public void UserInfo_ShouldCaptureAllFields()
    {
        var userInfo = new UserInfo(
            Guid.NewGuid(), "user@test.com", "Test User", "NIK-001",
            new List<string> { "Admin" }, new List<string> { "users.read", "users.create" });

        userInfo.FullName.Should().Be("Test User");
        userInfo.Email.Should().Be("user@test.com");
        userInfo.Roles.Should().Contain("Admin");
        userInfo.Permissions.Should().HaveCount(2);
    }

    [Fact]
    public void AuthResponse_ShouldHaveTokens()
    {
        var userInfo = new UserInfo(Guid.NewGuid(), "u@t.com", "U", "N", [], []);
        var response = new AuthResponse("access-token-123", "refresh-token-456", DateTime.UtcNow.AddMinutes(15), userInfo);

        response.AccessToken.Should().Be("access-token-123");
        response.RefreshToken.Should().Be("refresh-token-456");
    }
}
