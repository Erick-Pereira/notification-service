namespace Simcag.NotificationService.Application.DTOs;

public class AlertNotificationDto
{
    public Guid UserId { get; init; }
    public string? AlertId { get; init; }
    public string? ProductName { get; init; } = string.Empty;
    public string? ProductId { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string? AlertCategory { get; init; }
    public string? Message { get; init; }
    public string? Severity { get; init; }
    public Guid? TenantId { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal PriceChange { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string? CorrelationId { get; init; }
}

public class UpdatePreferencesDto
{
    public Guid UserId { get; init; }
    public bool EmailEnabled { get; init; }
    public bool SmsEnabled { get; init; }
    public string? EmailAddress { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? AlertDropEnabled { get; init; }
    public bool? AlertRiseEnabled { get; init; }
    public bool? AlertTrendEnabled { get; init; }
    public string? MinimumSeverity { get; init; }
    public DateTime? MuteAllUntilUtc { get; init; }
    public DateTime? SnoozePriceAlertsUntilUtc { get; init; }
    /// <summary>Quando true, aplica mute/snooze (use null em ambas as datas para limpar).</summary>
    public bool ApplyMuteSnooze { get; init; }
}

public class SendNotificationRequestDto
{
    public Guid UserId { get; init; }
    /// <summary>Se vazio, envia e-mail (se ativo) e em seguida SMS se e-mail indisponível (fallback).</summary>
    public string? Channel { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

public class PreferencesResponseDto
{
    public Guid UserId { get; init; }
    public bool EmailEnabled { get; init; }
    public bool SmsEnabled { get; init; }
    public string? EmailAddress { get; init; }
    public string? PhoneNumber { get; init; }
    public bool AlertDropEnabled { get; init; }
    public bool AlertRiseEnabled { get; init; }
    public bool AlertTrendEnabled { get; init; }
    public string MinimumSeverity { get; init; } = "Info";
    public DateTime? MuteAllUntilUtc { get; init; }
    public DateTime? SnoozePriceAlertsUntilUtc { get; init; }
}

public sealed class NotificationDeliveryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? Severity { get; set; }
    public Guid? TenantId { get; set; }
    public string? AlertId { get; set; }
    public string? ContextJson { get; set; }
    public string? PayloadSummary { get; set; }
    public string? OperationalLink { get; set; }
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class NotificationDeliveryPageDto
{
    public IReadOnlyList<NotificationDeliveryDto> Items { get; set; } = Array.Empty<NotificationDeliveryDto>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class NotificationDashboardDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Suppressed { get; set; }
    public int Filtered { get; set; }
}

public sealed class NotificationGovernanceChannelDto
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class NotificationGovernancePolicyDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class NotificationGovernanceDto
{
    public IReadOnlyList<NotificationGovernanceChannelDto> Channels { get; set; } = Array.Empty<NotificationGovernanceChannelDto>();
    public IReadOnlyList<NotificationGovernancePolicyDto> Policies { get; set; } = Array.Empty<NotificationGovernancePolicyDto>();
    public IReadOnlyList<string> OperationalNotes { get; set; } = Array.Empty<string>();
}

public sealed class NotificationTemplateDto
{
    public string Code { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string SubjectPattern { get; set; } = string.Empty;
    public string BodyPattern { get; set; } = string.Empty;
    public string SourceEvent { get; set; } = string.Empty;
}
