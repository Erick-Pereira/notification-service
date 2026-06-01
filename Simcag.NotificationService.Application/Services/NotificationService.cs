using System.Text.Json;
using Microsoft.Extensions.Logging;
using Simcag.Shared.ErrorHandling;
using Simcag.NotificationService.Application.Abstractions;
using Simcag.NotificationService.Application.DTOs;
using Simcag.NotificationService.Application.Governance;
using Simcag.NotificationService.Application.Mapping;
using Simcag.NotificationService.Domain.Entities;
using Simcag.NotificationService.Domain.Interfaces;

namespace Simcag.NotificationService.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IEmailProvider _emailProvider;
    private readonly ISmsProvider _smsProvider;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationSendPolicy _sendPolicy;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailProvider emailProvider,
        ISmsProvider smsProvider,
        INotificationPreferenceRepository preferenceRepository,
        INotificationRepository notificationRepository,
        INotificationSendPolicy sendPolicy,
        ILogger<NotificationService> logger)
    {
        _emailProvider = emailProvider;
        _smsProvider = smsProvider;
        _preferenceRepository = preferenceRepository;
        _notificationRepository = notificationRepository;
        _sendPolicy = sendPolicy;
        _logger = logger;
    }

    public NotificationGovernanceDto GetGovernanceCatalog() => NotificationGovernanceCatalog.Build();

    public IReadOnlyList<NotificationTemplateDto> GetTemplates() => NotificationGovernanceCatalog.Templates();

    public async Task<NotificationDeliveryPageDto> ListDeliveriesAsync(
        Guid userId,
        string? status,
        string? channel,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _notificationRepository
            .GetDeliveriesPageAsync(userId, status, channel, page, pageSize, cancellationToken)
            .ConfigureAwait(false);
        return new NotificationDeliveryPageDto
        {
            Items = items.Select(ToDeliveryDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<NotificationDashboardDto> GetOperationalDashboardAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var counts = await _notificationRepository.CountByStatusForUserAsync(userId, cancellationToken).ConfigureAwait(false);
        int G(string key) => counts.TryGetValue(key, out var n) ? n : 0;
        var total = counts.Values.Sum();
        return new NotificationDashboardDto
        {
            Total = total,
            Pending = G("Pending"),
            Sent = G("Sent"),
            Failed = G("Failed"),
            Suppressed = G("Suppressed"),
            Filtered = G("Filtered"),
        };
    }

    public async Task<bool> RetryDeliveryAsync(Guid deliveryId, Guid userId, CancellationToken cancellationToken = default)
    {
        var n = await _notificationRepository.GetByIdAsync(deliveryId, cancellationToken).ConfigureAwait(false);
        if (n == null || n.UserId != userId)
            return false;
        if (n.Channel is not ("Email" or "SMS"))
            return false;
        try
        {
            n.PrepareRetry();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        await _notificationRepository.UpdateAsync(n, cancellationToken).ConfigureAwait(false);

        if (n.Channel == "Email")
        {
            return await DispatchEmailForTrackedAsync(n, cancellationToken).ConfigureAwait(false);
        }

        return await DispatchSmsForTrackedAsync(n, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SendAlertNotificationAsync(AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        var utc = DateTime.UtcNow;
        var productLabel = string.IsNullOrWhiteSpace(alert.ProductName) ? (alert.ProductId ?? "N/A") : alert.ProductName!;
        var subject = $"Price Alert: {alert.AlertType} - {productLabel}";
        var body = BuildAlertBody(alert, productLabel);
        var contextJson = BuildContextJson(alert);
        var summary = Truncate(alert.Message ?? subject, 480);
        var opLink = BuildOperationalLink(alert);
        const string eventSource = "AlertPipeline";

        async Task RecordFilteredAsync(string reason) =>
            await SaveFilteredAsync(
                    alert.UserId,
                    subject,
                    body,
                    reason,
                    alert,
                    contextJson,
                    summary,
                    opLink,
                    cancellationToken)
                .ConfigureAwait(false);

        var preferences = await _preferenceRepository.GetByUserIdAsync(alert.UserId, cancellationToken).ConfigureAwait(false);
        if (preferences == null)
        {
            await RecordFilteredAsync("Sem preferências de contacto configuradas.").ConfigureAwait(false);
            return false;
        }

        if (preferences.IsGloballyMuted(utc))
        {
            await RecordFilteredAsync("Mute global ativo até " + preferences.MuteAllUntilUtc!.Value.ToString("u")).ConfigureAwait(false);
            return false;
        }

        if (preferences.IsPriceAlertSnoozed(utc))
        {
            await RecordFilteredAsync("Snooze de alertas de preço até " + preferences.SnoozePriceAlertsUntilUtc!.Value.ToString("u")).ConfigureAwait(false);
            return false;
        }

        if (!preferences.IsSeverityEnabled(alert.Severity))
        {
            await RecordFilteredAsync($"Severidade abaixo do mínimo ({preferences.MinimumSeverity}).").ConfigureAwait(false);
            return false;
        }

        if (!ResolveShouldNotify(alert, preferences))
        {
            await RecordFilteredAsync($"Tipo de alerta desativado nas preferências ({alert.AlertType}).").ConfigureAwait(false);
            return false;
        }

        var success = true;
        var alertKeyBase = !string.IsNullOrWhiteSpace(alert.AlertId) ? alert.AlertId! : alert.OccurredAt.Ticks.ToString();

        if (preferences.EmailEnabled && !string.IsNullOrWhiteSpace(preferences.EmailAddress))
        {
            var dedup = $"{alert.UserId}:alert:{alertKeyBase}:email";
            var rate = $"{alert.UserId}:email:alert";
            var row = Notification.CreateOperational(
                alert.UserId,
                "PriceAlert",
                "Email",
                preferences.EmailAddress!,
                subject,
                body,
                "Pending",
                eventSource,
                alert.CorrelationId,
                alert.Severity,
                alert.TenantId,
                alert.AlertId,
                contextJson,
                summary,
                opLink);
            await _notificationRepository.AddAsync(row, cancellationToken).ConfigureAwait(false);

            if (!await _sendPolicy.TryAcquireAsync(dedup, rate, cancellationToken).ConfigureAwait(false))
            {
                row.MarkAsSuppressed("Deduplicação ou rate limit (Redis).");
                await _notificationRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Email skipped (deduplication or rate) for user {UserId}", alert.UserId);
            }
            else
            {
                success = await DispatchEmailForTrackedAsync(row, cancellationToken).ConfigureAwait(false) && success;
            }
        }

        if (preferences.SmsEnabled && !string.IsNullOrWhiteSpace(preferences.PhoneNumber))
        {
            var smsBody = string.IsNullOrWhiteSpace(alert.Message)
                ? $"[{alert.AlertType}] {productLabel}: {alert.CurrentPrice:C} ({alert.PriceChange:P0} from {alert.Source})"
                : Truncate(alert.Message!, 1400);
            var dedup = $"{alert.UserId}:alert:{alertKeyBase}:sms";
            var rate = $"{alert.UserId}:sms:alert";
            var row = Notification.CreateOperational(
                alert.UserId,
                "PriceAlert",
                "SMS",
                preferences.PhoneNumber!,
                string.Empty,
                smsBody,
                "Pending",
                eventSource,
                alert.CorrelationId,
                alert.Severity,
                alert.TenantId,
                alert.AlertId,
                contextJson,
                summary,
                opLink);
            await _notificationRepository.AddAsync(row, cancellationToken).ConfigureAwait(false);

            if (!await _sendPolicy.TryAcquireAsync(dedup, rate, cancellationToken).ConfigureAwait(false))
            {
                row.MarkAsSuppressed("Deduplicação ou rate limit (Redis).");
                await _notificationRepository.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("SMS skipped (deduplication or rate) for user {UserId}", alert.UserId);
            }
            else
            {
                success = await DispatchSmsForTrackedAsync(row, cancellationToken).ConfigureAwait(false) && success;
            }
        }

        return success;
    }

    private async Task SaveFilteredAsync(
        Guid userId,
        string subject,
        string body,
        string reason,
        AlertNotificationDto alert,
        string contextJson,
        string summary,
        string opLink,
        CancellationToken ct)
    {
        var row = Notification.CreateOperational(
            userId,
            "PriceAlert",
            "Policy",
            "-",
            subject,
            body,
            "Pending",
            "AlertPipeline",
            alert.CorrelationId,
            alert.Severity,
            alert.TenantId,
            alert.AlertId,
            contextJson,
            summary,
            opLink);
        row.MarkAsFiltered(reason);
        await _notificationRepository.AddAsync(row, ct).ConfigureAwait(false);
    }

    private async Task<bool> DispatchEmailForTrackedAsync(Notification notification, CancellationToken ct)
    {
        try
        {
            var ok = await _emailProvider
                .SendAsync(notification.Recipient, notification.Subject, notification.Body, ct)
                .ConfigureAwait(false);
            if (ok)
            {
                notification.MarkAsSent();
                await _notificationRepository.UpdateAsync(notification, ct).ConfigureAwait(false);
                _logger.LogInformation("Email sent successfully to {Recipient}", notification.Recipient);
                return true;
            }

            notification.MarkAsFailed("Provider returned false");
            await _notificationRepository.UpdateAsync(notification, ct).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ErrorSanitizer.Sanitize(ex.Message));
            await _notificationRepository.UpdateAsync(notification, ct).ConfigureAwait(false);
            _logger.LogError(ex, "Failed to send email to {Recipient}", notification.Recipient);
            return false;
        }
    }

    private async Task<bool> DispatchSmsForTrackedAsync(Notification notification, CancellationToken ct)
    {
        try
        {
            var ok = await _smsProvider.SendAsync(notification.Recipient, notification.Body, ct).ConfigureAwait(false);
            if (ok)
            {
                notification.MarkAsSent();
                await _notificationRepository.UpdateAsync(notification, ct).ConfigureAwait(false);
                _logger.LogInformation("SMS sent successfully to {Recipient}", notification.Recipient);
                return true;
            }

            notification.MarkAsFailed("Provider returned false");
            await _notificationRepository.UpdateAsync(notification, ct).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ErrorSanitizer.Sanitize(ex.Message));
            await _notificationRepository.UpdateAsync(notification, ct).ConfigureAwait(false);
            _logger.LogError(ex, "Failed to send SMS to {Recipient}", notification.Recipient);
            return false;
        }
    }

    private static NotificationDeliveryDto ToDeliveryDto(Notification n) =>
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
            Source = n.Source,
            CorrelationId = n.CorrelationId,
            Severity = n.Severity,
            TenantId = n.TenantId,
            AlertId = n.AlertId,
            ContextJson = n.ContextJson,
            PayloadSummary = n.PayloadSummary,
            OperationalLink = n.OperationalLink,
            RetryCount = n.RetryCount,
            SentAt = n.SentAt,
            ErrorMessage = n.ErrorMessage,
            CreatedAt = n.CreatedAt,
            UpdatedAtUtc = n.UpdatedAtUtc,
        };

    private static string BuildContextJson(AlertNotificationDto alert) =>
        JsonSerializer.Serialize(
            new
            {
                alert.AlertId,
                alert.ProductId,
                alert.AlertType,
                alert.AlertCategory,
                alert.TenantId,
                alert.OccurredAt,
            });

    private static string BuildOperationalLink(AlertNotificationDto alert)
    {
        if (!string.IsNullOrWhiteSpace(alert.ProductId))
            return $"/insights?productId={Uri.EscapeDataString(alert.ProductId)}";
        if (!string.IsNullOrWhiteSpace(alert.AlertId))
            return $"/alertas?alertId={Uri.EscapeDataString(alert.AlertId)}";
        return "/alertas";
    }

    private static string BuildAlertBody(AlertNotificationDto alert, string productLabel)
    {
        if (!string.IsNullOrWhiteSpace(alert.Message))
        {
            return
                $"""
                 {alert.Message}
                 Product: {productLabel}
                 Alert Type: {alert.AlertType}
                 Severity: {alert.Severity ?? "n/a"}
                 Time: {alert.OccurredAt:u}
                 """;
        }

        return
            $"""
             Price Alert Notification
             ---------------------
             Product: {productLabel}
             Alert Type: {alert.AlertType}
             Current Price: {alert.CurrentPrice:C}
             Change: {alert.PriceChange:P2}
             Source: {alert.Source}
             Time: {alert.OccurredAt:u}
             """;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    private static bool ResolveShouldNotify(AlertNotificationDto alert, NotificationPreference preferences)
    {
        if (PriceAlertKindMapper.TryGetKind(alert.AlertType, alert.AlertCategory, out var mapped))
        {
            return PriceAlertKindMapper.IsEnabledFor(mapped, preferences);
        }

        var t = (alert.AlertType ?? string.Empty).ToUpperInvariant();
        return t switch
        {
            "DROP" => preferences.AlertDropEnabled,
            "RISE" => preferences.AlertRiseEnabled,
            "TREND" => preferences.AlertTrendEnabled,
            _ => false,
        };
    }

    public async Task<bool> SendNotificationAsync(SendNotificationRequestDto request, CancellationToken cancellationToken = default)
    {
        var channel = (request.Channel ?? string.Empty).Trim();
        if (channel.Equals("email", StringComparison.OrdinalIgnoreCase))
        {
            return await SendEmailAsync(request.UserId, request.Subject, request.Body, cancellationToken).ConfigureAwait(false);
        }

        if (channel.Equals("sms", StringComparison.OrdinalIgnoreCase))
        {
            return await SendSmsAsync(request.UserId, request.Body, cancellationToken).ConfigureAwait(false);
        }

        if (await SendEmailAsync(request.UserId, request.Subject, request.Body, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        return await SendSmsAsync(request.UserId, request.Body, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SendEmailAsync(Guid userId, string subject, string body, CancellationToken cancellationToken = default)
    {
        var preferences = await _preferenceRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (preferences?.IsGloballyMuted(DateTime.UtcNow) == true)
        {
            return false;
        }

        if (preferences?.EmailEnabled != true || string.IsNullOrWhiteSpace(preferences.EmailAddress))
        {
            return false;
        }

        var rate = $"{userId}:email:manual";
        if (!await _sendPolicy.TryAcquireAsync(string.Empty, rate, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        return await SendEmailInternalAsync(userId, "Email", preferences.EmailAddress, subject, body, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SendSmsAsync(Guid userId, string message, CancellationToken cancellationToken = default)
    {
        var preferences = await _preferenceRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (preferences?.IsGloballyMuted(DateTime.UtcNow) == true)
        {
            return false;
        }

        if (preferences?.SmsEnabled != true || string.IsNullOrWhiteSpace(preferences.PhoneNumber))
        {
            return false;
        }

        var rate = $"{userId}:sms:manual";
        if (!await _sendPolicy.TryAcquireAsync(string.Empty, rate, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        return await SendSmsInternalAsync(userId, "SMS", preferences.PhoneNumber, message, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> SendEmailInternalAsync(
        Guid userId,
        string type,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(userId, type, "Email", recipient, subject, body);
        await _notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);

        try
        {
            var success = await _emailProvider.SendAsync(recipient, subject, body, cancellationToken).ConfigureAwait(false);
            if (success)
            {
                notification.MarkAsSent();
                await _notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
                return true;
            }

            notification.MarkAsFailed("Provider returned false");
            await _notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ErrorSanitizer.Sanitize(ex.Message));
            await _notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            return false;
        }
    }

    private async Task<bool> SendSmsInternalAsync(
        Guid userId,
        string type,
        string recipient,
        string body,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(userId, type, "SMS", recipient, string.Empty, body);
        await _notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);

        try
        {
            var success = await _smsProvider.SendAsync(recipient, body, cancellationToken).ConfigureAwait(false);
            if (success)
            {
                notification.MarkAsSent();
                await _notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("SMS sent successfully to {Recipient}", recipient);
                return true;
            }

            notification.MarkAsFailed("Provider returned false");
            await _notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ErrorSanitizer.Sanitize(ex.Message));
            await _notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Failed to send SMS to {Recipient}", recipient);
            return false;
        }
    }

    public Task<NotificationPreference?> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

    public async Task UpdateUserPreferencesAsync(UpdatePreferencesDto preferences, CancellationToken cancellationToken = default)
    {
        var existing = await _preferenceRepository.GetByUserIdAsync(preferences.UserId, cancellationToken).ConfigureAwait(false);
        if (existing == null)
        {
            var newPreference = NotificationPreference.Create(
                preferences.UserId,
                preferences.EmailAddress,
                preferences.PhoneNumber);
            newPreference.UpdatePreferences(
                preferences.EmailEnabled,
                preferences.SmsEnabled,
                preferences.EmailAddress,
                preferences.PhoneNumber);
            if (preferences.AlertDropEnabled.HasValue
                || preferences.AlertRiseEnabled.HasValue
                || preferences.AlertTrendEnabled.HasValue)
            {
                newPreference.UpdateAlertPreferences(
                    preferences.AlertDropEnabled ?? true,
                    preferences.AlertRiseEnabled ?? true,
                    preferences.AlertTrendEnabled ?? true);
            }

            if (preferences.MinimumSeverity is not null)
            {
                newPreference.UpdateMinimumSeverity(preferences.MinimumSeverity);
            }

            if (preferences.ApplyMuteSnooze)
            {
                newPreference.UpdateMuteAndSnooze(preferences.MuteAllUntilUtc, preferences.SnoozePriceAlertsUntilUtc);
            }

            await _preferenceRepository.AddAsync(newPreference, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.UpdatePreferences(
                preferences.EmailEnabled,
                preferences.SmsEnabled,
                preferences.EmailAddress,
                preferences.PhoneNumber);
            if (preferences.AlertDropEnabled.HasValue
                || preferences.AlertRiseEnabled.HasValue
                || preferences.AlertTrendEnabled.HasValue)
            {
                existing.UpdateAlertPreferences(
                    preferences.AlertDropEnabled ?? existing.AlertDropEnabled,
                    preferences.AlertRiseEnabled ?? existing.AlertRiseEnabled,
                    preferences.AlertTrendEnabled ?? existing.AlertTrendEnabled);
            }

            if (preferences.MinimumSeverity is not null)
            {
                existing.UpdateMinimumSeverity(preferences.MinimumSeverity);
            }

            if (preferences.ApplyMuteSnooze)
            {
                existing.UpdateMuteAndSnooze(preferences.MuteAllUntilUtc, preferences.SnoozePriceAlertsUntilUtc);
            }

            await _preferenceRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        }
    }
}
