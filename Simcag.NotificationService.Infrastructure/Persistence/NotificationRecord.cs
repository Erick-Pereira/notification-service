namespace Simcag.NotificationService.Infrastructure.Persistence;

public sealed class NotificationRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class NotificationPreferenceRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public bool AlertDropEnabled { get; set; } = true;
    public bool AlertRiseEnabled { get; set; } = true;
    public bool AlertTrendEnabled { get; set; } = true;
    public string MinimumSeverity { get; set; } = "Info";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
