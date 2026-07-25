using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ERPSystem.Host.Controllers;
using Microsoft.IdentityModel.Tokens;

namespace ERPSystem.Tests.E2E.TestFixtures;

/// <summary>
/// Generates JWTs for E2E tests. Phase 6.1c: Multi-Company model.
/// Tokens carry <c>default_company_id</c> + <c>company_ids</c> instead of
/// the legacy <c>companyId</c> / <c>tid</c> claims.
/// </summary>
public static class TestJwtGenerator
{
    public static string Generate(
        string userId,
        string companyId,
        string? email = null,
        string fullName = "E2E Test User",
        string[]? roles = null,
        TimeSpan? expires = null,
        string[]? additionalCompanyIds = null)
    {
        roles ??= new[] { "Admin" };
        email ??= $"{userId}@test.local";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ErpWebApplicationFactory.TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId),
            new("default_company_id", companyId),
            new("fullName", fullName),
            new(ClaimTypes.Role, string.Join(",", roles))
        };

        // One company_ids claim per company. Always include the default.
        var allCompanyIds = new List<string> { companyId };
        if (additionalCompanyIds != null)
            allCompanyIds.AddRange(additionalCompanyIds);
        foreach (var cid in allCompanyIds.Distinct())
            claims.Add(new Claim("company_ids", cid));

        var token = new JwtSecurityToken(
            issuer: "E2E-TEST",
            audience: "E2E-TEST-Users",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(expires ?? TimeSpan.FromMinutes(30)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
