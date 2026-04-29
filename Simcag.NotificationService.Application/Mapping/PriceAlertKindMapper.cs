using Simcag.NotificationService.Domain.ValueObjects;

namespace Simcag.NotificationService.Application.Mapping;

/// <summary>
/// Mapeia rótulos de eventos (incl. financeiros) para a preferência interna Drop/Rise/Trend.
/// </summary>
public static class PriceAlertKindMapper
{
    public static bool TryGetKind(string? alertType, string? alertCategory, out PriceAlertKind kind)
    {
        var t = (alertType ?? string.Empty) + " " + (alertCategory ?? string.Empty);
        if (t.Contains("TREND", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Trend", StringComparison.OrdinalIgnoreCase))
        {
            kind = PriceAlertKind.Trend;
            return true;
        }
        if (t.Contains("DROP", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Drop", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Under", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Underprice", StringComparison.OrdinalIgnoreCase))
        {
            kind = PriceAlertKind.Drop;
            return true;
        }
        if (t.Contains("RISE", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Rise", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Over", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Overprice", StringComparison.OrdinalIgnoreCase))
        {
            kind = PriceAlertKind.Rise;
            return true;
        }
        if (t.Contains("Superf", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Concentra", StringComparison.OrdinalIgnoreCase))
        {
            kind = PriceAlertKind.Rise;
            return true;
        }

        kind = default;
        return false;
    }

    public static bool IsEnabledFor(PriceAlertKind kind, Domain.Entities.NotificationPreference p) =>
        kind switch
        {
            PriceAlertKind.Drop => p.AlertDropEnabled,
            PriceAlertKind.Rise => p.AlertRiseEnabled,
            PriceAlertKind.Trend => p.AlertTrendEnabled,
            _ => false
        };
}
