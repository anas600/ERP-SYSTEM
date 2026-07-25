using System.Data;

namespace ERPSystem.Modules.Identity.Application.Auth;

/// <summary>
/// Phase 6.1c: Multi-Company model — <c>ITenantBootstrap</c> is REMOVED.
/// The Holding Company is auto-seeded at startup by
/// <c>DefaultHoldingBootstrapHostedService</c>. The new register flow links the
/// new user to the Holding via the <c>user_companies</c> join table directly.
/// </summary>

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest req, string? ip, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest req, string? ip, CancellationToken ct);
    Task<AuthResult> RefreshAsync(RefreshTokenRequest req, string? ip, CancellationToken ct);
    Task RevokeAsync(Guid userId, string refreshToken, string? ip, CancellationToken ct);

    /// <summary>
    /// Returns the list of companies the user has access to (used by the
    /// frontend's company switcher). Replaces the legacy
    /// <c>ITenantContext</c>/<c>tenant_id</c> approach.
    /// </summary>
    Task<GetUserCompaniesResponse> GetUserCompaniesAsync(Guid userId, CancellationToken ct);
}

public sealed class AuthResult { public bool Succeeded { get; init; } public AuthResponse? Response { get; init; } public string? Error { get; init; } public AuthErrorCode? ErrorCode { get; init; } public static AuthResult Ok(AuthResponse r) => new() { Succeeded = true, Response = r }; public static AuthResult Fail(string e, AuthErrorCode c) => new() { Succeeded = false, Error = e, ErrorCode = c }; }
public enum AuthErrorCode { InvalidCredentials, UserAlreadyExists, UserInactive, InvalidRefreshToken, RefreshTokenExpired, RefreshTokenRevoked, ValidationError, InternalError, NoCompaniesAssigned }
