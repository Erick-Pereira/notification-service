using Microsoft.Extensions.Logging;
using Simcag.NotificationService.Application.Abstractions;
using Simcag.NotificationService.Application.Configuration;
using Simcag.Shared.Events;

namespace Simcag.NotificationService.Application.Services;

public interface IAlertNotificationRecipientResolver
{
    Task<IReadOnlyList<Guid>> ResolveRecipientIdsAsync(
        Guid? eventUserId,
        Guid? eventTenantId,
        Guid? envelopeTenantId,
        CancellationToken ct);
}

public sealed class AlertNotificationRecipientResolver : IAlertNotificationRecipientResolver
{
    private readonly NotificationRecipientOptions _options;
    private readonly IIdentityNotificationRecipientClient _identityClient;
    private readonly ILogger<AlertNotificationRecipientResolver> _logger;

    public AlertNotificationRecipientResolver(
        NotificationRecipientOptions options,
        IIdentityNotificationRecipientClient identityClient,
        ILogger<AlertNotificationRecipientResolver> logger)
    {
        _options = options;
        _identityClient = identityClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> ResolveRecipientIdsAsync(
        Guid? eventUserId,
        Guid? eventTenantId,
        Guid? envelopeTenantId,
        CancellationToken ct)
    {
        var ids = new HashSet<Guid>();
        var tenantId = eventTenantId ?? envelopeTenantId;

        if (tenantId is { } t && t != Guid.Empty)
        {
            try
            {
                foreach (var id in await _identityClient.GetGovernanceRecipientUserIdsAsync(t, ct))
                    ids.Add(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao obter destinatários do identity para tenant {TenantId}", t);
            }
        }

        if (eventUserId is { } uploader && uploader != Guid.Empty)
            ids.Add(uploader);

        if (ids.Count == 0 && _options.DefaultNotifyUserId is { } fallback && fallback != Guid.Empty)
            ids.Add(fallback);

        return ids.ToList();
    }
}
