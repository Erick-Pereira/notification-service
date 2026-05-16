using Simcag.NotificationService.Application.DTOs;
using Simcag.NotificationService.Domain.Entities;

namespace Simcag.NotificationService.Application.Services;

public interface INotificationService
{
    Task<bool> SendAlertNotificationAsync(AlertNotificationDto alert, CancellationToken cancellationToken = default);
    Task<bool> SendEmailAsync(Guid userId, string subject, string body, CancellationToken cancellationToken = default);
    Task<bool> SendSmsAsync(Guid userId, string message, CancellationToken cancellationToken = default);
    Task<bool> SendNotificationAsync(SendNotificationRequestDto request, CancellationToken cancellationToken = default);
    Task<NotificationPreference?> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateUserPreferencesAsync(UpdatePreferencesDto preferences, CancellationToken cancellationToken = default);

    Task<NotificationDeliveryPageDto> ListDeliveriesAsync(
        Guid userId,
        string? status,
        string? channel,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<NotificationDashboardDto> GetOperationalDashboardAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> RetryDeliveryAsync(Guid deliveryId, Guid userId, CancellationToken cancellationToken = default);

    NotificationGovernanceDto GetGovernanceCatalog();

    IReadOnlyList<NotificationTemplateDto> GetTemplates();
}
