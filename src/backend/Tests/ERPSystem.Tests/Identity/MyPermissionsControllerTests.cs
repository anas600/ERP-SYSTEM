// Sprint 63 (DEC-218) — MyPermissionsController tests.
//
// Mirrors ModuleVisibilityControllerTests but for /api/me/permissions.
// L19 / DEC-095: the userId is resolved from JWT claims, NOT from a request DTO.

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
public class MyPermissionsControllerTests
{
    private static readonly Guid AdminUser = new("11111111-1111-1111-1111-dddddddddddd");
    private static readonly Guid HrUser = new("33333333-3333-3333-3333-eeeeeeeeeeee");

    private static (MyPermissionsController ctrl,
                    Mock<IPermissionService> permSvc) Build(Guid userId, string role)
    {
        var permSvc = new Mock<IPermissionService>(MockBehavior.Strict);
        var ctrl = new MyPermissionsController(permSvc.Object, NullLogger<MyPermissionsController>.Instance);

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
    public async Task GetPermissions_Admin_ReturnsAllPermissions_IncludingAdminAllWildcard()
    {
        // Arrange
        var (ctrl, permSvc) = Build(AdminUser, "Admin");
        var adminPerms = new HashSet<string>
        {
            "admin.all", "projects.view", "projects.create", "projects.update",
            "finance.accounts.view", "hr.employees.view", "hr.employees.create",
        };
        permSvc
            .Setup(s => s.GetPermissionsForUserAsync(AdminUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminPerms);

        // Act
        var result = await ctrl.GetPermissions(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<MyPermissionsResponse>().Subject;

        body.Permissions.Should().Contain("admin.all");
        body.Permissions.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetPermissions_HrUser_ReturnsOnlyHrAndPayrollAndCompanyPermissions()
    {
        // Arrange
        var (ctrl, permSvc) = Build(HrUser, "hr");
        var hrPerms = new HashSet<string>
        {
            "hr.employees.view", "hr.employees.create", "hr.employees.update",
            "hr.departments.view", "companies.view", "dashboard.view",
        };
        permSvc
            .Setup(s => s.GetPermissionsForUserAsync(HrUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hrPerms);

        // Act
        var result = await ctrl.GetPermissions(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<MyPermissionsResponse>().Subject;

        body.Permissions.Should().HaveCount(6);
        body.Permissions.Should().NotContain("admin.all");
        body.Permissions.Should().NotContain("finance.accounts.view");
        body.Permissions.Should().NotContain("projects.create");
    }

    [Fact]
    public async Task GetPermissions_NoJwtUserId_ThrowsUnauthorized()
    {
        // Arrange — no JWT claims on the principal.
        var permSvc = new Mock<IPermissionService>(MockBehavior.Strict);
        var ctrl = new MyPermissionsController(permSvc.Object, NullLogger<MyPermissionsController>.Instance);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()), // empty
            },
        };

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => ctrl.GetPermissions(CancellationToken.None));
        permSvc.VerifyNoOtherCalls();
    }
}
