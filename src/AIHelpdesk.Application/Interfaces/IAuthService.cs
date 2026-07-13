using AIHelpdesk.Contracts.Auth;

namespace AIHelpdesk.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress);
    Task LogoutAsync(string refreshToken);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task<UserInfo> GetProfileAsync(Guid userId);
    Task<UserInfo> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
