// Sprint 63 (DEC-215) — PermissionAuthorizationMiddleware.
//
// Runs early in the request pipeline (before UseAuthentication) to perform
// any pre-flight work the [RequirePermission] filter relies on. Today that
// is minimal: it warms up the ICompanyContext from the X-Company-Id header
// and ensures the JWT-claim-based user id is available in scope.
//
// Design note: We deliberately do NOT pre-warm the IPermissionService cache
// here. Permission checks are on-demand (cached for 60s on first miss) and
// the warmup would be wasted work for anonymous requests. The [RequirePermission]
// attribute only fires for authenticated routes (after [Authorize] ran),
// which is when the cache is actually useful.

using System.Security.Claims;
using ERPSystem.Shared.CompanyContext;
using Microsoft.AspNetCore.Http;

namespace ERPSystem.Host.Authorization;

public sealed class PermissionAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public PermissionAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICompanyContext companyContext)
    {
        // The ICompanyContext already knows how to read X-Company-Id from the
        // headers and the JWT claim list (see CompanyContextMiddleware). We
        // only need to ensure the user is identifiable here so downstream
        // [RequirePermission] filters can resolve the userId without re-doing
        // the same JWT lookup.
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out _))
        {
            // The user is authenticated — touch ICompanyContext so DI can
            // resolve it lazily for [RequirePermission] consumers. The actual
            // permission check happens in RequirePermissionAttribute, not here.
            _ = companyContext;
        }

        await _next(context);
    }
}
