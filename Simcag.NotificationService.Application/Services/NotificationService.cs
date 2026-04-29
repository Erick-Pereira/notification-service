using Microsoft.Extensions.Logging;
using Simcag.NotificationService.Application.Abstractions;
using Simcag.NotificationService.Application.DTOs;
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

    public async Task<bool> SendAlertNotificationAsync(AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        var preferences = await _preferenceRepository.GetByUserIdAsync(alert.UserId, cancellationToken);
        if (preferences == null)
        {
            _logger.LogWarning("No preferences found for user {UserId}", alert.UserId);
            return false;
        }

        if (!preferences.IsSeverityEnabled(alert.Severity))
        {
            _logger.LogInformation(
                "Severity {Severity} below user minimum {Min} for user {UserId}",
                alert.Severity, preferences.MinimumSeverity, alert.UserId);
            return false;
        }

        var shouldNotify = ResolveShouldNotify(alert, preferences);
        if (!shouldNotify)
        {
            _logger.LogInformation("Alert type filtered for user {UserId} ({Type})", alert.UserId, alert.AlertType);
            return false;
        }

        var productLabel = string.IsNullOrWhiteSpace(alert.ProductName) ? (alert.ProductId ?? "N/A") : alert.ProductName!;
        var subject = $"Price Alert: {alert.AlertType} - {productLabel}";
        var body = BuildAlertBody(alert, productLabel);

        var success = true;
        var alertKeyBase = !string.IsNullOrWhiteSpace(alert.AlertId) ? alert.AlertId! : alert.OccurredAt.Ticks.ToString();

        if (preferences.EmailEnabled && !string.IsNullOrWhiteSpace(preferences.EmailAddress))
        {
            var dedup = $"{alert.UserId}:alert:{alertKeyBase}:email";
            var rate = $"{alert.UserId}:email:alert";
            if (await _sendPolicy.TryAcquireAsync(dedup, rate, cancellationToken))
            {
                success = await SendEmailInternalAsync(alert.UserId, "Email", preferences.EmailAddress!, subject, body, cancellationToken)
                    && success;
            }
            else
            {
                _logger.LogDebug("Email skipped (deduplication or rate) for user {UserId}", alert.UserId);
            }
        }

        if (preferences.SmsEnabled && !string.IsNullOrWhiteSpace(preferences.PhoneNumber))
        {
            var smsBody = string.IsNullOrWhiteSpace(alert.Message)
                ? $"[{alert.AlertType}] {productLabel}: {alert.CurrentPrice:C} ({alert.PriceChange:P0} from {alert.Source})"
                : Truncate(alert.Message!, 1400);
            var dedup = $"{alert.UserId}:alert:{alertKeyBase}:sms";
            var rate = $"{alert.UserId}:sms:alert";
            if (await _sendPolicy.TryAcquireAsync(dedup, rate, cancellationToken))
            {
                success = await SendSmsInternalAsync(alert.UserId, "SMS", preferences.PhoneNumber!, smsBody, cancellationToken)
                    && success;
            }
            else
            {
                _logger.LogDebug("SMS skipped (deduplication or rate) for user {UserId}", alert.UserId);
            }
        }

        return success;
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
            _ => false
        };
    }

    public async Task<bool> SendNotificationAsync(SendNotificationRequestDto request, CancellationToken cancellationToken = default)
    {
        var channel = (request.Channel ?? string.Empty).Trim();
        if (channel.Equals("email", StringComparison.OrdinalIgnoreCase))
        {
            return await SendEmailAsync(request.UserId, request.Subject, request.Body, cancellationToken);
        }
        if (channel.Equals("sms", StringComparison.OrdinalIgnoreCase))
        {
            return await SendSmsAsync(request.UserId, request.Body, cancellationToken);
        }

        if (await SendEmailAsync(request.UserId, request.Subject, request.Body, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        return await SendSmsAsync(request.UserId, request.Body, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SendEmailAsync(Guid userId, string subject, string body, CancellationToken cancellationToken = default)
    {
        var preferences = await _preferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        if (preferences?.EmailEnabled != true || string.IsNullOrWhiteSpace(preferences.EmailAddress))
        {
            return false;
        }

        var rate = $"{userId}:email:manual";
        if (!await _sendPolicy.TryAcquireAsync(string.Empty, rate, cancellationToken))
        {
            return false;
        }

        return await SendEmailInternalAsync(userId, "Email", preferences.EmailAddress, subject, body, cancellationToken);
    }

    public async Task<bool> SendSmsAsync(Guid userId, string message, CancellationToken cancellationToken = default)
    {
        var preferences = await _preferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        if (preferences?.SmsEnabled != true || string.IsNullOrWhiteSpace(preferences.PhoneNumber))
        {
            return false;
        }

        var rate = $"{userId}:sms:manual";
        if (!await _sendPolicy.TryAcquireAsync(string.Empty, rate, cancellationToken))
        {
            return false;
        }

        return await SendSmsInternalAsync(userId, "SMS", preferences.PhoneNumber, message, cancellationToken);
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
        await _notificationRepository.AddAsync(notification, cancellationToken);

        try
        {
            var success = await _emailProvider.SendAsync(recipient, subject, body, cancellationToken);
            if (success)
            {
                notification.MarkAsSent();
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
                _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
                return true;
            }

            notification.MarkAsFailed("Provider returned false");
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            return false;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ex.Message);
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
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
        await _notificationRepository.AddAsync(notification, cancellationToken);

        try
        {
            var success = await _smsProvider.SendAsync(recipient, body, cancellationToken);
            if (success)
            {
                notification.MarkAsSent();
                await _notificationRepository.UpdateAsync(notification, cancellationToken);
                _logger.LogInformation("SMS sent successfully to {Recipient}", recipient);
                return true;
            }

            notification.MarkAsFailed("Provider returned false");
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            return false;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ex.Message);
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            _logger.LogError(ex, "Failed to send SMS to {Recipient}", recipient);
            return false;
        }
    }

    public Task<NotificationPreference?> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _preferenceRepository.GetByUserIdAsync(userId, cancellationToken);

    public async Task UpdateUserPreferencesAsync(UpdatePreferencesDto preferences, CancellationToken cancellationToken = default)
    {
        var existing = await _preferenceRepository.GetByUserIdAsync(preferences.UserId, cancellationToken);
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
            await _preferenceRepository.AddAsync(newPreference, cancellationToken);
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
            await _preferenceRepository.UpdateAsync(existing, cancellationToken);
        }
    }
}
