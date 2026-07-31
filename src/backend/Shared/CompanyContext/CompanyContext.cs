using Microsoft.AspNetCore.Http;

namespace ERPSystem.Shared.CompanyContext;

/// <summary>
/// سياق الشركة (Company) النشطة داخل الـ request
/// يُملأ من CompanyContextMiddleware بناءً على X-Company-Id header + JWT user id
///
/// Sprint 10 Phase 3: now scoped to the request via DI + IHttpContextAccessor.
/// Replaces the AsyncLocal-based implementation (fragile: leaked across Task.WhenAll
/// and BackgroundService scopes). Storage is HttpContext.Items, so it is naturally
/// request-scoped and disposed when the request ends.
///
/// Company context abstraction (Phase 6.1b: Multi-Company model, NOT Multi-Tenant).
/// في الـ v1 كل المستخدمين ينتمون لنفس الـ Holding افتراضياً؛
/// الـ header يسمح للـ Admin بتبديل الشركة النشطة في الواجهة.
/// </summary>
public sealed class CompanyContext : ICompanyContext
{
    // Keys for HttpContext.Items — centralized to avoid string-typo bugs.
    internal const string CompanyIdKey = "ERPSystem.CompanyContext.CompanyId";
    internal const string UserIdKey = "ERPSystem.CompanyContext.UserId";
    internal const string CompanyIdsKey = "ERPSystem.CompanyContext.CompanyIds";

    private readonly IHttpContextAccessor _http;

    public CompanyContext(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid? CompanyId => _http.HttpContext?.Items[CompanyIdKey] as Guid?;

    public Guid? UserId => _http.HttpContext?.Items[UserIdKey] as Guid?;

    public IReadOnlyList<Guid> CompanyIds =>
        _http.HttpContext?.Items[CompanyIdsKey] as IReadOnlyList<Guid> ?? Array.Empty<Guid>();

    public bool IsResolved => CompanyId.HasValue && UserId.HasValue;

    /// <summary>
    /// Set the active company + user. Called by CompanyContextMiddleware
    /// after resolving from X-Company-Id header + JWT claims.
    /// </summary>
    public void Set(Guid companyId, Guid userId, IReadOnlyList<Guid> companyIds)
    {
        // No HttpContext = background work. We can't store there; log a warning
        // and silently ignore. The middleware always runs in a request context.
        var http = _http.HttpContext;
        if (http == null)
        {
            return;
        }
        http.Items[CompanyIdKey] = companyId;
        http.Items[UserIdKey] = userId;
        http.Items[CompanyIdsKey] = companyIds;
    }

    /// <summary>
    /// Clear the context. Called by CompanyContextMiddleware in the finally block
    /// (defensive — HttpContext.Items is per-request, so this is mostly cosmetic).
    /// </summary>
    public void Clear()
    {
        var http = _http.HttpContext;
        if (http == null)
        {
            return;
        }
        http.Items.Remove(CompanyIdKey);
        http.Items.Remove(UserIdKey);
        http.Items.Remove(CompanyIdsKey);
    }
}
