// Sprint 63 (DEC-213) — PermissionService unit tests.
//
// All tests use Moq for the 3 repositories and IMemoryCache. No DB hits — pure
// in-memory unit tests that exercise the service contract: caching, role union,
// invalidation, validation.
//
// Mirrors the test style of Sprint60/Sprint61 tests (FluentAssertions + Moq + xUnit).
// Categorized as "Sprint63" so they can be filtered with `--filter "FullyQualifiedName~Sprint63"`.

using ERPSystem.Modules.Identity.Application.Services;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Modules.Identity.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Identity;

[Trait("Category", "Sprint63")]
public class Sprint63PermissionServiceTests
{
    private static readonly Guid AdminUserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ReadOnlyUserId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HrUserId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid UnknownUserId = new("99999999-9999-9999-9999-999999999999");

    /// <summary>
    /// Build a PermissionService backed by mocks. Each test gets a fresh
    /// <see cref="MemoryCache"/> so cache state is not leaked across tests.
    /// </summary>
    private static (PermissionService svc,
                    Mock<IPermissionRepository> perms,
                    Mock<IRolePermissionRepository> rolePerms,
                    Mock<IModuleVisibilityRepository> modVis,
                    IMemoryCache cache) Build()
    {
        var perms = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var rolePerms = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var modVis = new Mock<IModuleVisibilityRepository>(MockBehavior.Strict);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = new PermissionService(perms.Object, rolePerms.Object, modVis.Object, cache, NullLogger<PermissionService>.Instance);
        return (svc, perms, rolePerms, modVis, cache);
    }

