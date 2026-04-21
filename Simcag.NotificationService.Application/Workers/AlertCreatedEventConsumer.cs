using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Simcag.NotificationService.Application.Services;
using Simcag.NotificationService.Application.DTOs;
using Simcag.Shared.Messaging.Contracts;
using Simcag.Shared.Events;

namespace Simcag.NotificationService.Application.Workers;

public class AlertCreatedEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertCreatedEventConsumer> _logger;
    private readonly IEventConsumer<AlertCreatedEvent> _eventConsumer;

    public AlertCreatedEventConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<AlertCreatedEventConsumer> logger,
        IEventConsumer<AlertCreatedEvent> eventConsumer)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _eventConsumer = eventConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting AlertCreatedEvent consumer");

        await foreach (var messageEnvelope in _eventConsumer.ReadMessagesAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            try
            {
                var alert = messageEnvelope.Data;

                var alertDto = new AlertNotificationDto
                {
                    UserId = alert.AlertId,
                    ProductName = alert.ProductName,
                    AlertType = alert.AlertType,
                    CurrentPrice = alert.CurrentPrice ?? 0,
                    PriceChange = alert.PriceVariation ?? 0,
                    Source = alert.Source ?? "Unknown",
                    OccurredAt = alert.OccurredAt
                };

                await notificationService.SendAlertNotificationAsync(alertDto, stoppingToken);
                await _eventConsumer.AcknowledgeMessageAsync(messageEnvelope, stoppingToken);
                _logger.LogInformation("Successfully processed AlertCreatedEvent for product {ProductId}",
                    messageEnvelope.Data.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process AlertCreatedEvent for product {ProductId}",
                    messageEnvelope.Data.ProductId);
                await _eventConsumer.RejectMessageAsync(messageEnvelope, stoppingToken);
            }
        }

        _logger.LogInformation("AlertCreatedEvent consumer stopped");
    }
}