using Microsoft.Extensions.Logging;
using Simcag.NotificationService.Application.Services;
using Simcag.NotificationService.Application.Workers;
using Simcag.Shared.Events;

namespace Simcag.NotificationService.Application.Services;

/// <summary>Resolve destinatários e envia notificação de alerta para cada um.</summary>
public sealed class AlertNotificationDispatchService
{
    private readonly IAlertNotificationRecipientResolver _recipientResolver;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AlertNotificationDispatchService> _logger;

    public AlertNotificationDispatchService(
        IAlertNotificationRecipientResolver recipientResolver,
        INotificationService notificationService,
        ILogger<AlertNotificationDispatchService> logger)
    {
        _recipientResolver = recipientResolver;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<bool> DispatchTriggeredAsync(
        AlertTriggeredEvent alert,
        Guid? envelopeTenantId,
        string? correlationId,
        CancellationToken ct)
    {
        var recipientIds = await _recipientResolver.ResolveRecipientIdsAsync(
            alert.UserId,
            alert.TenantId,
            envelopeTenantId,
            ct);

        if (recipientIds.Count == 0)
        {
            _logger.LogWarning(
                "AlertTriggeredEvent sem destinatários (alertId {AlertId}, tenant {TenantId}).",
                alert.AlertId,
                alert.TenantId ?? envelopeTenantId);
            return false;
        }

        var anySent = false;
        foreach (var userId in recipientIds)
        {
            try
            {
                var dto = AlertEventMapping.ToDto(alert, userId, correlationId);
                if (await _notificationService.SendAlertNotificationAsync(dto, ct))
                    anySent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao notificar user {UserId} para alerta {AlertId}",
                    userId,
                    alert.AlertId);
            }
        }

        _logger.LogInformation(
            "AlertTriggeredEvent {AlertId}: {RecipientCount} destinatário(s) processado(s)",
            alert.AlertId,
            recipientIds.Count);

        return anySent;
    }
}
