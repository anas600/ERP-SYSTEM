// Sprint 64 Wave 3A (DEC-225) — Tests for SubStatementsController (3 tests).
//
// The controller is a thin wrapper around ISubStatementService. We mock the
// service to avoid DB / company-context plumbing, and assert routing +
// status code + ProblemDetails shape.

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

public class SubStatementsControllerTests
{
    // ===== Helpers =====

    private static SubStatementsController NewController(ISubStatementService svc, Guid userId)
    {
        var c = new SubStatementsController(svc);
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

    private static SubStatementResponse MakeStatement(Guid subContractId) => new()
    {
        SubContractId = subContractId,
        SubcontractorName = "مقاول الكهرباء",
        SubContractorCode = "ELEC-001",
        ContractNumber = "SC-001",
        ScopeOfWork = "أعمال الكهرباء",
        ContractValue = 50_000m,
        TotalBilledGross = 30_000m,
        TotalRetentionWithheld = 3_000m,
        TotalRetentionReleased = 0m,
        TotalPaid = 13_500m,
        OutstandingBalance = 16_500m,
        WorkCompletedToDate = 60m,
        BillingCount = 2,
        Status = 1,
        HealthStatus = "OK",
    };

    private static SubStatementSummaryResponse MakeSummary(Guid subcontractorId, Guid projectId) => new()
    {
        SubcontractorId = subcontractorId,
        SubcontractorName = "مقاول الكهرباء",
        ProjectId = projectId,
        ProjectName = "مشروع سكني",
        SubContractCount = 2,
        TotalContractValue = 70_000m,
        TotalBilled = 18_000m,
        TotalPaid = 5_000m,
        TotalOutstanding = 13_000m,
    };

    // ===== 1. GetBySubContract_Returns200_WithStatement =====

    [Fact]
    public async Task GetBySubContract_Returns200_WithStatement()
    {
        var subContractId = Guid.NewGuid();
        var statement = MakeStatement(subContractId);
        var svc = new Mock<ISubStatementService>();
        svc.Setup(s => s.GetBySubContractAsync(subContractId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubStatementResult<SubStatementResponse>.Ok(statement));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.GetBySubContract(subContractId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeOfType<SubStatementResponse>()
            .Which.HealthStatus.Should().Be("OK");
        ok.Value.Should().BeOfType<SubStatementResponse>()
            .Which.OutstandingBalance.Should().Be(16_500m);
    }

    // ===== 2. GetBySubContract_Returns404_WhenMissing =====

    [Fact]
    public async Task GetBySubContract_Returns404_WhenMissing()
    {
        var subContractId = Guid.NewGuid();
        var svc = new Mock<ISubStatementService>();
        svc.Setup(s => s.GetBySubContractAsync(subContractId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubStatementResult<SubStatementResponse>.Fail(
               "العقد الباطن غير موجود.", SubStatementErrorCode.NotFound));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.GetBySubContract(subContractId, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ===== 3. GetBySubcontractorAndProject_Returns200_WithSummary =====

    [Fact]
    public async Task GetBySubcontractorAndProject_Returns200_WithSummary()
    {
        var subcontractorId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var summary = MakeSummary(subcontractorId, projectId);
        var svc = new Mock<ISubStatementService>();
        svc.Setup(s => s.GetBySubcontractorAndProjectAsync(
                subcontractorId, projectId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubStatementResult<SubStatementSummaryResponse>.Ok(summary));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.GetBySubcontractorAndProject(subcontractorId, projectId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeOfType<SubStatementSummaryResponse>()
            .Which.SubContractCount.Should().Be(2);
        ok.Value.Should().BeOfType<SubStatementSummaryResponse>()
            .Which.TotalOutstanding.Should().Be(13_000m);
    }
}
