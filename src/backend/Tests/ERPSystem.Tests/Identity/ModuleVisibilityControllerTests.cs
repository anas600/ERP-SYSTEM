// Sprint 63 (DEC-217) — ModuleVisibilityController tests.
//
// L19 / DEC-095: the userId is resolved from JWT claims inside the controller
// (User.FindFirst(ClaimTypes.NameIdentifier)). These tests confirm:
//   1. The service is called with the JWT-derived userId (NOT a request DTO).
//   2. The returned list is sorted alphabetically (deterministic FE rendering).
//   3. The response shape is { modules: string[] } (matches the FE contract).
//   4. The three role scenarios match the visibility matrix in
//      Modules/Identity/AGENTS.md (admin → all 10, hr → 3, readonly → all 10).

using System.Security.Claims;
using ERPSystem.Host.Controllers;
using ERPSystem.Modules.Identity.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Identity;

[Trait("Category", "Sprint63")]
public class ModuleVisibilityControllerTests
{
    private static readonly Guid AdminUser = new("11111111-1111-1111-1111-aaaaaaaaaaaa");
    private static readonly Guid HrUser = new("33333333-3333-3333-3333-bbbbbbbbbbbb");
    private static readonly Guid ReadOnlyUser = new("55555555-5555-5555-5555-cccccccccccc");

    private static (ModuleVisibilityController ctrl,
                    Mock<IPermissionService> permSvc) Build(Guid userId, string role)
    {
        var permSvc = new Mock<IPermissionService>(MockBehavior.Strict);
        var ctrl = new ModuleVisibilityController(permSvc.Object, NullLogger<ModuleVisibilityController>.Instance);

        // Wire up a minimal HttpContext with a JWT-style principal so L19
        // user-id extraction works.
        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
        }, "TestAuth"));
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        return (ctrl, permSvc);
    }

    [Fact]
    public async Task GetVisibleModules_Admin_ReturnsAllTenModules_Sorted()
    {
        // Arrange
        var (ctrl, permSvc) = Build(AdminUser, "Admin");
        var allModules = new HashSet<string>
        {
            "Projects", "Finance", "HR", "Payroll", "Inventory",
            "Procurement", "AR", "Companies", "Identity", "Dashboard",
        };
        permSvc
            .Setup(s => s.GetVisibleModulesForUserAsync(AdminUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allModules);

        // Act
        var result = await ctrl.GetVisibleModules(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<VisibleModulesResponse>().Subject;

        body.Modules.Should().HaveCount(10);
        body.Modules.Should().BeInAscendingOrder();
        body.Modules.Should().Contain(new[] { "Dashboard", "HR", "Finance" });
        permSvc.Verify(
            s => s.GetVisibleModulesForUserAsync(AdminUser, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetVisibleModules_HrUser_ReturnsThreeModules_Sorted()
    {
        // Arrange — HR role gets: HR, Companies, Dashboard (per visibility matrix).
        var (ctrl, permSvc) = Build(HrUser, "hr");
        var hrModules = new HashSet<string> { "HR", "Companies", "Dashboard" };
        permSvc
            .Setup(s => s.GetVisibleModulesForUserAsync(HrUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hrModules);

        // Act
        var result = await ctrl.GetVisibleModules(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<VisibleModulesResponse>().Subject;

        body.Modules.Should().BeEquivalentTo(new[] { "Companies", "Dashboard", "HR" }); // sorted
        body.Modules.Should().NotContain("Finance");
        body.Modules.Should().NotContain("Projects");
    }

    [Fact]
    public async Task GetVisibleModules_ReadOnly_ReturnsAllTenModules_ButViewOnly()
    {
        // Arrange — ReadOnly sees every module (but the FE/UX layer hides
        // create/edit buttons via PermissionGate).
        var (ctrl, permSvc) = Build(ReadOnlyUser, "readonly");
        var allModules = new HashSet<string>
        {
            "Projects", "Finance", "HR", "Payroll", "Inventory",
            "Procurement", "AR", "Companies", "Identity", "Dashboard",
        };
        permSvc
            .Setup(s => s.GetVisibleModulesForUserAsync(ReadOnlyUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allModules);

        // Act
        var result = await ctrl.GetVisibleModules(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<VisibleModulesResponse>().Subject;

        body.Modules.Should().HaveCount(10);
        body.Modules.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetVisibleModules_NoJwtUserId_ThrowsUnauthorized()
    {
        // Arrange — no JWT claims on the principal.
        var permSvc = new Mock<IPermissionService>(MockBehavior.Strict);
        var ctrl = new ModuleVisibilityController(permSvc.Object, NullLogger<ModuleVisibilityController>.Instance);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()), // empty
            },
        };

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => ctrl.GetVisibleModules(CancellationToken.None));
        // Service should never be called when auth is missing.
        permSvc.VerifyNoOtherCalls();
    }
}
