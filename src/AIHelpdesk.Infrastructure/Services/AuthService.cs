using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AIHelpdesk.Application.Interfaces;
using AIHelpdesk.Contracts.Auth;
using AIHelpdesk.Domain.Entities;
using AIHelpdesk.Infrastructure.Data;
using AIHelpdesk.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AIHelpdesk.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid email or password");

        var result = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!result)
            throw new UnauthorizedAccessException("Invalid email or password");

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress)
    {
        var principal = _tokenService.ValidateToken(request.AccessToken);
        if (principal == null)
            throw new UnauthorizedAccessException("Invalid access token");

        var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId == null)
            throw new UnauthorizedAccessException("Invalid token claims");

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId.ToString() == userId);

        if (refreshToken == null || !refreshToken.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token");

        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive");

        return await GenerateAuthResponseAsync(user, ipAddress);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token != null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        return Task.CompletedTask;
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return;

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<UserInfo> GetProfileAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        return await BuildUserInfoAsync(user);
    }

    public async Task<UserInfo> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        user.FullName = request.FullName;
        user.NIK = request.NIK;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        return await BuildUserInfoAsync(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user, string? ipAddress)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user.Id);

        var accessToken = _tokenService.GenerateAccessToken(user, roles.ToList(), permissions.ToList());
        var refreshToken = _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        });

        await _context.SaveChangesAsync();

        var userInfo = new UserInfo(user.Id, user.Email ?? "", user.FullName, user.NIK, roles.ToList(), permissions.ToList());

        return new AuthResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15),
            userInfo);
    }

    private async Task<IList<string>> GetUserPermissionsAsync(Guid userId)
    {
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var permissions = await _context.Set<Permission>()
            .Where(p => p.Roles.Any(r => userRoles.Contains(r.Id)))
            .Select(p => p.Name)
            .Distinct()
            .ToListAsync();

        return permissions;
    }

    private async Task<UserInfo> BuildUserInfoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user.Id);

        return new UserInfo(user.Id, user.Email ?? "", user.FullName, user.NIK, roles.ToList(), permissions.ToList());
    }
}
