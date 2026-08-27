namespace ERPSystem.Modules.Identity.Application.Services;

/// <summary>
/// Sprint 63 (DEC-213) — Permission resolution service.
/// <para>
/// Resolves a user's effective permission set by joining
/// <c>users → user_roles → role_permissions → permissions</c>. Results are cached
/// in <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> for 60 seconds
/// to absorb the wave of <c>[RequirePermission]</c> checks that a single request
/// typically triggers.
/// </para>
/// <para>
/// <b>L19 / DEC-095</b>: <c>userId</c> MUST be passed in by the caller (resolved from
/// JWT claims upstream). This service MUST NEVER take a user id from a request DTO.
/// </para>
/// <para>
/// <b>Thread safety</b>: <see cref="InvalidateCacheAsync"/> must be called by the admin
/// controller whenever a user's role membership or a role's permission set changes —
/// otherwise the change won't take effect for up to 60 seconds.
/// </para>
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Returns the set of permission <c>code</c>s granted to the user (union across all roles).
    /// Cached for 60s in <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>.
    /// </summary>
    Task<HashSet<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Convenience check: <c>GetPermissionsForUserAsync(userId).Contains(code)</c>.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken ct);

    /// <summary>
    /// Returns the set of module names the user can see (union across all roles,
    /// filtered by <c>is_visible = TRUE</c>). Cached for 60s.
    /// </summary>
    Task<HashSet<string>> GetVisibleModulesForUserAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Invalidates the cached entry for a user. Call when role membership or
    /// role-permission mappings change.
    /// </summary>
    Task InvalidateCacheAsync(Guid userId, CancellationToken ct);
}
