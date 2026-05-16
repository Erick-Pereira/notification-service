using Simcag.NotificationService.Domain.Entities;

namespace Simcag.NotificationService.Infrastructure.Persistence;

internal static class EntityMappers
{
    public static Domain.Entities.Notification ToDomain(NotificationRecord r) =>
        Domain.Entities.Notification.Rehydrate(
            r.Id,
            r.UserId,
            r.Type,
            r.Channel,
            r.Recipient,
            r.Subject,
            r.Body,
            r.Status,
            r.SentAt,
            r.ErrorMessage,
            r.CreatedAt,
            r.Source,
            r.CorrelationId,
            r.Severity,
            r.TenantId,
            r.AlertId,
            r.ContextJson,
            r.PayloadSummary,
            r.OperationalLink,
            r.RetryCount,
            r.UpdatedAtUtc);

    public static NotificationRecord ToRecord(Domain.Entities.Notification n) =>
        new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Type = n.Type,
            Channel = n.Channel,
            Recipient = n.Recipient,
            Subject = n.Subject,
            Body = n.Body,
            Status = n.Status,
            SentAt = n.SentAt,
            ErrorMessage = n.ErrorMessage,
            CreatedAt = n.CreatedAt,
            Source = n.Source,
            CorrelationId = n.CorrelationId,
            Severity = n.Severity,
            TenantId = n.TenantId,
            AlertId = n.AlertId,
            ContextJson = n.ContextJson,
            PayloadSummary = n.PayloadSummary,
            OperationalLink = n.OperationalLink,
            RetryCount = n.RetryCount,
            UpdatedAtUtc = n.UpdatedAtUtc,
        };

    public static NotificationPreference ToDomain(NotificationPreferenceRecord r) =>
        NotificationPreference.Rehydrate(
            r.Id,
            r.UserId,
            r.EmailEnabled,
            r.SmsEnabled,
            r.EmailAddress,
            r.PhoneNumber,
            r.AlertDropEnabled,
            r.AlertRiseEnabled,
            r.AlertTrendEnabled,
            r.MinimumSeverity,
            r.MuteAllUntilUtc,
            r.SnoozePriceAlertsUntilUtc,
            r.CreatedAt,
            r.UpdatedAt);

    public static NotificationPreferenceRecord ToRecord(NotificationPreference p) =>
        new()
        {
            Id = p.Id,
            UserId = p.UserId,
            EmailEnabled = p.EmailEnabled,
            SmsEnabled = p.SmsEnabled,
            EmailAddress = p.EmailAddress,
            PhoneNumber = p.PhoneNumber,
            AlertDropEnabled = p.AlertDropEnabled,
            AlertRiseEnabled = p.AlertRiseEnabled,
            AlertTrendEnabled = p.AlertTrendEnabled,
            MinimumSeverity = p.MinimumSeverity,
            MuteAllUntilUtc = p.MuteAllUntilUtc,
            SnoozePriceAlertsUntilUtc = p.SnoozePriceAlertsUntilUtc,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
        };
}
