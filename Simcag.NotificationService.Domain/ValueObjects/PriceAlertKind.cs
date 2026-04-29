namespace Simcag.NotificationService.Domain.ValueObjects;

/// <summary>
/// Tipo de alerta de preço mapeado para as preferências Drop/Rise/Trend.
/// </summary>
public enum PriceAlertKind
{
    Drop = 0,
    Rise = 1,
    Trend = 2
}
