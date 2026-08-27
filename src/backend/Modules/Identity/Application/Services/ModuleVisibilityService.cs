namespace ERPSystem.Modules.Identity.Application.Services;

/// <summary>
/// Sprint 63 (DEC-217, Wave 3) — Default <see cref="IModuleVisibilityService"/> impl.
/// <para>
/// Delegates to <see cref="IPermissionService.GetVisibleModulesForUserAsync"/> and
/// materializes the result into a sorted <see cref="List{T}"/>. The sort keeps the
/// JSON payload stable across calls (so the FE can use it as a React key, cache key, etc).
/// </para>
/// </summary>
public sealed class ModuleVisibilityService : IModuleVisibilityService
{
    private readonly IPermissionService _permissionService;

    public ModuleVisibilityService(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<IReadOnlyList<string>> GetVisibleModulesForUserAsync(Guid userId, CancellationToken ct)
    {
        var set = await _permissionService.GetVisibleModulesForUserAsync(userId, ct);
        // Materialize the HashSet into a sorted List for stable JSON output.
        // List is JSON-friendly (becomes a JSON array). HashSet is not.
        return set.OrderBy(m => m, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
