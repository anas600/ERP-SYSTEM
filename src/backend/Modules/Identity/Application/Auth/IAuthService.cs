using System.Data;

namespace ERPSystem.Modules.Identity.Application.Auth;

/// <summary>
/// Phase 6.1c: Multi-Company model — legacy bootstrap interface is REMOVED.
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
    /// frontend's company switcher). Replaces the legacy multi-tenant approach.
    /// </summary>
    Task<GetUserCompaniesResponse> GetUserCompaniesAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Sprint 61 (L175, DEC-198): Bootstrap the first admin on a brand-new
    /// deployment. Returns 409 if any user already exists; otherwise creates
    /// the admin user, the Admin role, the user_role link, and the
    /// user_companies link to the Holding, all in one transaction.
    /// </summary>
    Task<AdminBootstrapResult> AdminBootstrapAsync(AdminBootstrapRequest req, CancellationToken ct);
}

public sealed class AuthResult { public bool Succeeded { get; init; } public AuthResponse? Response { get; init; } public string? Error { get; init; } public AuthErrorCode? ErrorCode { get; init; } public static AuthResult Ok(AuthResponse r) => new() { Succeeded = true, Response = r }; public static AuthResult Fail(string e, AuthErrorCode c) => new() { Succeeded = false, Error = e, ErrorCode = c }; }
public enum AuthErrorCode { InvalidCredentials, UserAlreadyExists, UserInactive, InvalidRefreshToken, RefreshTokenExpired, RefreshTokenRevoked, ValidationError, InternalError, NoCompaniesAssigned, AlreadyBootstrapped, HoldingNotFound, RoleNotFound }

/// <summary>
/// Sprint 61 (L175): Result of <see cref="IAuthService.AdminBootstrapAsync"/>.
/// <see cref="Conflict"/> is true when the deployment already has at least one
/// user and the bootstrap endpoint refuses to create another admin.
/// </summary>
public sealed class AdminBootstrapResult
{
    public bool Success { get; init; }
    public bool Conflict { get; init; }
    public string? Error { get; init; }
    public AdminBootstrapResponse? Response { get; init; }
    public AuthErrorCode? ErrorCode { get; init; }

    public static AdminBootstrapResult Ok(AdminBootstrapResponse r) =>
        new() { Success = true, Response = r };

    public static AdminBootstrapResult ConflictResult(string error) =>
        new() { Success = false, Conflict = true, Error = error, ErrorCode = AuthErrorCode.AlreadyBootstrapped };

    public static AdminBootstrapResult Fail(string error, AuthErrorCode code) =>
        new() { Success = false, Error = error, ErrorCode = code };
}
