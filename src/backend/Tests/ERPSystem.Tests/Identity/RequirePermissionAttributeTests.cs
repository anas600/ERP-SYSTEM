// Sprint 63 (DEC-215) — [RequirePermission] action filter tests.
//
// These tests exercise the 6 documented branches of the attribute:
//   - Authorization:Enabled == false → no-op (test mode escape hatch)
//   - No JWT → 401 Unauthorized
//   - Admin role → bypass (no IPermissionService call)
//   - Non-admin + has permission → allow
//   - Non-admin + lacks permission → 403
//   - L19 / DEC-095: UserId read from JWT, NOT from request DTO
//
// All tests set Authorization:Enabled=true in-memory so the attribute
// actually runs. The 50+ existing tests use Authorization:Enabled=false
// in appsettings.Test.json to skip the filter entirely.

using System.Security.Claims;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Identity.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Identity;

[Trait("Category", "Sprint63")]
public class RequirePermissionAttributeTests
{
    private static readonly Guid TestUserId = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private const string TestPermission = "projects.test.create";

    /// <summary>
    /// Build an AuthorizationFilterContext with the given JWT, role, and
    /// in-memory configuration. The HttpContext's RequestServices returns
    /// the supplied IServiceProvider (used to resolve IConfiguration and
    /// IPermissionService).
    /// </summary>
    private static AuthorizationFilterContext BuildContext(
        ClaimsPrincipal? user,
        IConfiguration config,
        Mock<IPermissionService>? permService = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(config);
        if (permService != null) services.AddSingleton(permService.Object);
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        if (user != null) httpContext.User = user;

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static IConfiguration ConfigWithAuthEnabled(bool enabled) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authorization:Enabled"] = enabled ? "true" : "false",
            })
            .Build();

    private static ClaimsPrincipal UserWithId(Guid id) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
        }, "TestAuth"));

    private static ClaimsPrincipal UserWithIdAndRole(Guid id, string role) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, role),
        }, "TestAuth"));

    // ============ 1. Authorization:Enabled == false → no-op ============

    [Fact]
    public async Task OnAuthorizationAsync_AllowsRequest_WhenAuthorizationDisabled()
    {
        var config = ConfigWithAuthEnabled(false);
        var permService = new Mock<IPermissionService>(MockBehavior.Strict); // strict — must NOT be called
        var ctx = BuildContext(UserWithId(TestUserId), config, permService);

        var attr = new RequirePermissionAttribute(TestPermission);
        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull("when Authorization:Enabled=false the filter is a no-op");
        permService.VerifyNoOtherCalls();
    }

    // ============ 2. No JWT → 401 Unauthorized ============

    [Fact]
    public async Task OnAuthorizationAsync_Returns401_WhenNoJwt()
    {
        var config = ConfigWithAuthEnabled(true);
        var permService = new Mock<IPermissionService>(MockBehavior.Strict);
        // Anonymous principal — no NameIdentifier claim
        var anon = new ClaimsPrincipal(new ClaimsIdentity());
        var ctx = BuildContext(anon, config, permService);

        var attr = new RequirePermissionAttribute(TestPermission);
        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeOfType<UnauthorizedResult>();
        permService.VerifyNoOtherCalls();
    }

    // ============ 3. Admin role → bypass ============

    [Fact]
    public async Task OnAuthorizationAsync_BypassesCheck_ForAdminRole()
    {
        var config = ConfigWithAuthEnabled(true);
        var permService = new Mock<IPermissionService>(MockBehavior.Strict);
        var ctx = BuildContext(UserWithIdAndRole(TestUserId, "Admin"), config, permService);

        var attr = new RequirePermissionAttribute(TestPermission);
        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull("Admin role bypasses the permission check");
        permService.VerifyNoOtherCalls();
    }

    // ============ 4. Non-admin + lacks permission → 403 ============

    [Fact]
    public async Task OnAuthorizationAsync_Returns403_WhenUserLacksPermission()
    {
        var config = ConfigWithAuthEnabled(true);
        var permService = new Mock<IPermissionService>(MockBehavior.Strict);
        permService.Setup(p => p.HasPermissionAsync(TestUserId, TestPermission, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false);
        var ctx = BuildContext(UserWithId(TestUserId), config, permService);

        var attr = new RequirePermissionAttribute(TestPermission);
        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        permService.Verify(p => p.HasPermissionAsync(TestUserId, TestPermission, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============ 5. Non-admin + has permission → allow ============

    [Fact]
    public async Task OnAuthorizationAsync_AllowsRequest_WhenUserHasPermission()
    {
        var config = ConfigWithAuthEnabled(true);
        var permService = new Mock<IPermissionService>(MockBehavior.Strict);
        permService.Setup(p => p.HasPermissionAsync(TestUserId, TestPermission, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true);
        var ctx = BuildContext(UserWithId(TestUserId), config, permService);

        var attr = new RequirePermissionAttribute(TestPermission);
        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull("user has the required permission");
        permService.Verify(p => p.HasPermissionAsync(TestUserId, TestPermission, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============ 6. L19 / DEC-095: UserId from JWT, NOT from request ============

    [Fact]
    public async Task OnAuthorizationAsync_UsesUserIdFromJwt_NotFromRequest()
    {
        var config = ConfigWithAuthEnabled(true);
        var permService = new Mock<IPermissionService>(MockBehavior.Strict);

        // A different user (NOT the JWT subject) would have the permission.
        // The attribute must NOT honor any "userId" that might be smuggled
        // through the request — only the JWT NameIdentifier claim counts.
        var impostorUserId = Guid.NewGuid();
        var jwtUserId = TestUserId;

        permService.Setup(p => p.HasPermissionAsync(jwtUserId, TestPermission, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false);
        permService.Setup(p => p.HasPermissionAsync(impostorUserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true); // would 200 if the attribute called with the wrong id

        var ctx = BuildContext(UserWithId(jwtUserId), config, permService);

        var attr = new RequirePermissionAttribute(TestPermission);
        await attr.OnAuthorizationAsync(ctx);

        // The attribute must have queried with the JWT userId (got 403), and
        // must NOT have queried with the impostor userId (which would 200).
        permService.Verify(p => p.HasPermissionAsync(jwtUserId, TestPermission, It.IsAny<CancellationToken>()), Times.Once);
        permService.Verify(p => p.HasPermissionAsync(impostorUserId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }
}
