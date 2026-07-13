using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Application.Options;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIHelpdesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<IActionItemService, ActionItemService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<IChatService, ChatService>();
        // Phase 2: HR Module
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ILeaveTypeService, LeaveTypeService>();
        services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.Configure<AIOptions>(configuration.GetSection(AIOptions.SectionName));

        services.AddHttpClient<IAIService, AIService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        return services;
    }
}
