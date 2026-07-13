using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Users;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class UserServiceTests
{
    private async Task<(UserService, ApplicationDbContext)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>(context);
        var userManager = new UserManager<ApplicationUser>(
            store, null!, new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, null!);

        var service = new UserService(userManager, context);
        return (service, context);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        var (service, _) = await CreateServiceAsync();

        var result = await service.GetUsersAsync(1, 10, null, null);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser()
    {
        var (service, context) = await CreateServiceAsync();

        var dept = TestDataFactory.CreateDepartment();
        var pos = TestDataFactory.CreatePosition(dept.Id);
        context.Departments.Add(dept);
        context.Positions.Add(pos);
        await context.SaveChangesAsync();

        var roleStore = new RoleStore<ApplicationRole, ApplicationDbContext, Guid>(context);
        using var roleManager = new RoleManager<ApplicationRole>(
            roleStore, Array.Empty<IRoleValidator<ApplicationRole>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!);
        var role = TestDataFactory.CreateRole();
        await roleManager.CreateAsync(role);

        var request = new CreateUserRequest(
            "newuser@test.com", "Password123!", "New User", "NIK-002",
            dept.Id, pos.Id, new List<Guid> { role.Id });

        var result = await service.CreateUserAsync(request);

        result.Email.Should().Be("newuser@test.com");
        result.FullName.Should().Be("New User");
        result.IsActive.Should().BeTrue();

        var users = await service.GetUsersAsync(1, 10, null, null);
        users.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUserAsync_ShouldThrow_WhenUserNotFound()
    {
        var (service, _) = await CreateServiceAsync();

        var act = () => service.GetUserAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ActivateDeactivateUser_ShouldToggleStatus()
    {
        var (service, context) = await CreateServiceAsync();
        var user = TestDataFactory.CreateUser(isActive: true);

        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>(context);
        var userManager = new UserManager<ApplicationUser>(
            store, null!, new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, null!);
        await userManager.CreateAsync(user);

        await service.DeactivateUserAsync(user.Id);
        var deactivated = await service.GetUserAsync(user.Id);
        deactivated.IsActive.Should().BeFalse();

        await service.ActivateUserAsync(user.Id);
        var activated = await service.GetUserAsync(user.Id);
        activated.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsersAsync_ShouldSupportPagination()
    {
        var (service, context) = await CreateServiceAsync();
        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>(context);
        var userManager = new UserManager<ApplicationUser>(
            store, null!, new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!, null!);

        for (int i = 0; i < 15; i++)
            await userManager.CreateAsync(TestDataFactory.CreateUser($"user{i}@test.com", $"User {i}"));

        var page1 = await service.GetUsersAsync(1, 5, null, null);
        page1.Items.Should().HaveCount(5);
        page1.TotalCount.Should().Be(15);

        var page2 = await service.GetUsersAsync(2, 5, null, null);
        page2.Items.Should().HaveCount(5);
    }
}
