namespace Simcag.NotificationService.Domain.ValueObjects;

/// <summary>
/// Níveis de severidade conhecidos; desconhecidos são tratados como <see cref="Info"/>.
/// Alinhado com TCC_ECONDOMIZA.docx: NORMAL → Info, WARNING → Warning, CRITICAL → Critical
/// </summary>
public static class AlertSeverityLevel
{
    /// <summary>Severidade baixa (anteriormente "NORMAL" no TCC).</summary>
    public const string Info = "Info";
    
    /// <summary>Severidade média (WARNING).</summary>
    public const string Warning = "Warning";
    
    /// <summary>Severidade alta (CRITICAL).</summary>
    public const string Critical = "Critical";

    private static readonly Dictionary<string, int> Order = new(StringComparer.OrdinalIgnoreCase)
    {
        [Info] = 0,      // NORMAL → Info
        [Warning] = 1,   // WARNING → Warning
        [Critical] = 2   // CRITICAL → Critical
    };

    public static int Rank(string? severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            return Order[Info];
        
        var normalized = severity.Trim();
        return Order.TryGetValue(normalized, out var v) ? v : Order[Info];
    }

    /// <summary>
    /// Retorna true se a severidade do evento atinge ou excede o mínimo configurado.
    /// </summary>
    public static bool MeetsMinimum(string? eventSeverity, string? minimumRequired)
    {
        return Rank(eventSeverity) >= Rank(minimumRequired);
    }

    /// <summary>
    /// Converte nomenclatura do TCC para código (backward compatibility).
    /// Mapeamento: NORMAL → Info, WARNING → Warning, CRITICAL → Critical
    /// </summary>
    public static string NormalizeFromTcc(string severity) => severity switch
    {
        "NORMAL" => Info,
        "WARNING" => Warning,
        "CRITICAL" => Critical,
        _ => severity
    };
}
