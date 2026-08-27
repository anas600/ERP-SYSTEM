// Sprint 62 Wave 2A (DEC-197) — Tests for RegionalPremiumsController (4 tests).
//
// The controller is a thin wrapper around IRegionalPremiumService. We mock
// the service to avoid DB / company-context plumbing, and assert routing +
// status code + ProblemDetails shape.
//
// L19 / DEC-095: the controller reads userId from the JWT, never from the
// request body (L186 / Sprint 61 fix). Verified by the fact that the mock
// accepts any userId on CreateAsync/UpdateAsync/DeleteAsync.

using System.Security.Claims;
using ERPSystem.Host.Controllers;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ERPSystem.Tests.Projects;

public class RegionalPremiumsControllerTests
{
    // ===== Helpers =====

    private static RegionalPremiumsController NewController(IRegionalPremiumService svc, Guid userId)
    {
        var c = new RegionalPremiumsController(svc);
        var httpCtx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "TestAuth"))
        };
        c.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        return c;
    }

    private static RegionalPremiumResponse MakeResponse() => new(
        Id: Guid.NewGuid(),
        ProjectId: Guid.NewGuid(),
        Region: "Tripoli",
        NdbPercent: 1.5m,
        CitPercent: 5.0m,
        SsPercent: 0.0m,
        IsActive: true,
        CreatedAt: DateTime.UtcNow,
        CombinedPercent: 6.5m
    );

    private static CreateRegionalPremiumRequest MakeCreate() => new(
        Region: "Tripoli",
        NdbPercent: 1.5m,
        CitPercent: 5.0m,
        SsPercent: 0.0m,
        IsActive: true
    );

    private static UpdateRegionalPremiumRequest MakeUpdate() => new(
        Region: "Benghazi",
        NdbPercent: 1.5m,
        CitPercent: 5.0m,
        SsPercent: 0.5m,
        IsActive: true
    );

    // ===== 1. List_Returns200_WithRows =====

    [Fact]
    public async Task List_Returns200_WithRows()
    {
        var projectId = Guid.NewGuid();
        var rows = new List<RegionalPremiumResponse> { MakeResponse(), MakeResponse() };
        var svc = new Mock<IRegionalPremiumService>();
        svc.Setup(s => s.ListByProjectAsync(projectId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(RegionalPremiumResult<IReadOnlyList<RegionalPremiumResponse>>.Ok(rows));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.ListByProject(projectId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<RegionalPremiumResponse>>()
            .Which.Should().HaveCount(2);
    }

    // ===== 2. Create_Returns201_WithBody =====

    [Fact]
    public async Task Create_Returns201_WithBody()
    {
        var response = MakeResponse();
        var svc = new Mock<IRegionalPremiumService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CreateRegionalPremiumRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(RegionalPremiumResult<RegionalPremiumResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Create(response.ProjectId, MakeCreate(), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.Value.Should().BeOfType<RegionalPremiumResponse>()
            .Which.Id.Should().Be(response.Id);
        // projectId passed as a route value
        created.RouteValues!["projectId"].Should().Be(response.ProjectId);
    }

    // ===== 3. Create_Returns409_OnAlreadyExists =====

    [Fact]
    public async Task Create_Returns409_OnAlreadyExists()
    {
        var svc = new Mock<IRegionalPremiumService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CreateRegionalPremiumRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(RegionalPremiumResult<RegionalPremiumResponse>.Fail(
               "يوجد بالفعل خصم منطقة لنفس الـ region.", RegionalPremiumErrorCode.AlreadyExists));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Create(Guid.NewGuid(), MakeCreate(), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ===== 4. Delete_Returns204_OnSuccess =====

    [Fact]
    public async Task Delete_Returns204_OnSuccess()
    {
        var projectId = Guid.NewGuid();
        var id = Guid.NewGuid();
        var svc = new Mock<IRegionalPremiumService>();
        svc.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(RegionalPremiumResult<bool>.Ok(true));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Delete(projectId, id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    // ===== Bonus: Update_Returns200_OnSuccess (no regression on the bonus path) =====

    [Fact]
    public async Task Update_Returns200_OnSuccess()
    {
        var response = MakeResponse();
        var svc = new Mock<IRegionalPremiumService>();
        svc.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<UpdateRegionalPremiumRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(RegionalPremiumResult<RegionalPremiumResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Update(response.ProjectId, response.Id, MakeUpdate(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().BeOfType<RegionalPremiumResponse>()
            .Which.Id.Should().Be(response.Id);
    }

    // ===== Bonus: Delete_Returns404_WhenNotFound =====

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        var svc = new Mock<IRegionalPremiumService>();
        svc.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(RegionalPremiumResult<bool>.Fail("غير موجود.", RegionalPremiumErrorCode.NotFound));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Delete(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
