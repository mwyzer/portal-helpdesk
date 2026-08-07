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
using Npgsql;

namespace AIHelpdesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(NormalizeConnectionString(configuration.GetConnectionString("DefaultConnection")))
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
        services.AddScoped<IExcelService, ExcelService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ILeaveTypeService, LeaveTypeService>();
        services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<INotificationService, NotificationService>();
        // Phase 5: Ticketing Module
        services.AddScoped<ITicketCategoryService, TicketCategoryService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IEscalationService, EscalationService>();
        services.AddScoped<IAgentAssignmentService, AgentAssignmentService>();
        services.AddHostedService<TicketSlaBackgroundService>();
        services.AddHostedService<ActionItemReminderBackgroundService>();
        // Phase 6: Recruitment Module
        services.AddScoped<IJobVacancyService, JobVacancyService>();
        services.AddScoped<ICandidateService, CandidateService>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<IRecruitmentAIService, RecruitmentAIService>();
        // Phase 8: Candidate Self-Service Portal
        services.AddScoped<ICandidatePortalService, CandidatePortalService>();
        services.Configure<AIOptions>(configuration.GetSection(AIOptions.SectionName));

        services.AddHttpClient<IAIService, AIService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        return services;
    }

    // Render (and other Heroku-style hosts) hand out managed Postgres credentials as a
    // postgres:// URI, which Npgsql's keyword-value parser rejects outright. Convert it to
    // Npgsql's connection string format; local/keyword-style connection strings pass through untouched.
    private static string? NormalizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString) ||
            !(connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require,
        };

        return builder.ConnectionString;
    }
}
