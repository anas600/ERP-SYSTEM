// Sprint 64 Wave 2A (DEC-223) — Tests for SubProgressBillingsController (4 tests).
//
// The controller is a thin wrapper around ISubProgressBillingService. We mock
// the service to avoid DB / company-context plumbing, and assert routing +
// status code + ProblemDetails shape.
//
// L19 / DEC-095: the controller reads userId from the JWT, never from the
// request body (L186 / Sprint 61 fix).

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ERPSystem.Host.Controllers;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ERPSystem.Tests.Projects;

public class SubProgressBillingsControllerTests
{
    // ===== Helpers =====

    private static SubProgressBillingsController NewController(
        ISubProgressBillingService svc, Guid userId)
    {
        var c = new SubProgressBillingsController(svc);
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

    private static SubProgressBillingResponse MakeResponse(string billingNumber = "B-001") => new(
        Id: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        SubContractId: Guid.NewGuid(),
        BillingNumber: billingNumber,
        BillingDate: DateTime.UtcNow.Date,
        PeriodFrom: null,
        PeriodTo: null,
        WorkCompletedPercent: 30m,
        GrossAmount: 15_000m,
        RetentionDeducted: 1_500m,
        PreviousBillingsAmount: 0m,
        NetPayable: 13_500m,
        Status: 1,
        StatusName: "مسودة",
        Notes: null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow
    );

    private static CreateSubProgressBillingRequest MakeCreate() => new(
        BillingNumber: "B-001",
        BillingDate: DateTime.UtcNow.Date,
        PeriodFrom: null,
        PeriodTo: null,
        WorkCompletedPercent: 30m,
        Notes: null
    );

    private static UpdateSubProgressBillingRequest MakeUpdate() => new(
        PeriodFrom: null,
        PeriodTo: null,
        WorkCompletedPercent: 25m,
        Notes: "تعديل"
    );

    // ===== 1. ListBySubContract_Returns200_WithRows =====

    [Fact]
    public async Task ListBySubContract_Returns200_WithRows()
    {
        var rows = new List<SubProgressBillingResponse>
        {
            MakeResponse("B-001"),
            MakeResponse("B-002"),
        };
        var svc = new Mock<ISubProgressBillingService>();
        svc.Setup(s => s.ListBySubContractAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubProgressBillingResult<IReadOnlyList<SubProgressBillingResponse>>.Ok(rows));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.ListBySubContract(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<SubProgressBillingResponse>>()
            .Which.Should().HaveCount(2);
    }

    // ===== 2. Create_Returns201_WithBody =====

    [Fact]
    public async Task Create_Returns201_WithBody()
    {
        var response = MakeResponse();
        var svc = new Mock<ISubProgressBillingService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CreateSubProgressBillingRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubProgressBillingResult<SubProgressBillingResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Create(response.SubContractId, MakeCreate(), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.Value.Should().BeOfType<SubProgressBillingResponse>()
            .Which.Id.Should().Be(response.Id);
    }

    // ===== 3. Update_Returns200_OnSuccess =====

    [Fact]
    public async Task Update_Returns200_OnSuccess()
    {
        var response = MakeResponse();
        var svc = new Mock<ISubProgressBillingService>();
        svc.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<UpdateSubProgressBillingRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubProgressBillingResult<SubProgressBillingResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Update(response.Id, MakeUpdate(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().BeOfType<SubProgressBillingResponse>()
            .Which.Id.Should().Be(response.Id);
    }

    // ===== 4. Approve_Returns200_OnSuccess =====

    [Fact]
    public async Task Approve_Returns200_OnSuccess()
    {
        var response = MakeResponse() with { Status = 2, StatusName = "معتمد" };
        var svc = new Mock<ISubProgressBillingService>();
        svc.Setup(s => s.ApproveAsync(It.IsAny<Guid>(), response.Id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubProgressBillingResult<SubProgressBillingResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Approve(response.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().BeOfType<SubProgressBillingResponse>()
            .Which.Status.Should().Be(2);
    }
}
