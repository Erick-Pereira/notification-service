using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simcag.NotificationService.Application.Services;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging.Contracts;
using Simcag.Shared.Messaging.Telemetry;

namespace Simcag.NotificationService.Application.Workers;

public sealed class AlertTriggeredEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertTriggeredEventConsumer> _logger;
    private readonly IEventConsumer<AlertTriggeredEvent> _eventConsumer;

    public AlertTriggeredEventConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<AlertTriggeredEventConsumer> logger,
        IEventConsumer<AlertTriggeredEvent> eventConsumer)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _eventConsumer = eventConsumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting {Consumer} (alert-triggered-events)", nameof(AlertTriggeredEventConsumer));
        await foreach (var messageEnvelope in _eventConsumer.ReadMessagesAsync(stoppingToken))
        {
            using (MessagingConsumeTelemetry.BeginConsume(messageEnvelope, out _))
            {
            using var scope = _scopeFactory.CreateScope();
            var dispatch = scope.ServiceProvider.GetRequiredService<AlertNotificationDispatchService>();
            var alert = messageEnvelope.Data;

            _logger.LogInformation(
                "Processing AlertTriggeredEvent alertId={AlertId} productId={ProductId}",
                alert.AlertId,
                alert.ProductId);

            try
            {
                await dispatch.DispatchTriggeredAsync(
                    alert,
                    messageEnvelope.TenantId,
                    messageEnvelope.CorrelationId,
                    stoppingToken);
                await _eventConsumer.AcknowledgeMessageAsync(messageEnvelope, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar AlertTriggeredEvent para o alerta {AlertId}", alert.AlertId);
                await _eventConsumer.RejectMessageAsync(messageEnvelope, stoppingToken);
            }
            }
        }

        _logger.LogInformation("Consumer {Name} finalizado", nameof(AlertTriggeredEventConsumer));
    }
}
