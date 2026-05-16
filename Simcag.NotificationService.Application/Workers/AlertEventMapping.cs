using Simcag.NotificationService.Application.DTOs;
using Simcag.Shared.Events;

namespace Simcag.NotificationService.Application.Workers;

public static class AlertEventMapping
{
    public static AlertNotificationDto ToDto(AlertTriggeredEvent e, string? correlationId = null) =>
        new()
        {
            UserId = e.UserId ?? Guid.Empty,
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

    public static AlertNotificationDto ToDto(AlertCreatedEvent e, Guid? explicitUserId = null, string? correlationId = null)
    {
        var userId = explicitUserId ?? e.UserId;
        return new()
        {
            UserId = userId ?? Guid.Empty,
            AlertId = e.AlertId.ToString("D"),
            ProductName = e.ProductName,
            ProductId = e.ProductId,
            AlertType = e.AlertType,
            Message = e.Message,
            Severity = null,
            TenantId = e.TenantId,
            CurrentPrice = e.CurrentPrice ?? 0,
            PriceChange = NormalizePriceVariation(e.PriceVariation),
            Source = e.Source ?? "Unknown",
            OccurredAt = e.OccurredAt,
            CorrelationId = correlationId,
        };
    }

    private static decimal NormalizeDeviation(decimal deviation)
    {
        if (deviation is > 1m or < -1m)
            return deviation / 100m;
        return deviation;
    }

    private static decimal NormalizePriceVariation(decimal? v)
    {
        if (v is null) return 0m;
        var x = v.Value;
        if (x is > 1m or < -1m)
            return x / 100m;
        return x;
    }
}
