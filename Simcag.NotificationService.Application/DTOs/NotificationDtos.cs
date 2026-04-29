namespace Simcag.NotificationService.Application.DTOs;

public class AlertNotificationDto
{
    public Guid UserId { get; init; }
    public string? AlertId { get; init; }
    public string? ProductName { get; init; } = string.Empty;
    public string? ProductId { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string? AlertCategory { get; init; }
    public string? Message { get; init; }
    public string? Severity { get; init; }
    public Guid? TenantId { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal PriceChange { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}

public class UpdatePreferencesDto
{
    public Guid UserId { get; init; }
    public bool EmailEnabled { get; init; }
    public bool SmsEnabled { get; init; }
    public string? EmailAddress { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? AlertDropEnabled { get; init; }
    public bool? AlertRiseEnabled { get; init; }
    public bool? AlertTrendEnabled { get; init; }
    public string? MinimumSeverity { get; init; }
}

public class SendNotificationRequestDto
{
    public Guid UserId { get; init; }
    /// <summary>Se vazio, envia e-mail (se ativo) e em seguida SMS se e-mail indisponível (fallback).</summary>
    public string? Channel { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

public class PreferencesResponseDto
{
    public Guid UserId { get; init; }
    public bool EmailEnabled { get; init; }
    public bool SmsEnabled { get; init; }
    public string? EmailAddress { get; init; }
    public string? PhoneNumber { get; init; }
    public bool AlertDropEnabled { get; init; }
    public bool AlertRiseEnabled { get; init; }
    public bool AlertTrendEnabled { get; init; }
    public string MinimumSeverity { get; init; } = "Info";
}
