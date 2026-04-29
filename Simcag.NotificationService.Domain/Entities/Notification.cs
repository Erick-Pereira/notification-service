using Simcag.NotificationService.Domain.ValueObjects;

namespace Simcag.NotificationService.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Channel { get; private set; } = string.Empty;
    public string Recipient { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public DateTime? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    private Notification(Guid userId, string type, string channel, string recipient, string subject, string body)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel is required", nameof(channel));
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required", nameof(recipient));

        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Channel = channel;
        Recipient = recipient;
        Subject = subject ?? string.Empty;
        Body = body;
        Status = "Pending";
        CreatedAt = DateTime.UtcNow;
    }

    public static Notification Create(Guid userId, string type, string channel, string recipient, string subject, string body)
        => new(userId, type, channel, recipient, subject, body);

    /// <summary>Reconstrução a partir do armazenamento (EF, etc.).</summary>
    public static Notification Rehydrate(
        Guid id,
        Guid userId,
        string type,
        string channel,
        string recipient,
        string subject,
        string body,
        string status,
        DateTime? sentAt,
        string? errorMessage,
        DateTime createdAt) =>
        new(
            id,
            userId,
            type,
            channel,
            recipient,
            subject,
            body,
            status,
            sentAt,
            errorMessage,
            createdAt);

    private Notification(
        Guid id,
        Guid userId,
        string type,
        string channel,
        string recipient,
        string subject,
        string body,
        string status,
        DateTime? sentAt,
        string? errorMessage,
        DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        Type = type;
        Channel = channel;
        Recipient = recipient;
        Subject = subject;
        Body = body;
        Status = status;
        SentAt = sentAt;
        ErrorMessage = errorMessage;
        CreatedAt = createdAt;
    }

    public void MarkAsSent()
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
    }
}

public class NotificationPreference
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public string? EmailAddress { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool AlertDropEnabled { get; private set; } = true;
    public bool AlertRiseEnabled { get; private set; } = true;
    public bool AlertTrendEnabled { get; private set; } = true;
    /// <summary>
    /// Notificações com severidade abaixo deste mínimo são ignoradas (Info, Warning, Critical).
    /// </summary>
    public string MinimumSeverity { get; private set; } = AlertSeverityLevel.Info;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private NotificationPreference() { }

    private NotificationPreference(Guid userId, string? emailAddress, string? phoneNumber)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        Id = Guid.NewGuid();
        UserId = userId;
        EmailAddress = emailAddress;
        PhoneNumber = phoneNumber;
        EmailEnabled = !string.IsNullOrWhiteSpace(emailAddress);
        SmsEnabled = !string.IsNullOrWhiteSpace(phoneNumber);
        AlertDropEnabled = true;
        AlertRiseEnabled = true;
        AlertTrendEnabled = true;
        MinimumSeverity = AlertSeverityLevel.Info;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static NotificationPreference Create(Guid userId, string? emailAddress = null, string? phoneNumber = null)
        => new(userId, emailAddress, phoneNumber);

    public static NotificationPreference Rehydrate(
        Guid id,
        Guid userId,
        bool emailEnabled,
        bool smsEnabled,
        string? emailAddress,
        string? phoneNumber,
        bool alertDropEnabled,
        bool alertRiseEnabled,
        bool alertTrendEnabled,
        string minimumSeverity,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            userId,
            emailEnabled,
            smsEnabled,
            emailAddress,
            phoneNumber,
            alertDropEnabled,
            alertRiseEnabled,
            alertTrendEnabled,
            minimumSeverity,
            createdAt,
            updatedAt);

    private NotificationPreference(
        Guid id,
        Guid userId,
        bool emailEnabled,
        bool smsEnabled,
        string? emailAddress,
        string? phoneNumber,
        bool alertDropEnabled,
        bool alertRiseEnabled,
        bool alertTrendEnabled,
        string minimumSeverity,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        UserId = userId;
        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
        EmailAddress = emailAddress;
        PhoneNumber = phoneNumber;
        AlertDropEnabled = alertDropEnabled;
        AlertRiseEnabled = alertRiseEnabled;
        AlertTrendEnabled = alertTrendEnabled;
        MinimumSeverity = minimumSeverity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void UpdatePreferences(bool emailEnabled, bool smsEnabled, string? emailAddress, string? phoneNumber)
    {
        EmailEnabled = emailEnabled;
        SmsEnabled = smsEnabled;
        EmailAddress = emailAddress;
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAlertPreferences(bool dropEnabled, bool riseEnabled, bool trendEnabled)
    {
        AlertDropEnabled = dropEnabled;
        AlertRiseEnabled = riseEnabled;
        AlertTrendEnabled = trendEnabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMinimumSeverity(string? minimumSeverity)
    {
        if (string.IsNullOrWhiteSpace(minimumSeverity))
        {
            MinimumSeverity = AlertSeverityLevel.Info;
        }
        else
        {
            var s = minimumSeverity.Trim();
            MinimumSeverity = s.Equals(AlertSeverityLevel.Warning, StringComparison.OrdinalIgnoreCase)
                || s.Equals(AlertSeverityLevel.Critical, StringComparison.OrdinalIgnoreCase)
                || s.Equals(AlertSeverityLevel.Info, StringComparison.OrdinalIgnoreCase)
                ? s
                : AlertSeverityLevel.Info;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsSeverityEnabled(string? eventSeverity) =>
        AlertSeverityLevel.MeetsMinimum(eventSeverity, MinimumSeverity);
}