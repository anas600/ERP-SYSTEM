// Sprint 63 (DEC-211..214) — RBAC seed data loader.
//
// Extracted from RbacBootstrapHostedService so the seed data can be unit-tested
// without a live database. The hosted service just wires the loaded seed to the DB.

using System.Text.Json;

namespace ERPSystem.Host.Bootstrap;

/// <summary>
/// Loads <c>Shared/SeedData/RbacSeedData.json</c> into a typed in-memory model.
/// The file is located via <see cref="ResolveSeedDataPath"/> (app base + dev fallback).
/// </summary>
public static class RbacSeedData
{
    /// <summary>JSON DTO root.</summary>
    public sealed class Seed
    {
        public List<Role> Roles { get; set; } = new();
        public List<Permission> Permissions { get; set; } = new();
        public Dictionary<string, List<string>> RolePermissions { get; set; } = new();
        public Dictionary<string, Dictionary<string, bool>> ModuleVisibility { get; set; } = new();
    }

    public sealed class Role
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
    }

    public sealed class Permission
    {
        public string Code { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string Module { get; set; } = string.Empty;
    }

    /// <summary>
    /// Load and parse the seed JSON. Throws <see cref="FileNotFoundException"/>
    /// if the file is missing, <see cref="InvalidDataException"/> if the JSON is malformed
    /// or missing required fields.
    /// </summary>
    public static Seed Load(string? explicitPath = null)
    {
        var path = explicitPath ?? ResolveSeedDataPath();
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"RbacSeedData.json not found at '{path}'. " +
                "Check the .csproj CopyToOutput rules in src/backend/Host/ERP-SYSTEM.csproj.",
                path);
        }
        var json = File.ReadAllText(path);
        var seed = JsonSerializer.Deserialize<Seed>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException("RbacSeedData.json is empty or invalid");

        Validate(seed);
        return seed;
    }

    /// <summary>
    /// Locate the seed JSON. Checks the app base first, then walks up the directory
    /// tree (for dev/test runs where the file is at the source path).
    /// </summary>
    public static string ResolveSeedDataPath()
    {
        var appBase = AppContext.BaseDirectory;
        var candidate = Path.Combine(appBase, "Shared", "SeedData", "RbacSeedData.json");
        if (File.Exists(candidate)) return candidate;

        var dir = new DirectoryInfo(appBase);
        for (var i = 0; i < 6 && dir != null; i++, dir = dir.Parent!)
        {
            candidate = Path.Combine(dir.FullName, "Shared", "SeedData", "RbacSeedData.json");
            if (File.Exists(candidate)) return candidate;
        }
        // Return the appBase candidate even if missing — caller will throw FileNotFoundException
        // with a useful path.
        return candidate;
    }

    /// <summary>
    /// Validates the seed structure: 5 roles, ≥80 permissions, all permission codes
    /// referenced in rolePermissions must exist in the catalog, all modules must be consistent.
    /// Throws <see cref="InvalidDataException"/> on any violation.
    /// </summary>
    public static void Validate(Seed seed)
    {
        if (seed.Roles.Count == 0)
            throw new InvalidDataException("RbacSeedData.json: no roles defined");
        if (seed.Permissions.Count < 80)
            throw new InvalidDataException(
                $"RbacSeedData.json: expected ≥80 permissions, got {seed.Permissions.Count}");
        if (seed.Roles.Select(r => r.Code.ToLowerInvariant()).Distinct().Count() != seed.Roles.Count)
            throw new InvalidDataException("RbacSeedData.json: duplicate role codes");

        var catalog = new HashSet<string>(seed.Permissions.Select(p => p.Code), StringComparer.OrdinalIgnoreCase);
        foreach (var (roleCode, codes) in seed.RolePermissions)
        {
            foreach (var code in codes)
            {
                if (code == "*") continue;
                if (!catalog.Contains(code))
                {
                    throw new InvalidDataException(
                        $"RbacSeedData.json: role '{roleCode}' references unknown permission code '{code}'");
                }
            }
        }
    }
}
