// Sprint 63 (DEC-215) — [RequirePermission] action filter attribute.
//
// Applied to controller classes and/or action methods to enforce RBAC checks
// per the permission catalog seeded by RbacBootstrap. The attribute:
//   1. Honors a global "Authorization:Enabled" appsettings flag (test mode =
//      disable, prod = enable). When disabled, the filter is a no-op so
//      existing 50+ tests don't break.
//   2. Reads the user id from JWT (User.FindFirst(ClaimTypes.NameIdentifier))
//      — NEVER from a request DTO (L19 / DEC-095).
//   3. Bypasses the check for users in the "Admin" role (per RBAC design:
//      admins have every permission).
//   4. Otherwise calls IPermissionService.HasPermissionAsync, which resolves
//      the user's effective permission set (cached for 60s) and returns 403
//      if the user lacks the required permission.
//
// When the attribute is applied BOTH at the class level AND on a method,
// both checks run (per ASP.NET Core's IAsyncAuthorizationFilter semantics).
// The class-level value is a "default"; the method-level value is the
// specific action. This is the documented and expected behavior.

using System.Security.Claims;
using ERPSystem.Modules.Identity.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Host.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>The permission code (e.g. "projects.engineer_reports.create").</summary>
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("permission code is required", nameof(permission));
        Permission = permission;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1) Test-mode escape hatch. When Authorization:Enabled == false the
        //    filter is a no-op so the existing 50+ tests don't have to mock
        //    the JWT/claims/permission flow. Production sets this to true.
        var config = context.HttpContext.RequestServices.GetService<IConfiguration>();
        var enabled = config?.GetValue("Authorization:Enabled", true) ?? true;
        if (!enabled) return;

        // 2) L19 / DEC-095: read userId from JWT claims ONLY. We do NOT take
        //    it from a request DTO — that's a known spoofing vector.
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.HttpContext.User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            // No valid JWT — refuse the request before we ever hit the DB.
            context.Result = new UnauthorizedResult();
            return;
        }

        // 3) Admin role bypass. Admins have every permission by definition —
        //    no need to query the permission cache for them.
        if (context.HttpContext.User.IsInRole("Admin")) return;

        // 4) Permission check. IPermissionService reads from its 60s IMemoryCache
        //    and falls back to the DB on miss. Invalidation is the caller's job
        //    (see AdminPermissionsController).
        var permService = context.HttpContext.RequestServices.GetService<IPermissionService>();
        if (permService == null)
        {
            // No permission service registered → refuse. This protects against
            // accidental deployments that forgot to wire up RBAC.
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "RBAC Not Configured",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "IPermissionService is not registered. RBAC enforcement cannot run.",
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };
            return;
        }

        var hasPermission = await permService.HasPermissionAsync(userId, Permission, context.HttpContext.RequestAborted);
        if (!hasPermission)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Permission Denied",
                Status = StatusCodes.Status403Forbidden,
                Detail = $"User lacks required permission: {Permission}",
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
