namespace AIHelpdesk.Contracts.Auth;

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string AccessToken, string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(
    Guid Id,
    string Email,
    string FullName,
    string? NIK,
    IList<string> Roles,
    IList<string> Permissions);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateProfileRequest(string FullName, string? NIK);
