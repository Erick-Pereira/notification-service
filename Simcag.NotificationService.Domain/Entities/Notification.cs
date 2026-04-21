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
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static NotificationPreference Create(Guid userId, string? emailAddress = null, string? phoneNumber = null)
        => new(userId, emailAddress, phoneNumber);

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
}