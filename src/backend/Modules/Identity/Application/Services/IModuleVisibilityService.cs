namespace ERPSystem.Modules.Identity.Application.Services;

/// <summary>
/// Sprint 63 (DEC-217, Wave 3) — FE-facing module-visibility service.
/// <para>
/// Thin wrapper over <see cref="IPermissionService.GetVisibleModulesForUserAsync"/>.
/// Exists so the controller layer can depend on a focused interface (only the
/// module-visibility method) instead of dragging in the entire
/// <see cref="IPermissionService"/> contract. Keeps the public surface narrow.
/// </para>
/// <para>
/// Returned as a <see cref="IReadOnlyList{T}"/> (not a <see cref="HashSet{T}"/>) so
/// the JSON payload is a JSON array on the wire and is naturally ordered.
/// </para>
/// </summary>
public interface IModuleVisibilityService
{
    Task<IReadOnlyList<string>> GetVisibleModulesForUserAsync(Guid userId, CancellationToken ct);
}
