using AIHelpdesk.Contracts.Auth;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIHelpdesk.Tests.Services;

public class AuthServiceTests
{
    private static (AuthService Service, ApplicationDbContext Context, UserManager<ApplicationUser> UserManager)
        CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);

        var userStore = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>(context);
        var userManager = new UserManager<ApplicationUser>(
            userStore, null!, new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);

        var tokenProvider = new DataProtectorTokenProvider<ApplicationUser>(
            new EphemeralDataProtectionProvider(), Options.Create(new DataProtectionTokenProviderOptions()),
            NullLogger<DataProtectorTokenProvider<ApplicationUser>>.Instance);
        userManager.RegisterTokenProvider(TokenOptions.DefaultProvider, tokenProvider);

        var roleStore = new RoleStore<ApplicationRole, ApplicationDbContext, Guid>(context);
        var roleManager = new RoleManager<ApplicationRole>(
            roleStore, Array.Empty<IRoleValidator<ApplicationRole>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestOnlySigningKeyThatIsLongEnough1234567890!",
                ["Jwt:Issuer"] = "AIHelpdeskTests",
                ["Jwt:Audience"] = "AIHelpdeskTests",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
            })
            .Build();
        var tokenService = new TokenService(configuration);

        var service = new AuthService(userManager, roleManager, context, tokenService, NullLogger<AuthService>.Instance);
        return (service, context, userManager);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_ForValidCredentials()
    {
        var (service, _, userManager) = CreateService();
        var user = TestDataFactory.CreateUser("login@test.com");
        await userManager.CreateAsync(user, "Password123!");

        var result = await service.LoginAsync(new LoginRequest("login@test.com", "Password123!"), "127.0.0.1");

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be("login@test.com");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_ForInvalidPassword()
    {
        var (service, _, userManager) = CreateService();
        var user = TestDataFactory.CreateUser("wrongpw@test.com");
        await userManager.CreateAsync(user, "Password123!");

        var act = () => service.LoginAsync(new LoginRequest("wrongpw@test.com", "WrongPassword!"), null);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenUserInactive()
    {
        var (service, _, userManager) = CreateService();
        var user = TestDataFactory.CreateUser("inactive@test.com", isActive: false);
        await userManager.CreateAsync(user, "Password123!");

        var act = () => service.LoginAsync(new LoginRequest("inactive@test.com", "Password123!"), null);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRotateToken()
    {
        var (service, context, userManager) = CreateService();
        var user = TestDataFactory.CreateUser("refresh@test.com");
        await userManager.CreateAsync(user, "Password123!");

        var login = await service.LoginAsync(new LoginRequest("refresh@test.com", "Password123!"), "127.0.0.1");

        var refreshed = await service.RefreshTokenAsync(
            new RefreshTokenRequest(login.AccessToken, login.RefreshToken), "127.0.0.1");

        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);

        var oldToken = await context.RefreshTokens.FirstAsync(rt => rt.Token == login.RefreshToken);
        oldToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeRefreshToken()
    {
        var (service, context, userManager) = CreateService();
        var user = TestDataFactory.CreateUser("logout@test.com");
        await userManager.CreateAsync(user, "Password123!");
        var login = await service.LoginAsync(new LoginRequest("logout@test.com", "Password123!"), null);

        await service.LogoutAsync(login.RefreshToken);

        var token = await context.RefreshTokens.FirstAsync(rt => rt.Token == login.RefreshToken);
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ShouldNotThrow_ForUnknownEmail()
    {
        var (service, _, _) = CreateService();

        var act = () => service.ForgotPasswordAsync(new ForgotPasswordRequest("nobody@test.com"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForgotPasswordAsync_ThenResetPasswordAsync_ShouldChangePassword()
    {
        var (service, _, userManager) = CreateService();
        var user = TestDataFactory.CreateUser("forgot@test.com");
        await userManager.CreateAsync(user, "OldPassword123!");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await service.ForgotPasswordAsync(new ForgotPasswordRequest("forgot@test.com"));

        await service.ResetPasswordAsync(new ResetPasswordRequest("forgot@test.com", token, "NewPassword123!"));

        (await userManager.CheckPasswordAsync(user, "NewPassword123!")).Should().BeTrue();
        (await userManager.CheckPasswordAsync(user, "OldPassword123!")).Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrow_ForInvalidToken()
    {
        var (service, _, userManager) = CreateService();
        var user = TestDataFactory.CreateUser("badtoken@test.com");
        await userManager.CreateAsync(user, "Password123!");

        var act = () => service.ResetPasswordAsync(
            new ResetPasswordRequest("badtoken@test.com", "not-a-real-token", "NewPassword123!"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
