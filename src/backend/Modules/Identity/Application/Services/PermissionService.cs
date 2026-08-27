using ERPSystem.Modules.Identity.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Identity.Application.Services;

/// <summary>
/// Sprint 63 (DEC-213) — Default <see cref="IPermissionService"/> impl.
/// <para>
/// Uses <see cref="IMemoryCache"/> with a 60-second TTL. Cache key is
/// <c>perm:user:{userId}</c> for permissions and <c>mod:user:{userId}</c> for
/// visible modules. Invalidated together via <see cref="InvalidateCacheAsync"/>.
/// </para>
/// <para>
/// <b>Why 60s</b>: a single user request may trigger many <c>[RequirePermission]</c>
/// checks (one per controller action in the call chain). A 60s TTL absorbs the burst
/// without stale data persisting too long. Admin role changes force-invalidate
/// (see <see cref="InvalidateCacheAsync"/>).
/// </para>
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly IPermissionRepository _permissions;
    private readonly IRolePermissionRepository _rolePermissions;
    private readonly IModuleVisibilityRepository _moduleVisibility;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IPermissionRepository permissions,
        IRolePermissionRepository rolePermissions,
        IModuleVisibilityRepository moduleVisibility,
        IMemoryCache cache,
        ILogger<PermissionService> logger)
    {
        _permissions = permissions;
        _rolePermissions = rolePermissions;
        _moduleVisibility = moduleVisibility;
        _cache = cache;
        _logger = logger;
    }

    public async Task<HashSet<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("userId must be a non-empty GUID", nameof(userId));

        var key = PermCacheKey(userId);
        if (_cache.TryGetValue<HashSet<string>>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var perms = await _rolePermissions.ListByUserAsync(userId, ct);
        var set = new HashSet<string>(perms.Select(p => p.Code), StringComparer.OrdinalIgnoreCase);

        // Cache (60s). AbsoluteExpiration is the simplest TTL; sliding would let hot
        // users pin the entry forever, which is fine for permissions but confusing
        // to reason about.
        _cache.Set(key, set, CacheTtl);

        _logger.LogDebug(
            "Resolved {Count} permission(s) for user {UserId} (cached 60s)",
            set.Count, userId);

        return set;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            throw new ArgumentException("permissionCode is required", nameof(permissionCode));

        var perms = await GetPermissionsForUserAsync(userId, ct);
        return perms.Contains(permissionCode);
    }

    public async Task<HashSet<string>> GetVisibleModulesForUserAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("userId must be a non-empty GUID", nameof(userId));

        var key = ModulesCacheKey(userId);
        if (_cache.TryGetValue<HashSet<string>>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var rows = await _moduleVisibility.ListByUserAsync(userId, ct);
        var set = new HashSet<string>(rows.Select(r => r.Module), StringComparer.OrdinalIgnoreCase);

        _cache.Set(key, set, CacheTtl);

        _logger.LogDebug(
            "Resolved {Count} visible module(s) for user {UserId} (cached 60s)",
            set.Count, userId);

        return set;
    }

    public Task InvalidateCacheAsync(Guid userId, CancellationToken ct)
    {
        _cache.Remove(PermCacheKey(userId));
        _cache.Remove(ModulesCacheKey(userId));
        _logger.LogDebug("Invalidated permission cache for user {UserId}", userId);
        return Task.CompletedTask;
    }

    private static string PermCacheKey(Guid userId) => $"perm:user:{userId:N}";
    private static string ModulesCacheKey(Guid userId) => $"mod:user:{userId:N}";
}
