using System;

namespace ERPSystem.Modules.Projects.Application.Dtos;

// =============================================================================
// Sprint 64 / DEC-223 — Sub-ProgressBilling DTOs (مستخلصات مقاولي الباطن).
//
// Wire-format for the Sub-ProgressBilling API. The service layer maps between
// the persistence entity (Modules/Projects/Entities/SubProgressBilling.cs) and
// these DTOs at the controller boundary. CompanyId is intentionally NOT in the
// requests — the service resolves it from the JWT context (L19 / DEC-095).
// =============================================================================

/// <summary>
/// Body for POST /api/sub-contracts/{subContractId}/billings.
/// L19: <c>CompanyId</c> is intentionally NOT in the request.
/// <c>BillingNumber</c> is supplied by the client (e.g. "B-001") and is unique
/// within the sub-contract. <c>WorkCompletedPercent</c> is the CUMULATIVE
/// percent — the service computes gross/retention/net on top of it.
/// </summary>
public record CreateSubProgressBillingRequest(
    string BillingNumber,
    DateTime BillingDate,
    DateTime? PeriodFrom,
    DateTime? PeriodTo,
    decimal WorkCompletedPercent,
    string? Notes
);

/// <summary>
/// Body for PUT /api/sub-progress-billings/{id} (Draft only).
/// Re-uses the same algorithm: gross/retention/net are recomputed on Update.
/// </summary>
public record UpdateSubProgressBillingRequest(
    DateTime? PeriodFrom,
    DateTime? PeriodTo,
    decimal WorkCompletedPercent,
    string? Notes
);

/// <summary>
/// Read DTO returned by the SubProgressBilling endpoints.
/// <c>StatusName</c> is the Arabic display label for <c>Status</c>
/// (1=مسودة, 2=معتمد, 3=مدفوع, 4=ملغى).
/// </summary>
public record SubProgressBillingResponse(
    Guid Id,
    Guid CompanyId,
    Guid SubContractId,
    string BillingNumber,
    DateTime BillingDate,
    DateTime? PeriodFrom,
    DateTime? PeriodTo,
    decimal WorkCompletedPercent,
    decimal GrossAmount,
    decimal RetentionDeducted,
    decimal PreviousBillingsAmount,
    decimal NetPayable,
    int Status,
    string StatusName,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Aggregated outstanding balance for a sub-contract.
/// Returned by GET /api/sub-contracts/{id}/balance.
///
/// <para><b>Formula</b>:</para>
/// <code>
/// totalBilledGross       = SUM(sub_progress_billings.gross_amount       WHERE status != 4)
/// totalRetentionWithheld = SUM(sub_progress_billings.retention_deducted WHERE status != 4)
/// totalPaid              = SUM(sub_payments.amount + sub_payments.retention_released)
/// outstandingBalance     = totalBilledGross - totalPaid
/// </code>
/// </summary>
public record SubContractBalanceResponse(
    Guid SubContractId,
    string ContractNumber,
    decimal ContractValue,
    decimal TotalBilledGross,
    decimal TotalRetentionWithheld,
    decimal TotalPaid,
    decimal OutstandingBalance
);
