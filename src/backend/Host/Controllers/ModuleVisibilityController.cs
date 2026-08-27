// Sprint 63 (DEC-217) — ModuleVisibilityController.
//
// Thin wrapper over IPermissionService.GetVisibleModulesForUserAsync.
// Drives the FE SmartSidebar — the FE only renders the sidebar items whose
// module the current user can see.
//
//   GET /api/me/visible-modules
//   → 200 { "modules": ["Dashboard", "HR", "Payroll", "Companies"] }
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
public sealed class ModuleVisibilityController : ControllerBase
{
    private readonly IPermissionService _permService;
    private readonly ILogger<ModuleVisibilityController> _logger;

    public ModuleVisibilityController(
        IPermissionService permService,
        ILogger<ModuleVisibilityController> logger)
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

    // ============ GET /api/me/visible-modules ============
    // Returns the sorted list of module names the current user can see. The
    // set is the union across all roles the user holds, filtered by
    // is_visible = TRUE.

    [HttpGet("visible-modules")]
    [ProducesResponseType(typeof(VisibleModulesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVisibleModules(CancellationToken ct)
    {
        var userId = UserId;
        var modules = await _permService.GetVisibleModulesForUserAsync(userId, ct);

        // Sort alphabetically for stable FE rendering (Set → deterministic order).
        var sorted = modules.OrderBy(m => m, StringComparer.Ordinal).ToList();

        _logger.LogDebug("ModuleVisibilityController: user {UserId} can see {Count} modules: [{Modules}]",
            userId, sorted.Count, string.Join(", ", sorted));

        return Ok(new VisibleModulesResponse { Modules = sorted });
    }
}

/// <summary>Public DTO for /api/me/visible-modules.</summary>
public sealed class VisibleModulesResponse
{
    /// <summary>Sorted, deduped list of module names the user can see (e.g. "Projects", "Finance", "HR").</summary>
    public IReadOnlyList<string> Modules { get; set; } = Array.Empty<string>();
}
