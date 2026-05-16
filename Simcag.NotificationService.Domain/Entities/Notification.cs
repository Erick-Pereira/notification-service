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

    /// <summary>Origem lógica (ex.: AlertTriggeredEvent, ManualSend).</summary>
    public string Source { get; private set; } = "Manual";

    public string? CorrelationId { get; private set; }
    public string? Severity { get; private set; }
    public Guid? TenantId { get; private set; }
    public string? AlertId { get; private set; }
    public string? ContextJson { get; private set; }
    public string? PayloadSummary { get; private set; }
    public string? OperationalLink { get; private set; }
    public int RetryCount { get; private set; }

    public DateTime? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Notification() { }

    private Notification(Guid userId, string type, string channel, string recipient, string subject, string body)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel is required", nameof(channel));
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required", nameof(recipient));

        var now = DateTime.UtcNow;
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Channel = channel;
        Recipient = recipient;
        Subject = subject ?? string.Empty;
        Body = body;
        Status = "Pending";
        Source = "Manual";
        CreatedAt = now;
        UpdatedAtUtc = now;
        RetryCount = 0;
    }

    public static Notification Create(Guid userId, string type, string channel, string recipient, string subject, string body)
        => new(userId, type, channel, recipient, subject, body);

    /// <summary>Entrega com metadados para governança, auditoria e UI operacional.</summary>
    public static Notification CreateOperational(
        Guid userId,
        string type,
        string channel,
        string recipient,
        string subject,
        string body,
        string initialStatus,
        string source,
        string? correlationId,
        string? severity,
        Guid? tenantId,
        string? alertId,
        string? contextJson,
        string? payloadSummary,
        string? operationalLink)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel is required", nameof(channel));
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient is required", nameof(recipient));

        var now = DateTime.UtcNow;
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Channel = channel,
            Recipient = recipient,
            Subject = subject ?? string.Empty,
            Body = body,
            Status = string.IsNullOrWhiteSpace(initialStatus) ? "Pending" : initialStatus.Trim(),
            Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim(),
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim(),
            Severity = string.IsNullOrWhiteSpace(severity) ? null : severity.Trim(),
            TenantId = tenantId,
            AlertId = string.IsNullOrWhiteSpace(alertId) ? null : alertId.Trim(),
            ContextJson = string.IsNullOrWhiteSpace(contextJson) ? null : contextJson.Trim(),
            PayloadSummary = string.IsNullOrWhiteSpace(payloadSummary) ? null : payloadSummary.Trim(),
            OperationalLink = string.IsNullOrWhiteSpace(operationalLink) ? null : operationalLink.Trim(),
            RetryCount = 0,
            CreatedAt = now,
            UpdatedAtUtc = now,
        };
    }

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
        DateTime createdAt,
        string source,
        string? correlationId,
        string? severity,
        Guid? tenantId,
        string? alertId,
        string? contextJson,
        string? payloadSummary,
        string? operationalLink,
        int retryCount,
        DateTime updatedAtUtc) =>
        new()
        {
            Id = id,
            UserId = userId,
            Type = type,
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = status,
            SentAt = sentAt,
            ErrorMessage = errorMessage,
            CreatedAt = createdAt,
            Source = string.IsNullOrWhiteSpace(source) ? "Manual" : source,
            CorrelationId = correlationId,
            Severity = severity,
            TenantId = tenantId,
            AlertId = alertId,
            ContextJson = contextJson,
            PayloadSummary = payloadSummary,
            OperationalLink = operationalLink,
            RetryCount = retryCount,
            UpdatedAtUtc = updatedAtUtc,
        };

    public void MarkAsSent()
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        Touch();
    }

    /// <summary>Deduplicação ou rate limit Redis — entrega não tentada.</summary>
    public void MarkAsSuppressed(string reason)
    {
        Status = "Suppressed";
        ErrorMessage = reason;
        Touch();
    }

    /// <summary>Filtro de preferências, mute/snooze ou severidade.</summary>
    public void MarkAsFiltered(string reason)
    {
        Status = "Filtered";
        ErrorMessage = reason;
        Touch();
    }

    public void PrepareRetry()
    {
        if (Status != "Failed")
            throw new InvalidOperationException("Retry só é permitido para notificações falhadas.");
        RetryCount++;
        Status = "Pending";
        ErrorMessage = null;
        SentAt = null;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
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

    /// <summary>Silencia todos os canais até UTC (inclusive operação corporativa).</summary>
    public DateTime? MuteAllUntilUtc { get; private set; }

    /// <summary>Pausa apenas alertas de preço (fila alert-triggered) até UTC.</summary>
    public DateTime? SnoozePriceAlertsUntilUtc { get; private set; }

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
        DateTime? muteAllUntilUtc,
        DateTime? snoozePriceAlertsUntilUtc,
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
            muteAllUntilUtc,
            snoozePriceAlertsUntilUtc,
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
        DateTime? muteAllUntilUtc,
        DateTime? snoozePriceAlertsUntilUtc,
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
        MuteAllUntilUtc = muteAllUntilUtc;
        SnoozePriceAlertsUntilUtc = snoozePriceAlertsUntilUtc;
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

    public void UpdateMuteAndSnooze(DateTime? muteAllUntilUtc, DateTime? snoozePriceAlertsUntilUtc)
    {
        MuteAllUntilUtc = muteAllUntilUtc;
        SnoozePriceAlertsUntilUtc = snoozePriceAlertsUntilUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsGloballyMuted(DateTime utcNow) => MuteAllUntilUtc.HasValue && MuteAllUntilUtc.Value > utcNow;

    public bool IsPriceAlertSnoozed(DateTime utcNow) =>
        SnoozePriceAlertsUntilUtc.HasValue && SnoozePriceAlertsUntilUtc.Value > utcNow;

    public bool IsSeverityEnabled(string? eventSeverity) =>
        AlertSeverityLevel.MeetsMinimum(eventSeverity, MinimumSeverity);
}
