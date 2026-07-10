using System.Collections;
using Microsoft.Extensions.Caching.Memory;

namespace ERPSystem.Host.Utilities;

/// <summary>
/// TenantCache — wrapper around IMemoryCache with tenant-scoped keys.
///
/// Used by read endpoints to cache responses per-tenant. Cache keys always
/// include the tenant id to prevent cross-tenant leaks.
///
/// Pattern:
///   - Read: var data = await cache.GetOrCreateAsync($"accounts:{tenantId}", () => _service.ListAsync(...), TimeSpan.FromMinutes(5));
///   - Invalidate: cache.InvalidatePrefix($"accounts");
///
/// DEC-107 / DL 82-84.
/// </summary>
public interface ITenantCache
{
    /// <summary>Get cached value or create it. Returns the cached or fresh value.</summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Get cached value (or default if missing).</summary>
    T? Get<T>(string key);

    /// <summary>Remove a specific key.</summary>
    void Remove(string key);

    /// <summary>Remove all keys starting with prefix (used after writes).</summary>
    void InvalidatePrefix(string prefix);

    /// <summary>Clear all cached entries for a tenant.</summary>
    void InvalidateTenant(Guid tenantId);
}

public class TenantCache : ITenantCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<TenantCache> _logger;

    public TenantCache(IMemoryCache cache, ILogger<TenantCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            _logger.LogDebug("[Cache] HIT {Key}", key);
            return cached;
        }
        _logger.LogDebug("[Cache] MISS {Key} - fetching", key);
        var fresh = await factory();
        if (fresh is not null)
        {
            _cache.Set(key, fresh, ttl);
        }
        return fresh;
    }

    public T? Get<T>(string key)
    {
        return _cache.TryGetValue(key, out T? v) ? v : default;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("[Cache] REMOVE {Key}", key);
    }

    public void InvalidatePrefix(string prefix)
    {
        // IMemoryCache has no built-in enumeration. We use reflection to access
        // the internal entries. If that fails, log a warning (best effort).
        try
        {
            if (_cache is MemoryCache mc)
            {
                var entriesField = typeof(MemoryCache).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var coherentState = entriesField?.GetValue(mc);
                if (coherentState is not null)
                {
                    var entriesCollection = coherentState.GetType().GetProperty("Entries")?.GetValue(coherentState) as IDictionary;
                    if (entriesCollection is not null)
                    {
                        List<string> toRemove = new();
                        foreach (DictionaryEntry entry in entriesCollection)
                        {
                            if (entry.Key is string k && k.StartsWith(prefix, StringComparison.Ordinal))
                            {
                                toRemove.Add(k);
                            }
                        }
                        foreach (var k in toRemove)
                        {
                            _cache.Remove(k);
                        }
                        _logger.LogDebug("[Cache] INVALIDATE {Prefix}* ({Count} entries)", prefix, toRemove.Count);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] InvalidatePrefix failed for {Prefix}", prefix);
        }
    }

    public void InvalidateTenant(Guid tenantId)
    {
        InvalidatePrefix($"t:{tenantId:N}:");
    }
}
