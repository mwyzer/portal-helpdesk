using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Tests.Services;

public class RoleServiceTests
{
    private async Task<(RoleService, ApplicationDbContext, RoleManager<ApplicationRole>)> CreateServiceAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        var store = new RoleStore<ApplicationRole, ApplicationDbContext, Guid>(context);
        var roleManager = new RoleManager<ApplicationRole>(
            store, Array.Empty<IRoleValidator<ApplicationRole>>(),
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!);

        var perm1 = TestDataFactory.CreatePermission("users.read", "Users");
        var perm2 = TestDataFactory.CreatePermission("users.create", "Users");
        context.Permissions.AddRange(perm1, perm2);
        await context.SaveChangesAsync();

        var service = new RoleService(roleManager, context);
        return (service, context, roleManager);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldCreateRole()
    {
        var (service, _, roleManager) = await CreateServiceAsync();

        var result = await service.CreateRoleAsync(new("Admin", "Administrator role", []));

        result.Name.Should().Be("Admin");
        result.Description.Should().Be("Administrator role");
        result.UserCount.Should().Be(0);

        var exists = await roleManager.RoleExistsAsync("Admin");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnRoles()
    {
        var (service, _, roleManager) = await CreateServiceAsync();

        await roleManager.CreateAsync(new ApplicationRole { Name = "Role1" });
        await roleManager.CreateAsync(new ApplicationRole { Name = "Role2" });

        var result = await service.GetRolesAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldDeleteRole()
    {
        var (service, _, roleManager) = await CreateServiceAsync();

        var role = new ApplicationRole { Name = "TempRole" };
        await roleManager.CreateAsync(role);

        var roles = await service.GetRolesAsync();
        roles.Should().HaveCount(1);

        await service.DeleteRoleAsync(role.Id);

        var after = await service.GetRolesAsync();
        after.Should().HaveCount(0);
    }

    [Fact]
    public async Task GetAllPermissionsAsync_ShouldReturnAll()
    {
        var (service, _, _) = await CreateServiceAsync();

        var perms = await service.GetAllPermissionsAsync();

        perms.Should().HaveCount(2);
        perms.Select(p => p.Name).Should().Contain(new[] { "users.read", "users.create" });
    }
}
