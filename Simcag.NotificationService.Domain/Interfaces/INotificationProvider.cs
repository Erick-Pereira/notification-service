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
}

public interface INotificationPreferenceRepository
{
    Task<Domain.Entities.NotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Domain.Entities.NotificationPreference preference, CancellationToken ct);
    Task UpdateAsync(Domain.Entities.NotificationPreference preference, CancellationToken ct);
}