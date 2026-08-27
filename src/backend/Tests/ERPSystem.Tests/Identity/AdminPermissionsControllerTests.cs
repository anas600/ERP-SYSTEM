// Sprint 63 (DEC-216) — AdminPermissionsController tests.
//
// These tests exercise the 5 endpoints of the admin RBAC management
// controller in isolation (no ASP.NET pipeline, no JWT). The controller's
// [RequirePermission] attribute is bypassed because the controller is
// instantiated directly and the action methods are invoked through normal
// method calls — not through the MVC pipeline.
//
// In other words: the controller-level [RequirePermission] filter is NOT
// exercised by these tests; what IS tested is the controller's own
// orchestration: repo calls and response shape.

using System.Security.Claims;
using ERPSystem.Host.Controllers;
using ERPSystem.Modules.Identity.Application.Services;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Modules.Identity.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Identity;

[Trait("Category", "Sprint63")]
public class AdminPermissionsControllerTests
{
    private static readonly Guid RoleId = new("11111111-2222-3333-4444-555555555555");
    private static readonly Guid User1 = new("11111111-aaaa-bbbb-cccc-000000000001");
    private static readonly Guid User2 = new("11111111-aaaa-bbbb-cccc-000000000002");

    private static (AdminPermissionsController ctrl,
                    Mock<IPermissionRepository> perms,
                    Mock<IRolePermissionRepository> rolePerms,
                    Mock<IRoleRepository> roles,
                    Mock<IPermissionService> permSvc) Build()
    {
        var perms = new Mock<IPermissionRepository>(MockBehavior.Strict);
        var rolePerms = new Mock<IRolePermissionRepository>(MockBehavior.Strict);
        var roles = new Mock<IRoleRepository>(MockBehavior.Strict);
        var permSvc = new Mock<IPermissionService>(MockBehavior.Strict);

        var ctrl = new AdminPermissionsController(
            perms.Object, rolePerms.Object, roles.Object, permSvc.Object,
            NullLogger<AdminPermissionsController>.Instance);

        // The controller's `User` property comes from ControllerContext.HttpContext.
        // Wire up a minimal HttpContext with an admin principal so the L19
        // user-id extraction in InvalidateCache doesn't NRE.
        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
        }, "TestAuth"));
        ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

        return (ctrl, perms, rolePerms, roles, permSvc);
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

    // ============ 1. GET /api/admin/permissions → 200 List<PermissionResponse> ============

    [Fact]
    public async Task List_Returns200_WithAllPermissions()
    {
        var (ctrl, perms, _, _, _) = Build();
        perms.Setup(p => p.ListAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Permission>
             {
                 P("projects.view", "Projects"),
                 P("finance.accounts.view", "Finance"),
                 P("hr.employees.view", "HR"),
             });

        var result = await ctrl.List(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<PermissionResponse>>().Subject.ToList();
        list.Should().HaveCount(3);
        list.Select(x => x.Code).Should().BeEquivalentTo(new[] { "projects.view", "finance.accounts.view", "hr.employees.view" });
    }

    // ============ 2. GET /api/admin/roles/{roleId}/permissions → 200 ============

    [Fact]
    public async Task GetRolePermissions_Returns200_WithRolePermissions()
    {
        var (ctrl, _, rolePerms, _, _) = Build();
        rolePerms.Setup(p => p.ListByRoleAsync(RoleId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Permission>
                 {
                     P("projects.view", "Projects"),
                     P("projects.create", "Projects"),
                 });

        var result = await ctrl.GetRolePermissions(RoleId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<PermissionResponse>>().Subject.ToList();
        list.Should().HaveCount(2);
        list.Should().OnlyContain(p => p.Module == "Projects");
    }

    // ============ 3. POST /api/admin/roles/{roleId}/permissions → 201 ============

    [Fact]
    public async Task AssignPermission_Returns201_WhenNew()
    {
        var (ctrl, perms, rolePerms, roles, permSvc) = Build();
        var perm = P("projects.create", "Projects");
        var req = new AdminPermissionsController.AssignPermissionRequest { PermissionId = perm.Id };

        perms.Setup(p => p.GetByIdAsync(perm.Id, It.IsAny<CancellationToken>())).ReturnsAsync(perm);
        rolePerms.Setup(p => p.InsertAsync(It.Is<RolePermission>(rp => rp.RoleId == RoleId && rp.PermissionId == perm.Id), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        // Invalidate-cache: assume the role has no members for this test
        // (we only care about the create path here).
        roles.Setup(r => r.GetUserIdsInRoleAsync(RoleId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Guid>());

        var result = await ctrl.Assign(RoleId, req, CancellationToken.None);

        result.Should().BeOfType<StatusCodeResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        perms.Verify(p => p.GetByIdAsync(perm.Id, It.IsAny<CancellationToken>()), Times.Once);
        rolePerms.Verify(p => p.InsertAsync(It.Is<RolePermission>(rp => rp.RoleId == RoleId && rp.PermissionId == perm.Id), It.IsAny<CancellationToken>()), Times.Once);
        permSvc.Verify(p => p.InvalidateCacheAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ============ 4. DELETE /api/admin/roles/{roleId}/permissions/{permId} → 204 ============

    [Fact]
    public async Task RemovePermission_Returns204_WhenExists()
    {
        var (ctrl, _, rolePerms, roles, _) = Build();
        var permId = Guid.NewGuid();

        rolePerms.Setup(p => p.DeleteAsync(RoleId, permId, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        roles.Setup(r => r.GetUserIdsInRoleAsync(RoleId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Guid>());

        var result = await ctrl.Revoke(RoleId, permId, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        rolePerms.Verify(p => p.DeleteAsync(RoleId, permId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============ 5. POST /api/admin/roles/{roleId}/invalidate-cache → 200 ============
    // The role has 2 members → IPermissionService.InvalidateCacheAsync is
    // called once for each user. The response is 200 OK.

    [Fact]
    public async Task InvalidateCache_Returns200_AndCallsInvalidateForAllUsersInRole()
    {
        var (ctrl, _, _, roles, permSvc) = Build();
        roles.Setup(r => r.GetUserIdsInRoleAsync(RoleId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Guid> { User1, User2 });
        permSvc.Setup(p => p.InvalidateCacheAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var result = await ctrl.InvalidateCache(RoleId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        permSvc.Verify(p => p.InvalidateCacheAsync(User1, It.IsAny<CancellationToken>()), Times.Once);
        permSvc.Verify(p => p.InvalidateCacheAsync(User2, It.IsAny<CancellationToken>()), Times.Once);
        permSvc.Verify(p => p.InvalidateCacheAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        ok.Value.Should().NotBeNull();
    }
}
