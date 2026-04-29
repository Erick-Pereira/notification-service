namespace Simcag.NotificationService.Domain.ValueObjects;

/// <summary>
/// Níveis de severidade conhecidos; desconhecidos são tratados como <see cref="Info"/>.
/// </summary>
public static class AlertSeverityLevel
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Critical = "Critical";

    private static readonly Dictionary<string, int> Order = new(StringComparer.OrdinalIgnoreCase)
    {
        [Info] = 0,
        [Warning] = 1,
        [Critical] = 2
    };

    public static int Rank(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            return Order[Info];
        return Order.TryGetValue(severity.Trim(), out var v) ? v : Order[Info];
    }

    /// <summary>
    /// Retorna true se a severidade do evento atinge ou excede o mínimo configurado.
    /// </summary>
    public static bool MeetsMinimum(string? eventSeverity, string? minimumRequired)
    {
        return Rank(eventSeverity) >= Rank(minimumRequired);
    }
}
