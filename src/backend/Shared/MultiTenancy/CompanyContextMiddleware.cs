using System.Security.Claims;

namespace ERPSystem.Shared.MultiTenancy;

/// <summary>
/// Middleware يلتقط CompanyId من X-Company-Id header + UserId من JWT.
/// إذا الـ header غائب، يستخدم أول company من JWT company_ids[] claim (default company).
///
/// لازم يأتي بعد UseAuthentication() و UseAuthorization() في الـ pipeline.
///
/// Phase 6: يحل محل TenantMiddleware. كلا الـ middlewares يتعايشوا في PR-6.1a
/// ثم TenantMiddleware يُحذف في PR-6.1b.
/// </summary>
public sealed class CompanyContextMiddleware
{
    private const string CompanyHeader = "X-Company-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CompanyContextMiddleware> _logger;

    public CompanyContextMiddleware(RequestDelegate next, ILogger<CompanyContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICompanyContext companyContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isPublic = IsPublicPath(path);

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? context.User.FindFirst("sub")?.Value;

            var companyIdsClaim = context.User.FindFirst("company_ids")?.Value
                                 ?? context.User.FindFirst("companyIds")?.Value;
            var companyIds = ParseCompanyIds(companyIdsClaim);

            // 1) X-Company-Id header يأخذ الأولوية
            var headerCompanyId = context.Request.Headers[CompanyHeader].ToString();
            Guid? selectedCompanyId = null;

            if (Guid.TryParse(headerCompanyId, out var headerGuid) &&
                companyIds.Contains(headerGuid))
            {
                selectedCompanyId = headerGuid;
            }
            else if (companyIds.Count > 0)
            {
                // 2) Fall back to default_company_id claim، ثم أول company
                var defaultClaim = context.User.FindFirst("default_company_id")?.Value
                                ?? context.User.FindFirst("defaultCompanyId")?.Value;
                if (Guid.TryParse(defaultClaim, out var defaultGuid) && companyIds.Contains(defaultGuid))
                {
                    selectedCompanyId = defaultGuid;
                }
                else
                {
                    selectedCompanyId = companyIds[0];
                }
            }

            if (Guid.TryParse(userClaim, out var userId) && selectedCompanyId.HasValue)
            {
                companyContext.Set(selectedCompanyId.Value, userId, companyIds);
                _logger.LogDebug("Company resolved: {CompanyId}, User: {UserId}", selectedCompanyId, userId);
            }
            else if (!isPublic)
            {
                _logger.LogWarning("Authenticated request without resolvable company on path {Path}", path);
            }
        }

        try
        {
            await _next(context);
        }
        finally
        {
            companyContext.Clear();
        }
    }

    private static IReadOnlyList<Guid> ParseCompanyIds(string? claim)
    {
        if (string.IsNullOrEmpty(claim)) return Array.Empty<Guid>();
        // JWT claim may be JSON array string e.g. "[\"guid1\",\"guid2\"]"
        // or simple comma-separated. Handle both.
        if (claim.StartsWith("["))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(claim);
                return parsed?
                    .Select(s => Guid.TryParse(s.Trim('"'), out var g) ? g : (Guid?)null)
                    .Where(g => g.HasValue)
                    .Select(g => g!.Value)
                    .ToList() ?? new List<Guid>();
            }
            catch
            {
                return Array.Empty<Guid>();
            }
        }
        return claim.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();
    }

    private static bool IsPublicPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;
        return path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase);
    }
}
