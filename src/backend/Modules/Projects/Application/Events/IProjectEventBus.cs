using System.Collections.Concurrent;

namespace ERPSystem.Modules.Projects.Application.Events;

/// <summary>
/// Sprint 65 / DEC-231+232 (Wave 1A): Lightweight in-process pub/sub for cross-module auto-triggers.
///
/// <para><b>Why in-process:</b> Sprint 22 removed the heavy cross-module event bus (MassTransit-style
/// with outbox + retry). For Wave 1A's "auto-JE" use case, we only need a single-process pub/sub
/// that fires handlers in the same transaction/request as the originating service.</para>
///
/// <para><b>What it is NOT:</b></para>
/// <list type="bullet">
///   <item>NOT a distributed message bus</item>
///   <item>NOT persisted (no outbox)</item>
///   <item>NOT retried (fire-and-forget)</item>
///   <item>NOT transactional with the handler (handlers run in their own scope)</item>
/// </list>
///
/// <para><b>Pattern:</b> services call <c>PublishAsync</c> at the end of a business operation.
/// Handlers subscribed via <c>Subscribe</c> are invoked sequentially. Handlers must be idempotent
/// (safe to re-fire) because there's no delivery guarantee.</para>
/// </summary>
public interface IProjectEventBus
{
    /// <summary>Publish an event to all subscribed handlers of type <typeparamref name="TEvent"/>.</summary>
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct) where TEvent : class;

    /// <summary>Register a handler for events of type <typeparamref name="TEvent"/>.</summary>
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class;
}

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IProjectEventBus"/>. Singleton lifetime.
/// </summary>
public sealed class ProjectEventBus : IProjectEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class
    {
        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (list)
        {
            list.Add(handler);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct) where TEvent : class
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;

        Delegate[] snapshot;
        lock (handlers)
        {
            snapshot = handlers.ToArray();
        }

        foreach (var del in snapshot)
        {
            var typed = (Func<TEvent, CancellationToken, Task>)del;
            // Handlers run sequentially; a failure in one stops the chain so callers can see it
            // in the log. No retry, no outbox — fire-and-forget semantics.
            await typed(evt, ct).ConfigureAwait(false);
        }
    }
}
