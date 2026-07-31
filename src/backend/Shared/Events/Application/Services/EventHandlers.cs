using Dapper;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Shared.Events.Application.Services;

public interface IProcessedEventsRepository
{
    Task<bool> IsProcessedAsync(Guid eventId, CancellationToken ct);
    /// <summary>
    /// Mark event as processed for idempotency. Phase 6.1b: dedup is keyed on event_id only
    /// (multi-company model — no tenant partitioning in processed_events).
    /// </summary>
    Task MarkProcessedAsync(Guid eventId, CancellationToken ct);
}

public sealed class ProcessedEventsRepository : IProcessedEventsRepository
{
    private readonly IDbConnectionFactory _db;
    public ProcessedEventsRepository(IDbConnectionFactory db) => _db = db;

    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var hit = await conn.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(
            "SELECT 1 FROM processed_events WHERE event_id = @EventId LIMIT 1",
            new { EventId = eventId }, cancellationToken: ct));
        return hit.HasValue;
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken ct)
    {
        // Phase 6.1b: processed_events table has company_id (nullable) but dedup is event_id only.
        // We omit company_id from the INSERT — the publisher's company is irrelevant to dedup.
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO processed_events (event_id, processed_at) VALUES (@EventId, @At)",
            new { EventId = eventId, At = DateTime.UtcNow }, cancellationToken: ct));
    }
}

/// <summary>Handler contract — one per event type. Discovered via DI.</summary>
public interface IIntegrationEventHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T @event, CancellationToken ct);
}
