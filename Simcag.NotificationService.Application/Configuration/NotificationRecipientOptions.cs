namespace Simcag.NotificationService.Application.Configuration;

/// <summary>
/// Destinatários de alerta: fallback dev.
/// </summary>
public sealed class NotificationRecipientOptions
{
    /// <summary>Utilizador padrão quando tenant/recipients não resolvem (dev/homologação).</summary>
    public Guid? DefaultNotifyUserId { get; init; }

    /// <summary>E-mail usado no bootstrap de preferências quando configurado.</summary>
    public string? DevFallbackEmail { get; init; }
}
