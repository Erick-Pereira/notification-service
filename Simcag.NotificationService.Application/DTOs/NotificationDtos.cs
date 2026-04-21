namespace Simcag.NotificationService.Application.DTOs;

public class AlertNotificationDto
{
    public Guid UserId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string AlertType { get; init; } = string.Empty;
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
}

public class NotificationResponseDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? SentAt { get; init; }
    public DateTime CreatedAt { get; init; }
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
}