namespace ERPSystem.Shared.Events;

/// <summary>
/// Domain event raised when a new Invoice is created (Sprint-4.5 T-010 / DEC-057).
/// In-process only — used by Projects module to update actual cost automatically.
/// (Named InvoiceCreatedDomainEvent to avoid clash with existing IIntegrationEvent types.)
/// Phase 6.1c: TenantId removed (multi-company model).
/// </summary>
public record InvoiceCreatedDomainEvent(
    Guid InvoiceId,
    Guid? ProjectId,
    decimal Amount,
    DateTime OccurredAt) : IDomainEvent;

/// <summary>
/// Domain event raised when a Journal Entry is posted (Sprint-4.5 T-010 / DEC-057).
/// In-process only — used by Reports module for cache invalidation.
/// Phase 6.1c: TenantId removed.
/// </summary>
public record JournalEntryPostedDomainEvent(
    Guid JournalEntryId,
    string Reference,
    decimal TotalDebit,
    decimal TotalCredit,
    DateTime OccurredAt) : IDomainEvent;

/// <summary>
/// Domain event raised when a Project's actual cost changes (Sprint-4.5 T-010 / DEC-057).
/// In-process only — used by Reports module to refresh project cost report.
/// Phase 6.1c: TenantId removed.
/// </summary>
public record ProjectCostUpdatedDomainEvent(
    Guid ProjectId,
    decimal NewActualCost,
    DateTime OccurredAt) : IDomainEvent;
