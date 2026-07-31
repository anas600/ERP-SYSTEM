using System.Text.Json;
using ERPSystem.Shared.Events.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Shared.Events.Application.Services;

public interface IEventBus
{
    /// <summary>
    /// Persists the event to outbox_events in the current DbContext/transaction
    /// (so the publish is atomic with the business operation).
    /// </summary>
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IIntegrationEvent;
}

public sealed class EventBus : IEventBus
{
    private readonly IOutboxRepository _outbox;
    private readonly ILogger<EventBus> _logger;
    public EventBus(IOutboxRepository outbox, ILogger<EventBus> logger)
    {
        _outbox = outbox; _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IIntegrationEvent
    {
        var type = typeof(T);
        // record types: use the clean name (StockReceivedEvent, etc.)
        var eventType = type.Name;
        var aggregateType = type.Name.Replace("Event", "");  // "StockReceived", "StockIssued", "JournalEntryPosted" -> ...
        // For more accurate aggregate_type, hardcode below in handlers — but this is good enough
        var aggregateType2 = AggregateTypeOf(@event);

        var payload = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Phase 6.1c: multi-company model. OutboxEvent.CompanyId is the publisher's
        // company (set by handlers that have ICompanyContext). Events no longer carry
        // TenantId — CompanyId is resolved at the event handler boundary via the
        // ICompanyContext populated by CompanyContextMiddleware from the request's
        // X-Company-Id header. For now we use Guid.Empty as a placeholder; the handler
        // can set it explicitly if needed (most don't need it — they use the receiver's
        // ICompanyContext at apply time).
        var row = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.Empty,
            EventType = eventType,
            AggregateId = @event.EventId,  // EventId is the dedup key; but AggregateId is for routing
            AggregateType = aggregateType2,
            Payload = payload,
            OccurredAt = @event.OccurredAt,
            ProcessedAt = null,
            RetryCount = 0,
            MaxRetries = 3
        };
        await _outbox.InsertAsync(row, ct);
        _logger.LogInformation("Published {EventType} (EventId={EventId})", eventType, @event.EventId);
    }

    private static string AggregateTypeOf(IIntegrationEvent evt) => evt switch
    {
        StockReceivedEvent => "StockMovement",
        StockIssuedEvent => "StockMovement",
        JournalEntryPostedEvent => "JournalEntry",
        _ => "Unknown"
    };
}
