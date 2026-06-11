namespace Simcag.NotificationService.Application.Branding;

/// <summary>Nome do produto visível em e-mails, SMS e templates de alerta.</summary>
public static class NotificationBranding
{
    public const string ProductName = "Econdomiza";

    public static string EmailSenderDisplayName =>
        FirstNonEmpty("SMTP__FROMNAME", "NOTIFICATION__SENDER_NAME") ?? ProductName;

    public static string AlertEmailSubject(string alertType, string productLabel) =>
        $"{ProductName} — Alerta: {alertType} — {productLabel}";

    public static string AlertEmailBodyFallbackHeader => $"Notificação de alerta — {ProductName}";

    private static string? FirstNonEmpty(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
