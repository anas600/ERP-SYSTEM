using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

/// <summary>
/// Sprint 61 — Result envelope for engineer-report service calls. Mirrors the
/// pattern used by <see cref="ProjectResult{T}"/> / <c>CostCenterResult</c> so the
/// controller can map Succeeded/Error uniformly.
/// </summary>
public sealed class EngineerReportResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public EngineerReportErrorCode? ErrorCode { get; init; }
    public static EngineerReportResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static EngineerReportResult<T> Fail(string e, EngineerReportErrorCode c) => new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum EngineerReportErrorCode
{
    NotFound, AlreadyExists, ValidationError, InvalidStatusTransition, Internal
}

public interface IEngineerReportService
{
    Task<EngineerReportResult<EngineerReportResponse>> CreateAsync(
        Guid userId, Guid projectId, CreateEngineerReportRequest req, CancellationToken ct);

    Task<EngineerReportResult<EngineerReportResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateEngineerReportRequest req, CancellationToken ct);

    Task<EngineerReportResult<EngineerReportResponse>> GetByIdAsync(Guid id, CancellationToken ct);

    Task<EngineerReportResult<IReadOnlyList<EngineerReportResponse>>> ListByProjectAsync(
        Guid projectId, DateTime? from, DateTime? to, EngineerReportStatus? status,
        int skip, int take, CancellationToken ct);

    Task<EngineerReportResult<EngineerReportResponse>> SubmitAsync(
        Guid userId, Guid id, CancellationToken ct);

    Task<EngineerReportResult<EngineerReportSignoffResponse>> SignoffAsync(
        Guid userId, Guid id, SignoffRequest req, CancellationToken ct);

    Task<EngineerReportResult<ListPhotosResult>> ListPhotosAsync(Guid id, CancellationToken ct);

    Task<EngineerReportResult<EngineerReportPhotoResponse>> AddPhotoAsync(
        Guid userId, Guid id, string filePath, string? caption, CancellationToken ct);
}

/// <summary>Wrapper for the list-photos endpoint so the service returns a typed value.</summary>
public sealed record ListPhotosResult(IReadOnlyList<EngineerReportPhotoResponse> Photos, int Count);

/// <summary>
/// Sprint 61 (DEC-192, DEC-193, DEC-194) — Engineer's Daily Report service.
///
/// <para><b>State machine (DEC-194)</b>:
/// <c>Draft → Submitted → Approved</c> or <c>Draft → Submitted → Rejected → (revise) → Draft → …</c>
/// </para>
///
/// <para><b>L19 / DEC-095</b>: the service resolves <c>CompanyId</c> from
/// <see cref="ICompanyContext"/>, never from the request DTO. Even the photo upload
/// uses the parent report's <c>CompanyId</c> (denormalized for the photo row).</para>
///
/// <para><b>Photo storage</b>: the controller writes the file to disk and passes the
/// relative <c>file_path</c> to the service. The service is responsible for the
/// database row only.</para>
/// </summary>
public sealed class EngineerReportService : IEngineerReportService
{
    private readonly IEngineerReportRepository _reports;
    private readonly IEngineerReportPhotoRepository _photos;
    private readonly IEngineerReportSignoffRepository _signoffs;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<EngineerReportService> _logger;

