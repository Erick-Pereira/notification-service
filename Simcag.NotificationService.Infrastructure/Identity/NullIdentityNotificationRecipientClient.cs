using Simcag.NotificationService.Application.Abstractions;

namespace Simcag.NotificationService.Infrastructure.Identity;

/// <summary>Ambiente Testing — sem RabbitMQ.</summary>
public sealed class NullIdentityNotificationRecipientClient : IIdentityNotificationRecipientClient
{
    public Task<IReadOnlyList<Guid>> GetGovernanceRecipientUserIdsAsync(Guid tenantId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
}
