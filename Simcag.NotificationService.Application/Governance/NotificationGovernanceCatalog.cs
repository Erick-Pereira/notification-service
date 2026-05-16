using Simcag.NotificationService.Application.DTOs;

namespace Simcag.NotificationService.Application.Governance;

/// <summary>Catálogo estático de governança (canais, políticas Redis, notas operacionais).</summary>
public static class NotificationGovernanceCatalog
{
    public static NotificationGovernanceDto Build() =>
        new()
        {
            Channels =
            [
                new NotificationGovernanceChannelDto
                {
                    Code = "Email",
                    DisplayName = "E-mail",
                    Description = "SMTP / fornecedor configurado no notification-service.",
                },
                new NotificationGovernanceChannelDto
                {
                    Code = "SMS",
                    DisplayName = "SMS",
                    Description = "SMS via fornecedor configurado; sujeito ao mesmo rate limit por utilizador.",
                },
                new NotificationGovernanceChannelDto
                {
                    Code = "Policy",
                    DisplayName = "Política interna",
                    Description = "Registos sem envio externo (filtrado, deduplicação, mute).",
                },
            ],
            Policies =
            [
                new NotificationGovernancePolicyDto
                {
                    Key = "DedupTtlHours",
                    Value = "24 (default RedisNotificationSendOptions)",
                    Description = "Chave Redis notif:dedup:* evita reenvio do mesmo alerta/canal no período.",
                },
                new NotificationGovernancePolicyDto
                {
                    Key = "MaxSendsPerUserPerHour",
                    Value = "30 (default)",
                    Description = "Contador Redis notif:rl:* por utilizador e tipo de envio.",
                },
                new NotificationGovernancePolicyDto
                {
                    Key = "AlertCooldown",
                    Value = "alert-service Redis (ver EvaluateAlertHandler)",
                    Description = "Supressão de alertas duplicados antes de persistir novo alerta.",
                },
            ],
            OperationalNotes =
            [
                "Cada tentativa de envio gera ou atualiza linha em notifications com estado terminal auditável.",
                "CorrelationId propaga-se do envelope RabbitMQ (MessageEnvelope) quando disponível.",
                "Mute global bloqueia envios externos; o motivo fica registado como Filtered.",
                "Snooze de alertas de preço bloqueia a fila alert-triggered até à data UTC definida.",
            ],
        };

    public static IReadOnlyList<NotificationTemplateDto> Templates() =>
    [
        new NotificationTemplateDto
        {
            Code = "price-alert-email",
            Channel = "Email",
            SubjectPattern = "Price Alert: {AlertType} - {ProductName}",
            BodyPattern = "{Message} + metadados (severidade, hora UTC, preços).",
            SourceEvent = "AlertTriggeredEvent / AlertCreatedEvent",
        },
        new NotificationTemplateDto
        {
            Code = "price-alert-sms",
            Channel = "SMS",
            SubjectPattern = "(vazio)",
            BodyPattern = "[{AlertType}] {Product}: preço e variação truncados.",
            SourceEvent = "AlertTriggeredEvent / AlertCreatedEvent",
        },
    ];
}
