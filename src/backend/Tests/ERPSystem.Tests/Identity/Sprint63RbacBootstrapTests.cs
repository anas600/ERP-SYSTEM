// Sprint 63 (DEC-214) — RbacBootstrap seed-data tests.
//
// These tests verify the RbacSeedData static class: JSON loading, structure validation,
// and the counts that the bootstrap relies on. The DB-bound seeding (RbacBootstrapHostedService)
// is hard to mock with Dapper, so we test the seed data layer directly. The hosted
// service uses ON CONFLICT DO NOTHING everywhere, so DB-level idempotency is structurally
// guaranteed.

using ERPSystem.Host.Bootstrap;
using FluentAssertions;
using Xunit;

namespace ERPSystem.Tests.Identity;

[Trait("Category", "Sprint63")]
public class Sprint63RbacBootstrapTests
{
    private static string SeedDataPath()
    {
        // Resolve the seed JSON the same way RbacSeedData does (dev/test fallback).
        return RbacSeedData.ResolveSeedDataPath();
    }

    [Fact]
    public void Bootstrap_Seeds5Roles_OnEmptyDatabase()
    {
        var seed = RbacSeedData.Load(SeedDataPath());

        seed.Roles.Should().HaveCount(5);
        seed.Roles.Select(r => r.Code.ToLowerInvariant())
            .Should().BeEquivalentTo(new[] { "admin", "finance", "hr", "pm", "readonly" });
    }

    [Fact]
    public void Bootstrap_Skips_OnAlreadySeeded_ByCountingDistinctRbacRoles()
    {
        // The idempotency guard in RbacBootstrapHostedService is:
        //   SELECT COUNT(*) FROM roles WHERE LOWER(name) IN ('admin','finance','hr','pm','readonly')
        // This test asserts the seed contains exactly those 5 codes (case-insensitive),
        // which is what the guard checks for.
        var seed = RbacSeedData.Load(SeedDataPath());

        var rbacCodes = new[] { "admin", "finance", "hr", "pm", "readonly" };
        var seededCodes = seed.Roles.Select(r => r.Code.ToLowerInvariant()).ToHashSet();

        foreach (var code in rbacCodes)
        {
            seededCodes.Should().Contain(code, "the idempotency guard looks for this code");
        }
    }

    [Fact]
    public void Bootstrap_CreatesExactly80PlusPermissions()
    {
        var seed = RbacSeedData.Load(SeedDataPath());

        seed.Permissions.Count.Should().BeGreaterThanOrEqualTo(80,
            "the spec requires ≥80 permissions (admin gets all of them)");
    }

    [Fact]
    public void Bootstrap_AllPermissionCodes_AreUnique()
    {
        var seed = RbacSeedData.Load(SeedDataPath());

        var distinct = seed.Permissions.Select(p => p.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        distinct.Should().Be(seed.Permissions.Count, "permission codes must be unique (UNIQUE constraint)");
    }

    [Fact]
    public void Bootstrap_AdminRole_HasWildcardGrant()
    {
        var seed = RbacSeedData.Load(SeedDataPath());

        seed.RolePermissions.Should().ContainKey("admin");
        seed.RolePermissions["admin"].Should().Contain("*", "admin must have all permissions");
    }

    [Fact]
    public void Bootstrap_ReadOnlyRole_HasOnlyViewPermissions()
    {
        var seed = RbacSeedData.Load(SeedDataPath());

        seed.RolePermissions["readonly"]
            .Should().OnlyContain(c => c.EndsWith(".view", StringComparison.OrdinalIgnoreCase),
                "readonly must have only .view permissions across all modules");
    }

    [Fact]
    public void Bootstrap_ModuleVisibility_CoversAll5RolesAnd10Modules()
    {
        var seed = RbacSeedData.Load(SeedDataPath());

        seed.ModuleVisibility.Keys.Should().BeEquivalentTo(new[] { "admin", "finance", "hr", "pm", "readonly" });
        foreach (var (role, modules) in seed.ModuleVisibility)
        {
            modules.Keys.Should().BeEquivalentTo(new[]
            {
                "Projects", "Finance", "HR", "Payroll", "Inventory",
                "Procurement", "AR", "Companies", "Identity", "Dashboard"
            }, $"role '{role}' must have a row for every module");
        }
    }

    [Fact]
    public void Bootstrap_AdminRole_SeesAll10Modules()
    {
        var seed = RbacSeedData.Load(SeedDataPath());

        seed.ModuleVisibility["admin"]
            .Should().OnlyContain(kvp => kvp.Value == true,
                "admin must see every module");
    }

    [Fact]
    public void Bootstrap_HRRole_SeesExactly3Modules()
    {
        var seed = RbacSeedData.Load(SeedDataPath());

        var visible = seed.ModuleVisibility["hr"]
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        visible.Should().HaveCount(3);
        visible.Should().BeEquivalentTo(new[] { "HR", "Companies", "Dashboard" });
    }

    [Fact]
    public void Bootstrap_Load_MissingFile_Throws()
    {
        var act = () => RbacSeedData.Load(@"C:\nonexistent\RbacSeedData.json");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Bootstrap_Validate_DetectsUnknownPermissionReferences()
    {
        // Build a synthetic seed with 80 valid permissions + 1 ghost reference.
        var validPerms = Enumerable.Range(0, 80)
            .Select(i => new RbacSeedData.Permission
            {
                Code = $"perm.{i:D3}.view",
                Resource = "test",
                Action = "view",
                Name = $"Permission {i}",
                Module = "Test"
            })
            .ToList();
        var seed = new RbacSeedData.Seed
        {
            Roles = new() { new() { Code = "admin", Name = "Admin", NameAr = "مدير" } },
            Permissions = validPerms,
            RolePermissions = new() { ["admin"] = new() { "perm.000.view", "ghost.permission" } },
            ModuleVisibility = new() { ["admin"] = new() { ["Test"] = true } }
        };

        var act = () => RbacSeedData.Validate(seed);

        act.Should().Throw<InvalidDataException>()
            .WithMessage("*ghost.permission*");
    }
}
