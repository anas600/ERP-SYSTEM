// Sprint 63 (DEC-218) — MyPermissionsController.
//
// Thin wrapper over IPermissionService.GetPermissionsForUserAsync.
// Drives the FE PermissionGate component — the FE only renders action buttons
// for permissions the current user holds.
//
//   GET /api/me/permissions
//   → 200 { "permissions": ["projects.view", "projects.create", ...] }
//
// L19 / DEC-095: userId is read from JWT claims inside the controller. It is
// NEVER read from a request DTO / query string / route value. The FE does not
// need to send a user id — the JWT carries it.

using System.Security.Claims;
using ERPSystem.Modules.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MyPermissionsController : ControllerBase
{
    private readonly IPermissionService _permService;
    private readonly ILogger<MyPermissionsController> _logger;

    public MyPermissionsController(
        IPermissionService permService,
        ILogger<MyPermissionsController> logger)
    {
        _permService = permService;
        _logger = logger;
    }

    /// <summary>
    /// L19 / DEC-095: UserId is resolved from JWT claims, NEVER from a request DTO.
    /// Falls back to the <c>sub</c> claim for OIDC-style tokens.
    /// </summary>
    private Guid UserId
    {
        get
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(raw) || !Guid.TryParse(raw, out var id))
            {
                throw new UnauthorizedAccessException("JWT does not carry a valid user id.");
            }
            return id;
        }
    }

    // ============ GET /api/me/permissions ============
    // Returns the sorted, deduped list of permission codes the current user
    // holds (union across all roles). The FE uses this to decide whether to
    // render create / edit / delete buttons.

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(MyPermissionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var userId = UserId;
        var perms = await _permService.GetPermissionsForUserAsync(userId, ct);

        // Sort alphabetically for stable FE rendering (Set → deterministic order).
        var sorted = perms.OrderBy(p => p, StringComparer.Ordinal).ToList();

        _logger.LogDebug("MyPermissionsController: user {UserId} holds {Count} permissions",
            userId, sorted.Count);

        return Ok(new MyPermissionsResponse { Permissions = sorted });
    }
}

/// <summary>Public DTO for /api/me/permissions.</summary>
public sealed class MyPermissionsResponse
{
    /// <summary>Sorted, deduped list of permission codes the user holds (e.g. "projects.view", "finance.accounts.create").</summary>
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
}
