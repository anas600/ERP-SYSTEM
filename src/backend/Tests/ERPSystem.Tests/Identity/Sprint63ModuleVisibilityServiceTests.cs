// Sprint 63 (DEC-217) — ModuleVisibilityService unit tests.
//
// The service is a thin wrapper over IPermissionService.GetVisibleModulesForUserAsync.
// These tests verify the wrapper contract: materializes the HashSet into a sorted
// List, returns IReadOnlyList, handles empty input, and delegates to the inner service.

using ERPSystem.Modules.Identity.Application.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Identity;

[Trait("Category", "Sprint63")]
public class Sprint63ModuleVisibilityServiceTests
{
    private static readonly Guid AdminUserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HrUserId = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid UnknownUserId = new("99999999-9999-9999-9999-999999999999");

    private static (ModuleVisibilityService svc, Mock<IPermissionService> perms) Build()
    {
        var perms = new Mock<IPermissionService>(MockBehavior.Strict);
        return (new ModuleVisibilityService(perms.Object), perms);
    }

    [Fact]
    public async Task GetVisibleModulesForUserAsync_Delegates_ToPermissionService()
    {
        var (svc, perms) = Build();
        perms.Setup(p => p.GetVisibleModulesForUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new HashSet<string> { "Projects", "Finance" });

        await svc.GetVisibleModulesForUserAsync(AdminUserId, CancellationToken.None);

        perms.Verify(p => p.GetVisibleModulesForUserAsync(AdminUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVisibleModulesForUserAsync_ReturnsList_NotSet()
    {
        var (svc, perms) = Build();
        perms.Setup(p => p.GetVisibleModulesForUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new HashSet<string> { "Finance", "Projects", "HR" });

        var result = await svc.GetVisibleModulesForUserAsync(AdminUserId, CancellationToken.None);

        // Returned as a stable, JSON-friendly ordered list (not a HashSet).
        result.Should().BeAssignableTo<IReadOnlyList<string>>();
        result.Should().NotBeOfType<HashSet<string>>("the FE expects a JSON array, not a JSON object");
    }

    [Fact]
    public async Task GetVisibleModulesForUserAsync_Result_IsSortedAlphabetically()
    {
        var (svc, perms) = Build();
        // Insertion order: Z, A, M. Output should be A, M, Z.
        perms.Setup(p => p.GetVisibleModulesForUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new HashSet<string> { "Z", "A", "M" });

        var result = await svc.GetVisibleModulesForUserAsync(AdminUserId, CancellationToken.None);

        result.Should().ContainInOrder("A", "M", "Z");
    }

    [Fact]
    public async Task GetVisibleModulesForUserAsync_EmptyForUnknownUser()
    {
        var (svc, perms) = Build();
        perms.Setup(p => p.GetVisibleModulesForUserAsync(UnknownUserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new HashSet<string>());

        var result = await svc.GetVisibleModulesForUserAsync(UnknownUserId, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVisibleModulesForUserAsync_AdminHasAll()
    {
        var (svc, perms) = Build();
        var all = new HashSet<string>
        {
            "Projects", "Finance", "HR", "Payroll", "Inventory",
            "Procurement", "AR", "Companies", "Identity", "Dashboard"
        };
        perms.Setup(p => p.GetVisibleModulesForUserAsync(AdminUserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(all);

        var result = await svc.GetVisibleModulesForUserAsync(AdminUserId, CancellationToken.None);

        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetVisibleModulesForUserAsync_HRHasExactly3()
    {
        var (svc, perms) = Build();
        perms.Setup(p => p.GetVisibleModulesForUserAsync(HrUserId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new HashSet<string> { "HR", "Companies", "Dashboard" });

        var result = await svc.GetVisibleModulesForUserAsync(HrUserId, CancellationToken.None);

        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(new[] { "HR", "Companies", "Dashboard" });
    }
}
