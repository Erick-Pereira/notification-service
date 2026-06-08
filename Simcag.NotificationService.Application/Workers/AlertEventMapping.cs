using Simcag.NotificationService.Application.DTOs;
using Simcag.Shared.Events;

namespace Simcag.NotificationService.Application.Workers;

public static class AlertEventMapping
{
    public static AlertNotificationDto ToDto(
        AlertTriggeredEvent e,
        Guid userId,
        string? correlationId = null) =>
        new()
        {
            UserId = userId,
            AlertId = e.AlertId,
            ProductName = e.ProductName,
            ProductId = e.ProductId,
            AlertType = e.AlertType,
            AlertCategory = e.AlertCategory,
            Message = e.Message,
            Severity = e.Severity,
            TenantId = e.TenantId,
            CurrentPrice = e.CurrentPrice,
            PriceChange = NormalizeDeviation(e.DeviationPercentage),
            Source = e.Source,
            OccurredAt = e.OccurredAt,
            CorrelationId = correlationId,
        };

    private static decimal NormalizeDeviation(decimal deviation)
    {
        if (deviation is > 1m or < -1m)
            return deviation / 100m;
        return deviation;
    }
}
