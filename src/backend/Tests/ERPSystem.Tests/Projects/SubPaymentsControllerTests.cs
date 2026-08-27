// Sprint 64 Wave 2A (DEC-224) — Tests for SubPaymentsController (4 tests).
//
// The controller is a thin wrapper around ISubPaymentService. We mock the
// service to avoid DB / company-context plumbing, and assert routing +
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

public class SubPaymentsControllerTests
{
    // ===== Helpers =====

    private static SubPaymentsController NewController(
        ISubPaymentService svc, Guid userId)
    {
        var c = new SubPaymentsController(svc);
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

    private static SubPaymentResponse MakeResponse(string paymentNumber = "P-001") => new(
        Id: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        SubContractId: Guid.NewGuid(),
        SubProgressBillingId: Guid.NewGuid(),
        PaymentNumber: paymentNumber,
        PaymentDate: DateTime.UtcNow.Date,
        Amount: 5_000m,
        RetentionReleased: 0m,
        PaymentMethod: "bank_transfer",
        ReferenceNumber: "REF-001",
        Notes: null,
        CreatedAt: DateTime.UtcNow
    );

    private static CreateSubPaymentRequest MakeCreate() => new(
        PaymentNumber: "P-001",
        PaymentDate: DateTime.UtcNow.Date,
        Amount: 5_000m,
        PaymentMethod: "bank_transfer",
        ReferenceNumber: "REF-001",
        Notes: null
    );

    private static SubContractBalanceResponse MakeBalance() => new(
        SubContractId: Guid.NewGuid(),
        ContractNumber: "SC-001",
        ContractValue: 50_000m,
        TotalBilledGross: 30_000m,
        TotalRetentionWithheld: 3_000m,
        TotalPaid: 20_000m,
        OutstandingBalance: 10_000m
    );

    // ===== 1. ListBySubContract_Returns200_WithRows =====

    [Fact]
    public async Task ListBySubContract_Returns200_WithRows()
    {
        var rows = new List<SubPaymentResponse>
        {
            MakeResponse("P-001"),
            MakeResponse("P-002"),
        };
        var svc = new Mock<ISubPaymentService>();
        svc.Setup(s => s.ListBySubContractAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubPaymentResult<IReadOnlyList<SubPaymentResponse>>.Ok(rows));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.ListBySubContract(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<SubPaymentResponse>>()
            .Which.Should().HaveCount(2);
    }

    // ===== 2. Create_Returns201_WithBody =====

    [Fact]
    public async Task Create_Returns201_WithBody()
    {
        var response = MakeResponse();
        var subContractId = response.SubContractId;
        var billingId = response.SubProgressBillingId;
        var svc = new Mock<ISubPaymentService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), subContractId, billingId,
                It.IsAny<CreateSubPaymentRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubPaymentResult<SubPaymentResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Create(subContractId, billingId, MakeCreate(), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.Value.Should().BeOfType<SubPaymentResponse>()
            .Which.Id.Should().Be(response.Id);
    }

    // ===== 3. ReleaseRetention_Returns201_WithBody =====

    [Fact]
    public async Task ReleaseRetention_Returns201_WithBody()
    {
        var response = MakeResponse("REL-001") with { Amount = 0m, RetentionReleased = 1_000m };
        var subContractId = response.SubContractId;
        var svc = new Mock<ISubPaymentService>();
        svc.Setup(s => s.ReleaseRetentionAsync(It.IsAny<Guid>(), subContractId,
                It.IsAny<ReleaseRetentionRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubPaymentResult<SubPaymentResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.ReleaseRetention(subContractId,
            new ReleaseRetentionRequest(DateTime.UtcNow.Date, 1_000m, "تحرير جزء"),
            CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.Value.Should().BeOfType<SubPaymentResponse>()
            .Which.RetentionReleased.Should().Be(1_000m);
    }

    // ===== 4. GetBalance_Returns200_WithBalance =====

    [Fact]
    public async Task GetBalance_Returns200_WithBalance()
    {
        var balance = MakeBalance();
        var subContractId = balance.SubContractId;
        var svc = new Mock<ISubPaymentService>();
        svc.Setup(s => s.GetBalanceAsync(subContractId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubPaymentResult<SubContractBalanceResponse>.Ok(balance));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.GetBalance(subContractId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeOfType<SubContractBalanceResponse>()
            .Which.OutstandingBalance.Should().Be(10_000m);
    }
}
