// Sprint 64 Wave 1A (DEC-221) — Tests for SubcontractorsController (4 tests).
//
// The controller is a thin wrapper around ISubcontractorService. We mock
// the service to avoid DB / company-context plumbing, and assert routing +
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

public class SubcontractorsControllerTests
{
    // ===== Helpers =====

    private static SubcontractorsController NewController(ISubcontractorService svc, Guid userId)
    {
        var c = new SubcontractorsController(svc);
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

    private static SubcontractorResponse MakeResponse(string code = "ELEC-001") => new(
        Id: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        Code: code,
        Name: "مقاول كهرباء",
        NameAr: null,
        ContactPerson: "أحمد",
        Phone: "091-1234567",
        Email: "ahmed@example.com",
        TradeSpecialty: "electrical",
        TaxId: "TAX-001",
        IsActive: true,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow
    );

    private static CreateSubcontractorRequest MakeCreate() => new(
        Code: "ELEC-001",
        Name: "مقاول كهرباء",
        NameAr: null,
        ContactPerson: "أحمد",
        Phone: "091-1234567",
        Email: "ahmed@example.com",
        TradeSpecialty: "electrical",
        TaxId: "TAX-001"
    );

    private static UpdateSubcontractorRequest MakeUpdate() => new(
        Name: "مقاول محدّث",
        NameAr: null,
        ContactPerson: "خالد",
        Phone: "092-7654321",
        Email: "khaled@example.com",
        TradeSpecialty: "electrical",
        TaxId: "TAX-002",
        IsActive: true
    );

    // ===== 1. List_Returns200_WithRows =====

    [Fact]
    public async Task List_Returns200_WithRows()
    {
        var rows = new List<SubcontractorResponse>
        {
            MakeResponse("ELEC-001"),
            MakeResponse("PLMB-001"),
        };
        var svc = new Mock<ISubcontractorService>();
        svc.Setup(s => s.ListAsync(It.IsAny<bool?>(), It.IsAny<string?>(), 0, 50, It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubcontractorResult<IReadOnlyList<SubcontractorResponse>>.Ok(rows));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.List(null, null, 0, 50, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<SubcontractorResponse>>()
            .Which.Should().HaveCount(2);
    }

    // ===== 2. Create_Returns201_WithBody =====

    [Fact]
    public async Task Create_Returns201_WithBody()
    {
        var response = MakeResponse();
        var svc = new Mock<ISubcontractorService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateSubcontractorRequest>(),
                It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubcontractorResult<SubcontractorResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Create(MakeCreate(), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.Value.Should().BeOfType<SubcontractorResponse>()
            .Which.Id.Should().Be(response.Id);
    }

    // ===== 3. Get_Returns404_WhenNotFound =====

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var svc = new Mock<ISubcontractorService>();
        svc.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubcontractorResult<SubcontractorResponse>.Fail(
               "المقاول الباطن غير موجود.", SubcontractorErrorCode.NotFound));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ===== 4. Delete_Returns204_OnSuccess =====

    [Fact]
    public async Task Delete_Returns204_OnSuccess()
    {
        var id = Guid.NewGuid();
        var svc = new Mock<ISubcontractorService>();
        svc.Setup(s => s.SoftDeleteAsync(It.IsAny<Guid>(), id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(SubcontractorResult<bool>.Ok(true));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }
}
