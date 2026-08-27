using System;

namespace ERPSystem.Modules.Projects.Application.Dtos;

// =============================================================================
// Sprint 64 / DEC-224 — Sub-Payment DTOs (مدفوعات مقاولي الباطن).
//
// Wire-format for the Sub-Payment API. The service layer maps between the
// persistence entity (Modules/Projects/Entities/SubPayment.cs) and these DTOs
// at the controller boundary. CompanyId is intentionally NOT in the requests —
// the service resolves it from the JWT context (L19 / DEC-095).
// =============================================================================

/// <summary>
/// Body for POST /api/sub-contracts/{subContractId}/billings/{billingId}/payments.
/// Regular payment against an approved billing. <c>RetentionReleased</c> is
/// always 0 here — the release flow uses <see cref="ReleaseRetentionRequest"/>.
/// </summary>
public record CreateSubPaymentRequest(
    string PaymentNumber,
    DateTime PaymentDate,
    decimal Amount,
    string? PaymentMethod,
    string? ReferenceNumber,
    string? Notes
);

/// <summary>
/// Read DTO returned by the SubPayment endpoints.
/// </summary>
public record SubPaymentResponse(
    Guid Id,
    Guid CompanyId,
    Guid SubContractId,
    Guid SubProgressBillingId,
    string PaymentNumber,
    DateTime PaymentDate,
    decimal Amount,
    decimal RetentionReleased,
    string? PaymentMethod,
    string? ReferenceNumber,
    string? Notes,
    DateTime CreatedAt
);

/// <summary>
/// Body for POST /api/sub-contracts/{subContractId}/release-retention.
/// The service creates a new SubPayment with <c>retention_released = Amount</c>
/// and validates that the amount does not exceed the currently-withheld
/// retention (i.e. <c>totalWithheld - totalAlreadyReleased</c>).
/// </summary>
public record ReleaseRetentionRequest(
    DateTime ReleaseDate,
    decimal Amount,
    string? Notes
);