    // Allowed signer roles (mirrors the DB schema: 'PM' | 'Client' | 'Engineer').
    private static readonly HashSet<string> AllowedSignerRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "PM", "Client", "Engineer"
    };

    public EngineerReportService(
        IEngineerReportRepository reports,
        IEngineerReportPhotoRepository photos,
        IEngineerReportSignoffRepository signoffs,
        ICompanyContext companyContext,
        ILogger<EngineerReportService> logger)
    {
        _reports = reports; _photos = photos; _signoffs = signoffs;
        _companyContext = companyContext; _logger = logger;
    }

    public async Task<EngineerReportResult<EngineerReportResponse>> CreateAsync(
        Guid userId, Guid projectId, CreateEngineerReportRequest req, CancellationToken ct)
    {
        // L19 / DEC-095: CompanyId comes from JWT, not from request.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        if (string.IsNullOrWhiteSpace(req.WorkDone))
            return EngineerReportResult<EngineerReportResponse>.Fail(
                "حقل work_done مطلوب.", EngineerReportErrorCode.ValidationError);

        // DEC-192: UNIQUE (project_id, report_date) — one report per project per day.
        var existing = await _reports.CountByProjectAndDateAsync(projectId, req.ReportDate, ct);
        if (existing > 0)
            return EngineerReportResult<EngineerReportResponse>.Fail(
                "يوجد تقرير آخر لهذا المشروع في نفس التاريخ.", EngineerReportErrorCode.AlreadyExists);

        var now = DateTime.UtcNow;
        var report = new EngineerReport
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProjectId = projectId,
            ReportDate = req.ReportDate.Date,
            EngineerId = userId, // L19 / DEC-095: from JWT (matches authenticated engineer)
            Status = EngineerReportStatus.Draft,
            Weather = req.Weather,
            WorkDone = req.WorkDone.Trim(),
            Issues = req.Issues,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _reports.InsertAsync(report, ct);
        _logger.LogInformation("EngineerReport created {ReportId} for project {ProjectId} on {Date}",
            report.Id, projectId, report.ReportDate);
        return EngineerReportResult<EngineerReportResponse>.Ok(await MapToResponseAsync(report, ct));
    }

    public async Task<EngineerReportResult<EngineerReportResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateEngineerReportRequest req, CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(id, ct);
        if (report == null)
            return EngineerReportResult<EngineerReportResponse>.Fail(
                "غير موجود.", EngineerReportErrorCode.NotFound);

        // Only Draft is editable; Submitted/Approved/Rejected → 400.
        if (report.Status != EngineerReportStatus.Draft)
            return EngineerReportResult<EngineerReportResponse>.Fail(
                "لا يمكن تعديل تقرير تم تقديمه.", EngineerReportErrorCode.InvalidStatusTransition);

        if (string.IsNullOrWhiteSpace(req.WorkDone))
            return EngineerReportResult<EngineerReportResponse>.Fail(
                "حقل work_done مطلوب.", EngineerReportErrorCode.ValidationError);

        report.Weather = req.Weather;
        report.WorkDone = req.WorkDone.Trim();
        report.Issues = req.Issues;
        report.UpdatedAt = DateTime.UtcNow;
        await _reports.UpdateAsync(report, ct);
        return EngineerReportResult<EngineerReportResponse>.Ok(await MapToResponseAsync(report, ct));
    }

    public async Task<EngineerReportResult<EngineerReportResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var r = await _reports.GetByIdAsync(id, ct);
        if (r == null)
            return EngineerReportResult<EngineerReportResponse>.Fail(
                "غير موجود.", EngineerReportErrorCode.NotFound);
        return EngineerReportResult<EngineerReportResponse>.Ok(await MapToResponseAsync(r, ct));
    }

    public async Task<EngineerReportResult<IReadOnlyList<EngineerReportResponse>>> ListByProjectAsync(
        Guid projectId, DateTime? from, DateTime? to, EngineerReportStatus? status,
        int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        // L19: company scope is enforced inside the repo via the companyId param.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");
        var rows = await _reports.ListByProjectAsync(projectId, companyId, from, to, status, skip, take, ct);
        var list = new List<EngineerReportResponse>(rows.Count);
        foreach (var r in rows) list.Add(await MapToResponseAsync(r, ct));
        return EngineerReportResult<IReadOnlyList<EngineerReportResponse>>.Ok(list);
    }

    public async Task<EngineerReportResult<EngineerReportResponse>> SubmitAsync(
        Guid userId, Guid id, CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(id, ct);
        if (report == null)
            return EngineerReportResult<EngineerReportResponse>.Fail(
                "غير موجود.", EngineerReportErrorCode.NotFound);

        // State machine: only Draft can be submitted.
        if (report.Status != EngineerReportStatus.Draft)
            return EngineerReportResult<EngineerReportResponse>.Fail(
                $"لا يمكن تقديم تقرير في الحالة {report.Status}.",
                EngineerReportErrorCode.InvalidStatusTransition);

        report.Status = EngineerReportStatus.Submitted;
        report.UpdatedAt = DateTime.UtcNow;
        await _reports.UpdateAsync(report, ct);
        _logger.LogInformation("EngineerReport {ReportId} submitted", report.Id);
        return EngineerReportResult<EngineerReportResponse>.Ok(await MapToResponseAsync(report, ct));
    }

    public async Task<EngineerReportResult<EngineerReportSignoffResponse>> SignoffAsync(
        Guid userId, Guid id, SignoffRequest req, CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(id, ct);
        if (report == null)
            return EngineerReportResult<EngineerReportSignoffResponse>.Fail(
                "غير موجود.", EngineerReportErrorCode.NotFound);

        if (report.Status != EngineerReportStatus.Submitted)
            return EngineerReportResult<EngineerReportSignoffResponse>.Fail(
                $"لا يمكن اعتماد تقرير في الحالة {report.Status}.",
                EngineerReportErrorCode.InvalidStatusTransition);

        if (string.IsNullOrWhiteSpace(req.SignerRole) || !AllowedSignerRoles.Contains(req.SignerRole))
            return EngineerReportResult<EngineerReportSignoffResponse>.Fail(
                "signer_role غير صالح. القيم المسموحة: PM, Client, Engineer.",
                EngineerReportErrorCode.ValidationError);

        var signoff = new EngineerReportSignoff
        {
            Id = Guid.NewGuid(),
            CompanyId = report.CompanyId,
            ReportId = report.Id,
            SignerId = userId,
            SignerRole = req.SignerRole,
            SignedAt = DateTime.UtcNow,
            SignatureText = req.SignatureText,
            Comment = req.Comment,
            Approved = req.Approved
        };
        await _signoffs.InsertAsync(signoff, ct);

        // State machine: Approved → Approved; Rejected → Rejected.
        report.Status = req.Approved ? EngineerReportStatus.Approved : EngineerReportStatus.Rejected;
        report.UpdatedAt = DateTime.UtcNow;
        await _reports.UpdateAsync(report, ct);

        _logger.LogInformation("EngineerReport {ReportId} signoff: approved={Approved} by {Role}",
            report.Id, req.Approved, req.SignerRole);

        return EngineerReportResult<EngineerReportSignoffResponse>.Ok(new EngineerReportSignoffResponse(
            signoff.Id, signoff.ReportId, signoff.SignerId, signoff.SignerRole,
            signoff.SignedAt, signoff.SignatureText, signoff.Comment, signoff.Approved));
    }

    public async Task<EngineerReportResult<ListPhotosResult>> ListPhotosAsync(Guid id, CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(id, ct);
        if (report == null)
            return EngineerReportResult<ListPhotosResult>.Fail(
                "غير موجود.", EngineerReportErrorCode.NotFound);
        var photos = await _photos.ListByReportAsync(id, ct);
        var list = photos.Select(p => new EngineerReportPhotoResponse(
            p.Id, p.ReportId, p.FilePath, p.Caption, p.UploadedAt)).ToList();
        return EngineerReportResult<ListPhotosResult>.Ok(new ListPhotosResult(list, list.Count));
    }

    public async Task<EngineerReportResult<EngineerReportPhotoResponse>> AddPhotoAsync(
        Guid userId, Guid id, string filePath, string? caption, CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(id, ct);
        if (report == null)
            return EngineerReportResult<EngineerReportPhotoResponse>.Fail(
                "غير موجود.", EngineerReportErrorCode.NotFound);

        var photo = new EngineerReportPhoto
        {
            Id = Guid.NewGuid(),
            CompanyId = report.CompanyId, // denormalized from parent report (L19 / DEC-095)
            ReportId = report.Id,
            FilePath = filePath,
            Caption = caption,
            UploadedAt = DateTime.UtcNow
        };
        await _photos.InsertAsync(photo, ct);
        return EngineerReportResult<EngineerReportPhotoResponse>.Ok(new EngineerReportPhotoResponse(
            photo.Id, photo.ReportId, photo.FilePath, photo.Caption, photo.UploadedAt));
    }

    // ===== mapping helpers =====

    private async Task<EngineerReportResponse> MapToResponseAsync(EngineerReport r, CancellationToken ct)
    {
        var photos = await _photos.ListByReportAsync(r.Id, ct);
        var signoffs = await _signoffs.ListByReportAsync(r.Id, ct);
        return new EngineerReportResponse(
            r.Id, r.ProjectId, r.ReportDate, r.EngineerId, r.Status.ToString(),
            r.Weather, r.WorkDone, r.Issues, r.CreatedAt, r.UpdatedAt,
            photos.Count,
            photos.Select(p => new EngineerReportPhotoResponse(
                p.Id, p.ReportId, p.FilePath, p.Caption, p.UploadedAt)).ToList(),
            signoffs.Select(s => new EngineerReportSignoffResponse(
                s.Id, s.ReportId, s.SignerId, s.SignerRole, s.SignedAt,
                s.SignatureText, s.Comment, s.Approved)).ToList());
    }
}
