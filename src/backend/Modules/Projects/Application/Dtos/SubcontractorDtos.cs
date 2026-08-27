using System;
using System.Collections.Generic;

namespace ERPSystem.Modules.Projects.Application.Dtos;

// =============================================================================
// Sprint 64 / DEC-221 + DEC-222 — Subcontractor & Sub-Contract DTOs.
//
// Wire-format for the Subcontractor + Sub-Contract APIs. The service layer maps
// between the persistence entity (Modules/Projects/Entities/) and these DTOs at
// the controller boundary. CompanyId is intentionally NOT in the requests —
// the service resolves it from the JWT context (L19 / DEC-095).
// =============================================================================

/// <summary>
/// Body for POST /api/subcontractors.
/// L19: <c>CompanyId</c> is intentionally NOT in the request.
/// </summary>
public record CreateSubcontractorRequest(
    string Code,
    string Name,
    string? NameAr,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? TradeSpecialty,
    string? TaxId
);

/// <summary>
/// Body for PUT /api/subcontractors/{id}. All fields required.
/// </summary>
public record UpdateSubcontractorRequest(
    string Name,
    string? NameAr,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? TradeSpecialty,
    string? TaxId,
    bool IsActive
);

/// <summary>
/// Read DTO returned by the Subcontractor endpoints.
/// </summary>
public record SubcontractorResponse(
    Guid Id,
    Guid CompanyId,
    string Code,
    string Name,
    string? NameAr,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? TradeSpecialty,
    string? TaxId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// -----------------------------------------------------------------------------
// SubContract DTOs
// -----------------------------------------------------------------------------

/// <summary>
/// Body for POST /api/projects/{projectId}/sub-contracts.
/// </summary>
public record CreateSubContractRequest(
    Guid SubcontractorId,
    string ContractNumber,
    string ScopeOfWork,
    decimal ContractValue,
    decimal RetentionPercent,
    int RetentionReleaseBilling,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Notes
);

/// <summary>
/// Body for PUT /api/sub-contracts/{id}. Status is included so the controller
/// can transition Active ↔ Completed ↔ Cancelled.
/// </summary>
public record UpdateSubContractRequest(
    string ScopeOfWork,
    decimal ContractValue,
    decimal RetentionPercent,
    int RetentionReleaseBilling,
    DateTime? StartDate,
    DateTime? EndDate,
    int Status,
    string? Notes
);

/// <summary>
/// Read DTO returned by the Sub-Contract endpoints. <c>StatusName</c> is the
/// Arabic display label for <c>Status</c> (1=نشط, 2=مكتمل, 3=ملغى).
/// </summary>
public record SubContractResponse(
    Guid Id,
    Guid CompanyId,
    Guid ProjectId,
    Guid SubcontractorId,
    string ContractNumber,
    string ScopeOfWork,
    decimal ContractValue,
    decimal RetentionPercent,
    int RetentionReleaseBilling,
    DateTime? StartDate,
    DateTime? EndDate,
    int Status,
    string StatusName,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
