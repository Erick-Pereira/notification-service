namespace Simcag.NotificationService.Domain.Interfaces;

public interface IEmailProvider
{
    Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public interface ISmsProvider
{
    Task<bool> SendAsync(string to, string message, CancellationToken ct = default);
}

public interface INotificationRepository
{
    Task AddAsync(Domain.Entities.Notification notification, CancellationToken ct);
    Task<Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<Domain.Entities.Notification>> GetByUserIdAsync(Guid userId, int limit, CancellationToken ct);
    Task<IEnumerable<Domain.Entities.Notification>> GetPendingAsync(int limit, CancellationToken ct);
    Task UpdateAsync(Domain.Entities.Notification notification, CancellationToken ct);

    Task<(IReadOnlyList<Domain.Entities.Notification> Items, int Total)> GetDeliveriesPageAsync(
        Guid userId,
        string? status,
        string? channel,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<IReadOnlyDictionary<string, int>> CountByStatusForUserAsync(Guid userId, CancellationToken ct);
}

public interface INotificationPreferenceRepository
{
    Task<Domain.Entities.NotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Domain.Entities.NotificationPreference preference, CancellationToken ct);
    Task UpdateAsync(Domain.Entities.NotificationPreference preference, CancellationToken ct);
}