// Sprint 61 Wave 2A — EngineerReportsController + EngineerReportPhotosController tests (5+ tests).
// Strategy: the controllers are thin wrappers around IEngineerReportService — we mock the
// service to avoid DB / multipart pipeline. The tests focus on routing + status codes +
// body shapes.

using System.Security.Claims;
using ERPSystem.Host.Controllers;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Projects.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ERPSystem.Tests.Projects;

public class EngineerReportsControllerTests
{
    // ===== Helpers =====

    private static EngineerReportsController NewController(IEngineerReportService svc, Guid userId)
    {
        var c = new EngineerReportsController(svc);
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

    private static EngineerReportPhotosController NewPhotoController(IEngineerReportService svc, Guid userId, IWebHostEnvironment env)
    {
        var c = new EngineerReportPhotosController(svc, env, NullLogger<EngineerReportPhotosController>.Instance);
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

    private static EngineerReportResponse MakeReport() => new(
        Id: Guid.NewGuid(),
        ProjectId: Guid.NewGuid(),
        ReportDate: new DateTime(2026, 8, 27),
        EngineerId: Guid.NewGuid(),
        Status: "Draft",
        Weather: "مشمس",
        WorkDone: "صب خرسانة",
        Issues: null,
        CreatedAt: DateTime.UtcNow,
        UpdatedAt: DateTime.UtcNow,
        PhotosCount: 0,
        Photos: new List<EngineerReportPhotoResponse>(),
        Signoffs: new List<EngineerReportSignoffResponse>()
    );

    // ===== 1. GetById — returns 200 with body =====

    [Fact]
    public async Task GetById_ReturnsOk_WithResponse()
    {
        var response = MakeReport();
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.GetByIdAsync(response.Id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<EngineerReportResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.GetById(response.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeOfType<EngineerReportResponse>()
            .Which.Id.Should().Be(response.Id);
    }

    // ===== 2. GetById — 404 when not found =====

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<EngineerReportResponse>.Fail("nope", EngineerReportErrorCode.NotFound));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ===== 3. Create — 201 with body when service succeeds =====

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_OnSuccess()
    {
        var response = MakeReport();
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CreateEngineerReportRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<EngineerReportResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Create(response.ProjectId, new CreateEngineerReportRequest(
            ReportDate: response.ReportDate,
            EngineerId: response.EngineerId,
            Weather: response.Weather,
            WorkDone: response.WorkDone,
            Issues: response.Issues
        ), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.RouteValues!["id"].Should().Be(response.Id);
    }

    // ===== 4. Create — 409 when duplicate (project_id, report_date) =====

    [Fact]
    public async Task Create_DuplicateDate_ReturnsConflict()
    {
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<CreateEngineerReportRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<EngineerReportResponse>.Fail(
               "duplicate", EngineerReportErrorCode.AlreadyExists));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Create(Guid.NewGuid(), new CreateEngineerReportRequest(
            ReportDate: DateTime.UtcNow, EngineerId: Guid.NewGuid(),
            Weather: null, WorkDone: "x", Issues: null
        ), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ===== 5. Update — 200 with body on success =====

    [Fact]
    public async Task Update_ReturnsOk_OnSuccess()
    {
        var response = MakeReport();
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<UpdateEngineerReportRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<EngineerReportResponse>.Ok(response));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Update(response.Id,
            new UpdateEngineerReportRequest(Weather: "ماطر", WorkDone: "x", Issues: null),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ===== 6. Update — 400 when not in Draft =====

    [Fact]
    public async Task Update_NonDraft_ReturnsBadRequest()
    {
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<UpdateEngineerReportRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<EngineerReportResponse>.Fail(
               "not editable", EngineerReportErrorCode.InvalidStatusTransition));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Update(Guid.NewGuid(),
            new UpdateEngineerReportRequest(Weather: null, WorkDone: "x", Issues: null),
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ===== 7. Submit — 200 with Submitted status =====

    [Fact]
    public async Task Submit_Draft_ReturnsOk_WithSubmittedStatus()
    {
        var draft = MakeReport();
        var submitted = draft with { Status = "Submitted", UpdatedAt = DateTime.UtcNow };
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.SubmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<EngineerReportResponse>.Ok(submitted));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Submit(draft.Id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().BeOfType<EngineerReportResponse>()
            .Which.Status.Should().Be("Submitted");
    }

    // ===== 8. Signoff — 200 with signoff body on Approve =====

    [Fact]
    public async Task Signoff_Approve_ReturnsOk_WithApprovedTrue()
    {
        var reportId = Guid.NewGuid();
        var signoff = new EngineerReportSignoffResponse(
            Id: Guid.NewGuid(), ReportId: reportId, SignerId: Guid.NewGuid(),
            SignerRole: "PM", SignedAt: DateTime.UtcNow,
            SignatureText: "Anas", Comment: "ok", Approved: true);
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.SignoffAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<SignoffRequest>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<EngineerReportSignoffResponse>.Ok(signoff));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.Signoff(reportId,
            new SignoffRequest(SignerRole: "PM", SignatureText: "Anas", Comment: "ok", Approved: true),
            CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().BeOfType<EngineerReportSignoffResponse>()
            .Which.Approved.Should().BeTrue();
    }

    // ===== 9. ListByProject — parses status string to enum and returns 200 =====

    [Fact]
    public async Task ListByProject_ParsesStatusFilter_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var list = new List<EngineerReportResponse> { MakeReport() };
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.ListByProjectAsync(projectId, null, null, EngineerReportStatus.Submitted,
                0, 50, It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<IReadOnlyList<EngineerReportResponse>>.Ok(list));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.ListByProject(projectId, from: null, to: null,
            status: "submitted", skip: 0, take: 50, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        svc.Verify(s => s.ListByProjectAsync(projectId, null, null,
            EngineerReportStatus.Submitted, 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListByProject_InvalidStatus_ReturnsBadRequest()
    {
        var svc = new Mock<IEngineerReportService>();
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.ListByProject(Guid.NewGuid(), null, null,
            status: "not-a-real-status", skip: 0, take: 50, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ===== 10. ListPhotos — 200 with wrapped result =====

    [Fact]
    public async Task ListPhotos_ReturnsOk_WithPhotos()
    {
        var reportId = Guid.NewGuid();
        var photoList = new List<EngineerReportPhotoResponse>
        {
            new(Guid.NewGuid(), reportId, "/uploads/x.jpg", "site", DateTime.UtcNow)
        };
        var svc = new Mock<IEngineerReportService>();
        svc.Setup(s => s.ListPhotosAsync(reportId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(EngineerReportResult<ListPhotosResult>.Ok(new ListPhotosResult(photoList, 1)));
        var ctrl = NewController(svc.Object, Guid.NewGuid());

        var result = await ctrl.ListPhotos(reportId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ===== 11. EngineerReportPhotosController.Upload — happy path 201 =====

    [Fact]
    public async Task Upload_ValidFile_ReturnsCreated()
    {
        // Set up a temporary wwwroot so the controller can write the file
        var tempRoot = Path.Combine(Path.GetTempPath(), "er-engrep-" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);
        try
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.WebRootPath).Returns(tempRoot);

            var reportId = Guid.NewGuid();
            var photo = new EngineerReportPhotoResponse(
                Id: Guid.NewGuid(), ReportId: reportId, FilePath: "/uploads/.../x.jpg",
                Caption: "site A", UploadedAt: DateTime.UtcNow);
            var svc = new Mock<IEngineerReportService>();
            svc.Setup(s => s.AddPhotoAsync(It.IsAny<Guid>(), reportId, It.IsAny<string>(), "site A",
                    It.IsAny<CancellationToken>()))
               .ReturnsAsync(EngineerReportResult<EngineerReportPhotoResponse>.Ok(photo));

            // Build a minimal IFormFile from a byte array
            var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
            var formFile = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "test.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var ctrl = NewPhotoController(svc.Object, Guid.NewGuid(), env.Object);
            var result = await ctrl.Upload(reportId, formFile, "site A", CancellationToken.None);

            result.Should().BeOfType<CreatedResult>();
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* swallow */ }
        }
    }

    // ===== 12. EngineerReportPhotosController.Upload — missing file → 400 =====

    [Fact]
    public async Task Upload_MissingFile_ReturnsBadRequest()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        var svc = new Mock<IEngineerReportService>();
        var ctrl = NewPhotoController(svc.Object, Guid.NewGuid(), env.Object);

        var result = await ctrl.Upload(Guid.NewGuid(), null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
