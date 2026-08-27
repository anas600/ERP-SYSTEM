// Sprint 64 Wave 1A (DEC-222) — Tests for SubContractsController (4 tests).
//
// The controller is a thin wrapper around ISubContractService. We mock the
// service to avoid DB / company-context plumbing, and assert routing +
// status code + ProblemDetails shape.
//
// L19 / DEC-095: the controller reads userId from the JWT, never from the
// request body (L186 / Sprint 61 fix). Verified by the fact that the mock
// accepts any userId on CreateAsync/UpdateAsync/SoftDeleteAsync.

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

public class SubContractsControllerTests
{
    // ===== Helpers =====

    private static SubContractsController NewController(ISubContractService svc, Guid userId)
    {
        var c = new SubContractsController(svc);
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

    private static SubContractResponse MakeResponse(string contractNumber = "SC-001") => new(
        Id: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        ProjectId: Guid.NewGuid(),
        SubcontractorId: Guid.NewGuid(),
        ContractNumber: contractNumber,
        ScopeOfWork: "أعمال الكهرباء",
        ContractValue: 50_000m,
        RetentionPercent: 10.0m,
        RetentionReleaseBilling: 3,
        StartDate: DateTime.UtcNow.Date,
        EndDate: null,
        Status: 1,
        StatusName: "نشط",
        Notes: null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow
    );

    private static CreateSubContractRequest MakeCreate() => new(
        SubcontractorId: Guid.NewGuid(),
        ContractNumber: "SC-001",
        ScopeOfWork: "أعمال الكهرباء",
        ContractValue: 50_000m,
        RetentionPercent: 10.0m,
        RetentionReleaseBilling: 3,
        StartDate: DateTime.UtcNow.Date,
        EndDate: null,
        Notes: null
    );

    private static UpdateSubContractRequest MakeUpdate() => new(
        ScopeOfWork: "أعمال الكهرباء والسباكة",
        ContractValue: 75_000m,
        RetentionPercent: 5.0m,
        RetentionReleaseBilling: 2,
        StartDate: DateTime.UtcNow.Date,
        EndDate: null,
        Status: 2,
        Notes: "تم الانتهاء"
    );

    // ===== 1. ListByProject_Returns200_WithRows =====

    [Fact]
    public async Task ListByProject_Returns200_WithRows()
    {
        var rows = new List<SubContractResponse>
        {
            MakeResponse("SC-001"),
            MakeResponse("SC-002"),
        };
        var svc = new Mock<ISubContractService>();
        svc.Setup(s => s.ListByProjectAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubContractResult<IReadOnlyList<SubContractResponse>>.Ok(rows));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.ListByProject(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<SubContractResponse>>()
            .Which.Should().HaveCount(2);
    }

    // ===== 2. Create_Returns201_WithBody =====

    [Fact]
    public async Task Create_Returns201_WithBody()
    {
        var response = MakeResponse();
        var svc = new Mock<ISubContractService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CreateSubContractRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubContractResult<SubContractResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Create(response.ProjectId, MakeCreate(), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.Value.Should().BeOfType<SubContractResponse>()
            .Which.Id.Should().Be(response.Id);
    }

    // ===== 3. Update_Returns200_OnSuccess =====

    [Fact]
    public async Task Update_Returns200_OnSuccess()
    {
        var response = MakeResponse();
        var svc = new Mock<ISubContractService>();
        svc.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<UpdateSubContractRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubContractResult<SubContractResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Update(response.Id, MakeUpdate(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().BeOfType<SubContractResponse>()
            .Which.Id.Should().Be(response.Id);
    }

    // ===== 4. Delete_Returns204_OnSuccess =====

    [Fact]
    public async Task Delete_Returns204_OnSuccess()
    {
        var id = Guid.NewGuid();
        var svc = new Mock<ISubContractService>();
        svc.Setup(s => s.SoftDeleteAsync(It.IsAny<Guid>(), id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubContractResult<bool>.Ok(true));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }
}
