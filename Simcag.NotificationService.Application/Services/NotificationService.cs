using Simcag.NotificationService.Domain.Entities;
using Simcag.NotificationService.Domain.Interfaces;
using Simcag.NotificationService.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Simcag.NotificationService.Application.Services;

public interface INotificationService
{
    Task<bool> SendAlertNotificationAsync(AlertNotificationDto alert, CancellationToken ct = default);
    Task<bool> SendEmailAsync(Guid userId, string subject, string body, CancellationToken ct = default);
    Task<bool> SendSmsAsync(Guid userId, string message, CancellationToken ct = default);
    Task<NotificationPreference?> GetUserPreferencesAsync(Guid userId, CancellationToken ct);
    Task UpdateUserPreferencesAsync(UpdatePreferencesDto preferences, CancellationToken ct);
}

public class NotificationService : INotificationService
{
    private readonly IEmailProvider _emailProvider;
    private readonly ISmsProvider _smsProvider;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailProvider emailProvider,
        ISmsProvider smsProvider,
        INotificationPreferenceRepository preferenceRepository,
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger)
    {
        _emailProvider = emailProvider;
        _smsProvider = smsProvider;
        _preferenceRepository = preferenceRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<bool> SendAlertNotificationAsync(AlertNotificationDto alert, CancellationToken ct = default)
    {
        var preferences = await _preferenceRepository.GetByUserIdAsync(alert.UserId, ct);
        if (preferences == null)
        {
            _logger.LogWarning("No preferences found for user {UserId}", alert.UserId);
            return false;
        }

        var alertType = alert.AlertType.ToUpperInvariant();
        var shouldNotify = (alertType == "DROP" && preferences.AlertDropEnabled) ||
                       (alertType == "RISE" && preferences.AlertRiseEnabled) ||
                       (alertType == "TREND" && preferences.AlertTrendEnabled);

        if (!shouldNotify)
        {
            _logger.LogInformation("Alert type {AlertType} notifications disabled for user {UserId}", alert.AlertType, alert.UserId);
            return false;
        }

        var subject = $"Price Alert: {alert.AlertType} - {alert.ProductName}";
        var body = $"""
            Price Alert Notification
            ---------------------
            Product: {alert.ProductName}
            Alert Type: {alert.AlertType}
            Current Price: {alert.CurrentPrice:C}
            Change: {alert.PriceChange:P2}
            Source: {alert.Source}
            Time: {alert.OccurredAt:u}
            """;

        var success = true;

        if (preferences.EmailEnabled && !string.IsNullOrWhiteSpace(preferences.EmailAddress))
        {
            success = await SendEmailInternalAsync(alert.UserId, "Email", preferences.EmailAddress, subject, body, ct) && success;
        }

        if (preferences.SmsEnabled && !string.IsNullOrWhiteSpace(preferences.PhoneNumber))
        {
            var smsBody = $"[{alert.AlertType}] {alert.ProductName}: {alert.CurrentPrice:C} ({alert.PriceChange:P0} from {alert.Source})";
            success = await SendSmsInternalAsync(alert.UserId, "SMS", preferences.PhoneNumber, smsBody, ct) && success;
        }

        return success;
    }

    public async Task<bool> SendEmailAsync(Guid userId, string subject, string body, CancellationToken ct = default)
    {
        var preferences = await _preferenceRepository.GetByUserIdAsync(userId, ct);
        if (preferences?.EmailEnabled != true || string.IsNullOrWhiteSpace(preferences.EmailAddress))
            return false;

        return await SendEmailInternalAsync(userId, "Email", preferences.EmailAddress, subject, body, ct);
    }

    public async Task<bool> SendSmsAsync(Guid userId, string message, CancellationToken ct = default)
    {
        var preferences = await _preferenceRepository.GetByUserIdAsync(userId, ct);
        if (preferences?.SmsEnabled != true || string.IsNullOrWhiteSpace(preferences.PhoneNumber))
            return false;

        return await SendSmsInternalAsync(userId, "SMS", preferences.PhoneNumber, message, ct);
    }

    private async Task<bool> SendEmailInternalAsync(Guid userId, string type, string recipient, string subject, string body, CancellationToken ct)
    {
        var notification = Notification.Create(userId, type, "Email", recipient, subject, body);
        await _notificationRepository.AddAsync(notification, ct);

        try
        {
            var success = await _emailProvider.SendAsync(recipient, subject, body, ct);
            if (success)
            {
                notification.MarkAsSent();
                await _notificationRepository.UpdateAsync(notification, ct);
                _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
                return true;
            }

            notification.MarkAsFailed("Provider returned false");
            await _notificationRepository.UpdateAsync(notification, ct);
            return false;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ex.Message);
            await _notificationRepository.UpdateAsync(notification, ct);
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
            return false;
        }
    }

    private async Task<bool> SendSmsInternalAsync(Guid userId, string type, string recipient, string body, CancellationToken ct)
    {
        var notification = Notification.Create(userId, type, "SMS", recipient, string.Empty, body);
        await _notificationRepository.AddAsync(notification, ct);

        try
        {
            var success = await _smsProvider.SendAsync(recipient, body, ct);
            if (success)
            {
                notification.MarkAsSent();
                await _notificationRepository.UpdateAsync(notification, ct);
                _logger.LogInformation("SMS sent successfully to {Recipient}", recipient);
                return true;
            }

            notification.MarkAsFailed("Provider returned false");
            await _notificationRepository.UpdateAsync(notification, ct);
            return false;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ex.Message);
            await _notificationRepository.UpdateAsync(notification, ct);
            _logger.LogError(ex, "Failed to send SMS to {Recipient}", recipient);
            return false;
        }
    }

    public async Task<NotificationPreference?> GetUserPreferencesAsync(Guid userId, CancellationToken ct)
        => await _preferenceRepository.GetByUserIdAsync(userId, ct);

    public async Task UpdateUserPreferencesAsync(UpdatePreferencesDto preferences, CancellationToken ct)
    {
        var existing = await _preferenceRepository.GetByUserIdAsync(preferences.UserId, ct);
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
            if (preferences.AlertDropEnabled.HasValue || preferences.AlertRiseEnabled.HasValue || preferences.AlertTrendEnabled.HasValue)
            {
                newPreference.UpdateAlertPreferences(
                    preferences.AlertDropEnabled ?? true,
                    preferences.AlertRiseEnabled ?? true,
                    preferences.AlertTrendEnabled ?? true);
            }
            await _preferenceRepository.AddAsync(newPreference, ct);
        }
        else
        {
            existing.UpdatePreferences(
                preferences.EmailEnabled,
                preferences.SmsEnabled,
                preferences.EmailAddress,
                preferences.PhoneNumber);
            if (preferences.AlertDropEnabled.HasValue || preferences.AlertRiseEnabled.HasValue || preferences.AlertTrendEnabled.HasValue)
            {
                existing.UpdateAlertPreferences(
                    preferences.AlertDropEnabled ?? existing.AlertDropEnabled,
                    preferences.AlertRiseEnabled ?? existing.AlertRiseEnabled,
                    preferences.AlertTrendEnabled ?? existing.AlertTrendEnabled);
            }
            await _preferenceRepository.UpdateAsync(existing, ct);
        }
    }
}