    private static Permission P(string code, string module = "Test") => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Resource = module.ToLowerInvariant(),
        Action = "view",
        Name = code,
        NameAr = null,
        Module = module,
        CreatedAt = DateTime.UtcNow,
    };

    private static List<Permission> AdminPermissions() => new()
    {
        P("projects.view", "Projects"),
        P("projects.create", "Projects"),
        P("projects.update", "Projects"),
        P("projects.delete", "Projects"),
        P("finance.accounts.view", "Finance"),
        P("hr.employees.view", "HR"),
        P("dashboard.view", "Dashboard"),
    };

    private static List<Permission> ReadOnlyPermissions() => new()
    {
        P("projects.view", "Projects"),
        P("finance.accounts.view", "Finance"),
        P("hr.employees.view", "HR"),
        P("dashboard.view", "Dashboard"),
    };

    private static List<ModuleVisibility> HrVisibility() => new()
    {
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "HR", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Companies", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Dashboard", IsVisible = true, CreatedAt = DateTime.UtcNow },
    };

    private static List<ModuleVisibility> AdminVisibility() => new()
    {
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Projects", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Finance", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "HR", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Payroll", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Inventory", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Procurement", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "AR", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Companies", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Identity", IsVisible = true, CreatedAt = DateTime.UtcNow },
        new ModuleVisibility { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), Module = "Dashboard", IsVisible = true, CreatedAt = DateTime.UtcNow },
    };

    // ============ GetPermissionsForUserAsync ============

    [Fact]
    public async Task GetPermissionsForUserAsync_Admin_ReturnsAllGranted()
    {
        var (svc, _, rolePerms, _, _) = Build();
        rolePerms.Setup(r => r.ListByUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Permission>)AdminPermissions());

        var perms = await svc.GetPermissionsForUserAsync(AdminUserId, CancellationToken.None);

        perms.Should().HaveCount(7);
        perms.Should().Contain("projects.create");
        perms.Should().Contain("projects.delete");
        perms.Should().Contain("dashboard.view");
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_ReadOnly_ReturnsViewOnlyAcrossModules()
    {
        var (svc, _, rolePerms, _, _) = Build();
        rolePerms.Setup(r => r.ListByUserAsync(ReadOnlyUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Permission>)ReadOnlyPermissions());

        var perms = await svc.GetPermissionsForUserAsync(ReadOnlyUserId, CancellationToken.None);

        perms.Should().HaveCount(4);
        perms.Should().OnlyContain(c => c.EndsWith(".view"));
        perms.Should().NotContain("projects.create");
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_UnknownUser_ReturnsEmptySet()
    {
        var (svc, _, rolePerms, _, _) = Build();
        rolePerms.Setup(r => r.ListByUserAsync(UnknownUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Permission>)new List<Permission>());

        var perms = await svc.GetPermissionsForUserAsync(UnknownUserId, CancellationToken.None);

        perms.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_CachesResults_OneDbHit()
    {
        var (svc, _, rolePerms, _, _) = Build();
        rolePerms.Setup(r => r.ListByUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Permission>)AdminPermissions());

        // 3 consecutive calls should hit the DB only once.
        await svc.GetPermissionsForUserAsync(AdminUserId, CancellationToken.None);
        await svc.GetPermissionsForUserAsync(AdminUserId, CancellationToken.None);
        await svc.GetPermissionsForUserAsync(AdminUserId, CancellationToken.None);

        rolePerms.Verify(r => r.ListByUserAsync(AdminUserId, It.IsAny<CancellationToken>()),
            Times.Once, "the 60s cache should absorb the second and third calls");
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_InvalidatesCache_OnInvalidateCacheAsync()
    {
        var (svc, _, rolePerms, _, _) = Build();
        rolePerms.Setup(r => r.ListByUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Permission>)AdminPermissions());

        // Prime the cache.
        await svc.GetPermissionsForUserAsync(AdminUserId, CancellationToken.None);
        rolePerms.Verify(r => r.ListByUserAsync(AdminUserId, It.IsAny<CancellationToken>()), Times.Once);

        // Invalidate.
        await svc.InvalidateCacheAsync(AdminUserId, CancellationToken.None);

        // Next call must hit the DB again.
        await svc.GetPermissionsForUserAsync(AdminUserId, CancellationToken.None);
        rolePerms.Verify(r => r.ListByUserAsync(AdminUserId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetPermissionsForUserAsync_ThrowsOnEmptyUserId()
    {
        var (svc, _, _, _, _) = Build();

        var act = async () => await svc.GetPermissionsForUserAsync(Guid.Empty, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*non-empty GUID*");
    }

    // ============ HasPermissionAsync ============

    [Fact]
    public async Task HasPermissionAsync_Admin_TrueForCreate()
    {
        var (svc, _, rolePerms, _, _) = Build();
        rolePerms.Setup(r => r.ListByUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Permission>)AdminPermissions());

        (await svc.HasPermissionAsync(AdminUserId, "projects.create", CancellationToken.None))
            .Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_ReadOnly_FalseForCreate()
    {
        var (svc, _, rolePerms, _, _) = Build();
        rolePerms.Setup(r => r.ListByUserAsync(ReadOnlyUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Permission>)ReadOnlyPermissions());

        (await svc.HasPermissionAsync(ReadOnlyUserId, "projects.create", CancellationToken.None))
            .Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_ReadOnly_TrueForView()
    {
        var (svc, _, rolePerms, _, _) = Build();
        rolePerms.Setup(r => r.ListByUserAsync(ReadOnlyUserId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Permission>)ReadOnlyPermissions());

        (await svc.HasPermissionAsync(ReadOnlyUserId, "projects.view", CancellationToken.None))
            .Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_ThrowsOnEmptyCode()
    {
        var (svc, _, _, _, _) = Build();
        var act = async () => await svc.HasPermissionAsync(AdminUserId, "", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ============ GetVisibleModulesForUserAsync ============

    [Fact]
    public async Task GetVisibleModulesForUserAsync_Admin_ReturnsAll10()
    {
        var (svc, _, _, modVis, _) = Build();
        modVis.Setup(m => m.ListByUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
              .ReturnsAsync((IReadOnlyList<ModuleVisibility>)AdminVisibility());

        var modules = await svc.GetVisibleModulesForUserAsync(AdminUserId, CancellationToken.None);

        modules.Should().HaveCount(10);
        modules.Should().Contain("Projects", "Finance", "HR", "Payroll", "Inventory",
            "Procurement", "AR", "Companies", "Identity", "Dashboard");
    }

    [Fact]
    public async Task GetVisibleModulesForUserAsync_HR_Returns3Modules()
    {
        var (svc, _, _, modVis, _) = Build();
        modVis.Setup(m => m.ListByUserAsync(HrUserId, It.IsAny<CancellationToken>()))
              .ReturnsAsync((IReadOnlyList<ModuleVisibility>)HrVisibility());

        var modules = await svc.GetVisibleModulesForUserAsync(HrUserId, CancellationToken.None);

        modules.Should().HaveCount(3);
        modules.Should().BeEquivalentTo(new[] { "HR", "Companies", "Dashboard" });
    }

    [Fact]
    public async Task GetVisibleModulesForUserAsync_UnknownUser_ReturnsEmpty()
    {
        var (svc, _, _, modVis, _) = Build();
        modVis.Setup(m => m.ListByUserAsync(UnknownUserId, It.IsAny<CancellationToken>()))
              .ReturnsAsync((IReadOnlyList<ModuleVisibility>)new List<ModuleVisibility>());

        var modules = await svc.GetVisibleModulesForUserAsync(UnknownUserId, CancellationToken.None);

        modules.Should().BeEmpty();
    }
}
