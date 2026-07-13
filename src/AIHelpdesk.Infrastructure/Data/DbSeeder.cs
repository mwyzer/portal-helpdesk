using AIHelpdesk.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AIHelpdesk.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // ── Seed Roles ──
        var roles = new[]
        {
            ("Super Admin", "Full system access"),
            ("HRD", "HR department access"),
            ("Secretary", "Secretary module access"),
            ("Manager", "Manager-level access"),
            ("Employee", "Basic employee access"),
        };

        foreach (var (name, description) in roles)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = name,
                    Description = description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        // ── Seed Users ──
        var users = new[]
        {
            (FullName: "Super Admin", Email: "admin@aihelpdesk.com", Password: "Admin@123", Role: "Super Admin"),
            (FullName: "HRD User",    Email: "hrd@aihelpdesk.com",    Password: "Hrd@12345",    Role: "HRD"),
            (FullName: "Secretary",   Email: "secretary@aihelpdesk.com", Password: "Secretary@123", Role: "Secretary"),
            (FullName: "Manager",     Email: "manager@aihelpdesk.com", Password: "Manager@123",   Role: "Manager"),
            (FullName: "Employee",    Email: "employee@aihelpdesk.com", Password: "Employee@123",  Role: "Employee"),
        };

        foreach (var (fullName, email, password, role) in users)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser != null) continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Failed to create user {email}: {errors}");
            }
        }

        // ── Seed Leave Types ──
        var leaveTypes = new[]
        {
            ("Annual Leave", "AL", 12, true, 0, false, false),
            ("Sick Leave", "SL", 14, true, 0, true, true),
            ("Special Leave", "SPL", 5, true, 0, false, false),
            ("Maternity Leave", "ML", 90, true, 12, false, false),
            ("Paternity Leave", "PL", 5, true, 6, false, false),
            ("Lateness", "LT", 0, false, 0, false, false),
            ("Early Leave", "EL", 0, false, 0, false, false),
            ("Work From Home", "WFH", 0, true, 0, false, false),
        };

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach (var (name, code, daysPerYear, isPaid, minServiceMonths, requiresAttachment, skipManagerApproval) in leaveTypes)
        {
            if (!await context.LeaveTypes.AnyAsync(lt => lt.Code == code))
            {
                context.LeaveTypes.Add(new LeaveType
                {
                    Name = name,
                    Code = code,
                    DaysPerYear = daysPerYear,
                    IsPaid = isPaid,
                    MinServiceMonths = minServiceMonths,
                    RequiresAttachment = requiresAttachment,
                    SkipManagerApproval = skipManagerApproval,
                    IsActive = true
                });
            }
        }
        await context.SaveChangesAsync();
    }
}
