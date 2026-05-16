using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simcag.NotificationService.Application.Services;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging.Contracts;
using Simcag.Shared.Messaging.Telemetry;

namespace Simcag.NotificationService.Application.Workers;

public sealed class AlertCreatedEventConsumer : BackgroundService
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
        _logger.LogInformation("Starting {Consumer} (queue alerts / AlertCreatedEvent)", nameof(AlertCreatedEventConsumer));
        await foreach (var messageEnvelope in _eventConsumer.ReadMessagesAsync(stoppingToken))
        {
            using (MessagingConsumeTelemetry.BeginConsume(messageEnvelope, out _))
            {
            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var alert = messageEnvelope.Data;
            if (alert.UserId is null || alert.UserId == Guid.Empty)
            {
                _logger.LogWarning(
                    "AlertCreatedEvent sem UserId (AlertId {AlertId}). Nada enviado; ack para evitar reprocessar.",
                    alert.AlertId);
                await _eventConsumer.AcknowledgeMessageAsync(messageEnvelope, stoppingToken);
                continue;
            }

            try
            {
                var dto = AlertEventMapping.ToDto(alert, correlationId: messageEnvelope.CorrelationId);
                await notificationService.SendAlertNotificationAsync(dto, stoppingToken);
                await _eventConsumer.AcknowledgeMessageAsync(messageEnvelope, stoppingToken);
                _logger.LogInformation("AlertCreatedEvent processado (AlertId {AlertId}, ProductId {ProductId})", alert.AlertId, alert.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar AlertCreatedEvent (ProductId {ProductId})", alert.ProductId);
                await _eventConsumer.RejectMessageAsync(messageEnvelope, stoppingToken);
            }
            }
        }

        _logger.LogInformation("Consumer {Name} finalizado", nameof(AlertCreatedEventConsumer));
    }
}