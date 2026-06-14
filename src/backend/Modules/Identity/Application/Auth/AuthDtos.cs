namespace ERPSystem.Modules.Identity.Application.Auth;

/// <summary>طلب تسجيل مستخدم جديد داخل مستأجر</summary>
public sealed class RegisterRequest
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? TenantName { get; set; } // اختياري — إذا أول مستخدم في tenant جديد
}

/// <summary>طلب تسجيل الدخول</summary>
public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid? TenantId { get; set; } // اختياري — إذا فارغ يبحث في كل الـ tenants
}

/// <summary>طلب تحديث الـ Access Token</summary>
public sealed class RefreshTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>استجابة موحّدة لعمليتي Login و Refresh</summary>
public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;
}

public sealed class UserInfo
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
