using AIHelpdesk.Domain.Entities;
using Bogus;
using Microsoft.AspNetCore.Identity;

namespace AIHelpdesk.Tests;

public static class TestDataFactory
{
    private static readonly Faker Faker = new();

    public static ApplicationUser CreateUser(
        string email = "test@example.com",
        string fullName = "Test User",
        string? nik = "NIK-001",
        bool isActive = true)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            NIK = nik,
            IsActive = isActive,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static ApplicationRole CreateRole(string name = "Test Role", string? description = "A test role")
    {
        return new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static Department CreateDepartment(string name = "IT", string code = "IT")
    {
        return new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Position CreatePosition(Guid departmentId, string name = "Developer")
    {
        return new Position
        {
            Id = Guid.NewGuid(),
            Name = name,
            DepartmentId = departmentId,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public static Permission CreatePermission(string name = "users.read", string group = "Users")
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Group = group,
            Description = $"Permission to {name}",
        };
    }

    public static RefreshToken CreateRefreshToken(Guid userId, bool isRevoked = false)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = "127.0.0.1",
            IsRevoked = isRevoked,
        };
    }
}
