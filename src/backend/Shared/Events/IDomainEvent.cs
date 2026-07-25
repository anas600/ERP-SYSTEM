namespace ERPSystem.Shared.Events;

/// <summary>
/// In-process domain event contract (Sprint-4.5 T-010 / DEC-057).
///
/// Phase 6.1c: Multi-Company model. <c>TenantId</c> is REMOVED — the company
/// context is provided by the HTTP request via <c>ICompanyContext</c> +
/// <c>X-Company-Id</c> header.
///
/// الفرق عن <see cref="IIntegrationEvent"/>:
/// - IIntegrationEvent: outbox + cross-process (heavyweight, async, retried)
/// - IDomainEvent:       in-process only (lightweight, sync, fire-and-forget)
/// </summary>
public interface IDomainEvent
{
    /// <summary>وقت حدوث الـ event</summary>
    DateTime OccurredAt { get; }
}
