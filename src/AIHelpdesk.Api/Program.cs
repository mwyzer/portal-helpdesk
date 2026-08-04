using System.Text;
using AIHelpdesk.Api.Middleware;
using AIHelpdesk.Application;
using AIHelpdesk.Infrastructure;
using AIHelpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
})
// Separate scheme + audience for the candidate self-service portal (see
// TokenService.GenerateCandidatePortalToken) -- a staff-issued token is rejected here (wrong
// audience) and a candidate-issued token is rejected by the default scheme above, so a
// candidate JWT cannot satisfy [Authorize] on any internal endpoint even if one were
// misconfigured without a role check.
.AddJwtBearer(AIHelpdesk.Infrastructure.Services.TokenService.CandidatePortalScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = AIHelpdesk.Infrastructure.Services.TokenService.CandidatePortalAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

app.UseResponseCompression();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

await DbSeeder.SeedAsync(app.Services);

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // HSTS/HTTPS redirect only outside Development: the local docker-compose setup and
    // `dotnet run` both serve plain HTTP with no cert, so enabling these there would break
    // every request. In production this assumes a reverse proxy or Kestrel terminates TLS.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<AIHelpdesk.Api.Hubs.NotificationHub>("/hubs/notifications");

app.MapGet("/api/health", async (ApplicationDbContext db) =>
{
    var databaseHealthy = await db.Database.CanConnectAsync();

    double? diskUsedPercent = null;
    var diskHealthy = true;
    try
    {
        var drive = new DriveInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()) ?? "/");
        if (drive.IsReady)
        {
            diskUsedPercent = Math.Round(100.0 * (1 - (double)drive.AvailableFreeSpace / drive.TotalSize), 1);
            diskHealthy = diskUsedPercent < 95; // matches the "disk usage > 85%" alert threshold with headroom before failing health
        }
    }
    catch
    {
        // disk info unavailable (e.g. unsupported platform/path) — don't fail health over it
    }

    var healthy = databaseHealthy && diskHealthy;
    var payload = new
    {
        status = healthy ? "Healthy" : "Unhealthy",
        database = databaseHealthy,
        diskUsedPercent,
        timestamp = DateTime.UtcNow
    };
    return healthy ? Results.Ok(payload) : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
