// Sprint 63 (DEC-216) — AdminPermissionsController.
//
// Admin-only API for managing the role ↔ permission matrix at runtime.
// Endpoints:
//   GET    /api/admin/permissions                                — list all permissions in the catalog
//   GET    /api/admin/roles/{roleId}/permissions                 — list permissions granted to a role
//   POST   /api/admin/roles/{roleId}/permissions                 — grant a permission to a role
//   DELETE /api/admin/roles/{roleId}/permissions/{permissionId}  — revoke a permission from a role
//   POST   /api/admin/roles/{roleId}/invalidate-cache            — invalidate cached permissions for every user in the role
//
// L19 / DEC-095: UserId is read from JWT claims inside the invalidate-cache
// endpoint (for logging / audit context). It is NEVER read from a request DTO.

using System.Security.Claims;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Identity.Application.Services;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
[RequirePermission("admin.permissions")]
public class AdminPermissionsController : ControllerBase
{
    private readonly IPermissionRepository _permissions;
    private readonly IRolePermissionRepository _rolePermissions;
    private readonly IRoleRepository _roles;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AdminPermissionsController> _logger;

    public AdminPermissionsController(
        IPermissionRepository permissions,
        IRolePermissionRepository rolePermissions,
        IRoleRepository roles,
        IPermissionService permissionService,
        ILogger<AdminPermissionsController> logger)
    {
        _permissions = permissions;
        _rolePermissions = rolePermissions;
        _roles = roles;
        _permissionService = permissionService;
        _logger = logger;
    }

    // ============ GET /api/admin/permissions ============
    // List the entire permission catalog. The catalog is global (not per-company).

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rows = await _permissions.ListAsync(ct);
        return Ok(rows.Select(PermissionResponse.From).ToList());
    }

    // ============ GET /api/admin/roles/{roleId}/permissions ============

    [HttpGet("roles/{roleId:guid}/permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions(Guid roleId, CancellationToken ct)
    {
        var rows = await _rolePermissions.ListByRoleAsync(roleId, ct);
        return Ok(rows.Select(PermissionResponse.From).ToList());
    }

    // ============ POST /api/admin/roles/{roleId}/permissions ============
    // Grant a permission to a role. Idempotent at the DB level (ON CONFLICT DO NOTHING).

    public sealed class AssignPermissionRequest
    {
        public Guid PermissionId { get; set; }
    }

    [HttpPost("roles/{roleId:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Assign(Guid roleId, [FromBody] AssignPermissionRequest req, CancellationToken ct)
    {
        if (req == null || req.PermissionId == Guid.Empty)
            return BadRequest(new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "permissionId is required." });

        // Existence check — return 404 if the permission id is unknown so the
        // admin UI can show a useful error instead of silently inserting a
        // dangling FK.
        var perm = await _permissions.GetByIdAsync(req.PermissionId, ct);
        if (perm == null)
            return NotFound(new ProblemDetails { Title = "Permission Not Found", Status = 404, Detail = $"Permission {req.PermissionId} does not exist." });

        await _rolePermissions.InsertAsync(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = req.PermissionId,
            CreatedAt = DateTime.UtcNow,
        }, ct);

        // Invalidate cache for every user in this role. The grant won't take
        // effect for up to 60s otherwise.
        await InvalidateCacheForRoleAsync(roleId, "grant", ct);

        return StatusCode(StatusCodes.Status201Created);
    }

    // ============ DELETE /api/admin/roles/{roleId}/permissions/{permissionId} ============

    [HttpDelete("roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        await _rolePermissions.DeleteAsync(roleId, permissionId, ct);

        await InvalidateCacheForRoleAsync(roleId, "revoke", ct);

        return NoContent();
    }

    // ============ POST /api/admin/roles/{roleId}/invalidate-cache ============
    // Force-refresh the IPermissionService cache for every user that holds
    // this role. Useful when an admin wants changes (e.g. an emergency grant
    // or revoke) to take effect immediately rather than waiting for the 60s
    // cache TTL to expire.

    [HttpPost("roles/{roleId:guid}/invalidate-cache")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InvalidateCache(Guid roleId, CancellationToken ct)
    {
        // L19: the caller id is taken from JWT for audit logging only.
        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        var invalidated = await InvalidateCacheForRoleAsync(roleId, "manual-invalidate", ct);

        _logger.LogInformation("AdminPermissionsController: cache invalidated for role {RoleId} (caller={CallerId}, users={Count})",
            roleId, callerId ?? "<unknown>", invalidated);

        return Ok(new { ok = true, roleId, invalidatedUsers = invalidated });
    }

    // -------- helpers --------

    /// <summary>
    /// Look up every user id that currently holds <paramref name="roleId"/>
    /// (via the <c>user_roles</c> join table) and call
    /// <see cref="IPermissionService.InvalidateCacheAsync"/> for each one.
    /// Returns the number of users whose cache was invalidated.
    /// </summary>
    private async Task<int> InvalidateCacheForRoleAsync(Guid roleId, string reason, CancellationToken ct)
    {
        var userIds = await _roles.GetUserIdsInRoleAsync(roleId, ct);

        foreach (var uid in userIds)
        {
            await _permissionService.InvalidateCacheAsync(uid, ct);
        }
        return userIds.Count;
    }
}

/// <summary>Public DTO for the admin permissions list endpoints.</summary>
public sealed class PermissionResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Module { get; set; } = string.Empty;

    public static PermissionResponse From(Permission p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Resource = p.Resource,
        Action = p.Action,
        Name = p.Name,
        NameAr = p.NameAr,
        Module = p.Module,
    };
}
