namespace ERPSystem.Modules.Identity.Application.Auth;

/// <summary>
/// Phase 6.1c: Multi-Company model. Register creates the first user under the
/// default Holding Company. The Holding is auto-seeded by
/// <c>DefaultHoldingBootstrapHostedService</c> at startup, so callers don't
/// need to know about it. The new user is auto-linked to the Holding via the
/// <c>user_companies</c> join table (with <c>is_default = true</c>).
/// </summary>
public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;
    /// <summary>The Holding Company this deployment is rooted at. (Always the same value within a single deployment.)</summary>
    public Guid HoldingCompanyId { get; set; }
}

public sealed class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    /// <summary>Default company the user lands on after login (set from <c>user_companies.is_default = true</c>).</summary>
    public Guid DefaultCompanyId { get; set; }
    /// <summary>All companies the user has access to. Drives the company switcher.</summary>
    public IReadOnlyList<UserCompanyInfo> Companies { get; set; } = Array.Empty<UserCompanyInfo>();
}

public sealed class UserCompanyInfo
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsHolding { get; set; }
}

public sealed class GetUserCompaniesResponse
{
    public Guid UserId { get; set; }
    public Guid DefaultCompanyId { get; set; }
    public IReadOnlyList<UserCompanyInfo> Companies { get; set; } = Array.Empty<UserCompanyInfo>();
}

// ============ Sprint 61 (L175, DEC-198) — first-admin bootstrap ============

/// <summary>
/// Request body for <c>POST /api/auth/admin-bootstrap</c>. Only callable on a
/// brand-new deployment (zero users in the system). Creates the first admin
/// user, the "Admin" role, the <c>user_role</c> link, and the
/// <c>user_companies</c> link to the Holding — all in a single transaction.
/// </summary>
public sealed class AdminBootstrapRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class AdminBootstrapResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public Guid CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
}
