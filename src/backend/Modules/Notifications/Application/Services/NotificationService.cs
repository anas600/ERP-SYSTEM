using ERPSystem.Modules.Notifications.Entities;
using ERPSystem.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Notifications.Application.Services;

public interface INotificationService
{
    Task CreateAsync(Guid userId, string type, string title, string message, string? referenceType = null, Guid? referenceId = null);
    Task<IReadOnlyList<Notification>> ListAsync(Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct);
    Task MarkReadAsync(Guid userId, Guid id, CancellationToken ct);
}

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly ILogger<NotificationService> _logger;
    public NotificationService(INotificationRepository repo, ILogger<NotificationService> logger)
    {
        _repo = repo; _logger = logger;
    }

    public async Task CreateAsync(Guid userId, string type, string title, string message, string? referenceType = null, Guid? referenceId = null)
    {
        var n = new Notification
        {
            Id = Guid.NewGuid(), UserId = userId,
            Type = type, Title = title, Message = message,
            ReferenceType = referenceType, ReferenceId = referenceId,
            IsRead = false, CreatedAt = DateTime.UtcNow
        };
        await _repo.InsertAsync(n, CancellationToken.None);
        _logger.LogInformation("Created notification {Type} for user {UserId}", type, userId);
    }

    public async Task<IReadOnlyList<Notification>> ListAsync(Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        return await _repo.ListAsync(userId, unreadOnly, skip, take, ct);
    }

    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken ct) =>
        await _repo.CountUnreadAsync(userId, ct);

    public async Task MarkReadAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var n = await _repo.GetByIdAsync(id, ct);
        if (n == null || n.UserId != userId) return;
        await _repo.MarkReadAsync(id, DateTime.UtcNow, ct);
    }
}
