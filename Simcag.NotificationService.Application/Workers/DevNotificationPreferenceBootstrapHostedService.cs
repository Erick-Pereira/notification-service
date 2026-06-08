using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simcag.NotificationService.Application.Configuration;
using Simcag.NotificationService.Domain.Entities;
using Simcag.NotificationService.Domain.Interfaces;

namespace Simcag.NotificationService.Application.Workers;

/// <summary>
/// Garante preferências mínimas para o utilizador padrão de alertas quando configurado.
/// </summary>
public sealed class DevNotificationPreferenceBootstrapHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationRecipientOptions _options;
    private readonly ILogger<DevNotificationPreferenceBootstrapHostedService> _logger;

    public DevNotificationPreferenceBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        NotificationRecipientOptions options,
        ILogger<DevNotificationPreferenceBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.DefaultNotifyUserId is not { } userId || userId == Guid.Empty)
            return;

        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationPreferenceRepository>();
        var existing = await repo.GetByUserIdAsync(userId, stoppingToken);
        if (existing is not null)
            return;

        var email = string.IsNullOrWhiteSpace(_options.DevFallbackEmail)
            ? $"dev+{userId:N}@simcag.local"
            : _options.DevFallbackEmail.Trim();

        var pref = NotificationPreference.Create(userId, email, phoneNumber: null);
        pref.UpdatePreferences(emailEnabled: true, smsEnabled: false, emailAddress: email, phoneNumber: null);
        pref.UpdateAlertPreferences(dropEnabled: true, riseEnabled: true, trendEnabled: true);
        pref.UpdateMinimumSeverity("Low");

        await repo.AddAsync(pref, stoppingToken);
        _logger.LogInformation(
            "Preferências de notificação criadas para dev user {UserId} ({Email})",
            userId,
            email);
    }
}
