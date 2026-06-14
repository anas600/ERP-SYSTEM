using ERPSystem.Modules.Identity.Application.Auth;

namespace ERPSystem.Modules.Identity.Application.Auth;

/// <summary>
/// عقد خدمة المصادقة
/// </summary>
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, string? ip, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest request, string? ip, CancellationToken ct);
    Task<AuthResult> RefreshAsync(RefreshTokenRequest request, string? ip, CancellationToken ct);
    Task RevokeAsync(Guid userId, string refreshToken, string? ip, CancellationToken ct);
}

/// <summary>نتيجة موحّدة لعمليات المصادقة</summary>
public sealed class AuthResult
{
    public bool Succeeded { get; init; }
    public AuthResponse? Response { get; init; }
    public string? Error { get; init; }
    public AuthErrorCode? ErrorCode { get; init; }

    public static AuthResult Ok(AuthResponse response) => new() { Succeeded = true, Response = response };
    public static AuthResult Fail(string error, AuthErrorCode code) => new() { Succeeded = false, Error = error, ErrorCode = code };
}

public enum AuthErrorCode
{
    InvalidCredentials,
    UserAlreadyExists,
    TenantNotFound,
    TenantInactive,
    UserInactive,
    InvalidRefreshToken,
    RefreshTokenExpired,
    RefreshTokenRevoked,
    ValidationError,
    InternalError
}
