using System.Security.Claims;
using ERPSystem.Modules.Identity.Entities;

namespace ERPSystem.Modules.Identity.Application.Auth;

/// <summary>خدمة توليد والتحقق من JWT و Refresh Tokens</summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Phase 6.1c: Generate Access Token with <c>default_company_id</c> +
    /// <c>company_ids[]</c> claims (multi-company model). Replaces the old
    /// <c>tenant_id</c> claim.
    /// </summary>
    (string token, DateTime expiresAt) GenerateAccessToken(User user, IEnumerable<string> roles, Guid defaultCompanyId, IReadOnlyList<Guid> companyIds);

    /// <summary>توليد Refresh Token عشوائي آمن (256-bit)</summary>
    (string token, string tokenHash, DateTime expiresAt) GenerateRefreshToken();

    /// <summary>هاش للـ refresh token (SHA-256 base64)</summary>
    string HashRefreshToken(string token);

    /// <summary>قراءة الـ principal من access token منتهي الصلاحية (للـ refresh flow)</summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
