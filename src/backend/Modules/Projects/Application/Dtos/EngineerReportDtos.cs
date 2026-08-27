using System;
using System.Collections.Generic;

namespace ERPSystem.Modules.Projects.Application.Dtos;

// =============================================================================
// Sprint 61 (DEC-192, DEC-193, DEC-194) — Engineer's Daily Report DTOs.
// These DTOs are the wire-format for Wave 2A (Repositories + Services + Controllers).
// The service layer maps between the persistence entity (Modules/Projects/Entities/)
// and these DTOs at the controller boundary.
// =============================================================================

/// <summary>
/// Read DTO returned by GET /api/projects/{id}/engineer-reports and
/// GET /api/engineer-reports/{id}.
/// </summary>
public record EngineerReportResponse(
    Guid Id,
    Guid ProjectId,
    DateTime ReportDate,
    Guid EngineerId,
    string Status,        // "Draft" | "Submitted" | "Approved" | "Rejected"
    string? Weather,
    string WorkDone,
    string? Issues,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int PhotosCount,
    IReadOnlyList<EngineerReportPhotoResponse> Photos,
    IReadOnlyList<EngineerReportSignoffResponse> Signoffs
);

/// <summary>
/// Read DTO returned by GET /api/engineer-reports/{id}/photos.
/// </summary>
public record EngineerReportPhotoResponse(
    Guid Id,
    Guid ReportId,
    string FilePath,
    string? Caption,
    DateTime UploadedAt
);

/// <summary>
/// Read DTO embedded in EngineerReportResponse for the signoff history.
/// </summary>
public record EngineerReportSignoffResponse(
    Guid Id,
    Guid ReportId,
    Guid SignerId,
    string SignerRole,    // "PM" | "Client" | "Engineer"
    DateTime SignedAt,
    string? SignatureText,
    string? Comment,
    bool Approved
);

/// <summary>
/// Body for POST /api/projects/{id}/engineer-reports (create a new draft report).
/// <c>CompanyId</c> is intentionally NOT in the request — the service resolves
/// it from the JWT context (L19 / L29 / L30).
/// </summary>
public record CreateEngineerReportRequest(
    DateTime ReportDate,
    Guid EngineerId,
    string? Weather,
    string WorkDone,
    string? Issues
);

/// <summary>
/// Body for PUT /api/engineer-reports/{id} (update a draft report).
/// Only editable while status == "Draft"; service returns 400 otherwise.
/// </summary>
public record UpdateEngineerReportRequest(
    string? Weather,
    string WorkDone,
    string? Issues
);

/// <summary>
/// Body for POST /api/engineer-reports/{id}/signoff (PM or Client approves / rejects).
/// <c>Approved</c> = true → report status becomes "Approved".
/// <c>Approved</c> = false → report status becomes "Rejected".
/// </summary>
public record SignoffRequest(
    string SignerRole,        // "PM" | "Client" | "Engineer"
    string? SignatureText,
    string? Comment,
    bool Approved
);
