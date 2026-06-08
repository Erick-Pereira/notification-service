using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Simcag.NotificationService.Application.DTOs;
using Simcag.NotificationService.Application.Security;
using Simcag.NotificationService.Application.Services;
using Simcag.Shared.Contracts;

namespace Simcag.NotificationService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
[Route("notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet("preferences/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<PreferencesResponseDto>>> GetPreferences(Guid userId, CancellationToken ct)
    {
        if (!NotificationCallerAuthorization.CanAccessUserData(User, userId))
            return Forbid();

        var preferences = await _notificationService.GetUserPreferencesAsync(userId, ct);
        if (preferences == null)
        {
            return ApiResponse<PreferencesResponseDto>.Ok(new PreferencesResponseDto { UserId = userId });
        }

        return ApiResponse<PreferencesResponseDto>.Ok(new PreferencesResponseDto
        {
            UserId = preferences.UserId,
            EmailEnabled = preferences.EmailEnabled,
            SmsEnabled = preferences.SmsEnabled,
            EmailAddress = preferences.EmailAddress,
            PhoneNumber = preferences.PhoneNumber,
            AlertDropEnabled = preferences.AlertDropEnabled,
            AlertRiseEnabled = preferences.AlertRiseEnabled,
            AlertTrendEnabled = preferences.AlertTrendEnabled,
            MinimumSeverity = preferences.MinimumSeverity,
            MuteAllUntilUtc = preferences.MuteAllUntilUtc,
            SnoozePriceAlertsUntilUtc = preferences.SnoozePriceAlertsUntilUtc,
        });
    }

    [HttpGet("governance")]
    public ApiResponse<NotificationGovernanceDto> Governance() =>
        ApiResponse<NotificationGovernanceDto>.Ok(_notificationService.GetGovernanceCatalog());

    [HttpGet("templates")]
    public ApiResponse<IReadOnlyList<NotificationTemplateDto>> Templates() =>
        ApiResponse<IReadOnlyList<NotificationTemplateDto>>.Ok(_notificationService.GetTemplates());

    [HttpGet("operational/dashboard")]
    public async Task<ActionResult<ApiResponse<NotificationDashboardDto>>> OperationalDashboard([FromQuery] Guid userId, CancellationToken ct)
    {
        if (!NotificationCallerAuthorization.CanAccessUserData(User, userId))
            return Forbid();

        var d = await _notificationService.GetOperationalDashboardAsync(userId, ct);
        return ApiResponse<NotificationDashboardDto>.Ok(d);
    }

    [HttpGet("deliveries")]
    public async Task<ActionResult<ApiResponse<NotificationDeliveryPageDto>>> Deliveries(
        [FromQuery] Guid userId,
        [FromQuery] string? status,
        [FromQuery] string? channel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!NotificationCallerAuthorization.CanAccessUserData(User, userId))
            return Forbid();

        var d = await _notificationService.ListDeliveriesAsync(userId, status, channel, page, pageSize, ct);
        return ApiResponse<NotificationDeliveryPageDto>.Ok(d);
    }

    [HttpPost("deliveries/{id:guid}/retry")]
    public async Task<ActionResult<ApiResponse<bool>>> RetryDelivery([FromRoute] Guid id, [FromQuery] Guid userId, CancellationToken ct)
    {
        if (!NotificationCallerAuthorization.CanAccessUserData(User, userId))
            return Forbid();

        var ok = await _notificationService.RetryDeliveryAsync(id, userId, ct);
        return ApiResponse<bool>.Ok(ok);
    }

    [HttpGet("preferences")]
    public Task<ActionResult<ApiResponse<PreferencesResponseDto>>> GetPreferencesByQuery(
        [FromQuery] Guid userId,
        CancellationToken ct = default) =>
        GetPreferences(userId, ct);

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePreferences([FromBody] UpdatePreferencesDto preferences, CancellationToken ct)
    {
        if (!NotificationCallerAuthorization.CanAccessUserData(User, preferences.UserId))
            return Forbid();

        await _notificationService.UpdateUserPreferencesAsync(preferences, ct);
        return ApiResponse<bool>.Ok(true);
    }

    [HttpPost("send")]
    public async Task<ApiResponse<bool>> Send([FromBody] SendNotificationRequestDto request, CancellationToken ct)
    {
        _logger.LogInformation("Envio unificado (send) para o usuário {UserId}", request.UserId);
        var success = await _notificationService.SendNotificationAsync(request, ct);
        return ApiResponse<bool>.Ok(success);
    }

    [HttpPost("email")]
    public async Task<ApiResponse<bool>> SendEmail([FromBody] SendEmailRequest request, CancellationToken ct)
    {
        var success = await _notificationService.SendEmailAsync(request.UserId, request.Subject, request.Body, ct);
        return ApiResponse<bool>.Ok(success);
    }

    [HttpPost("sms")]
    public async Task<ApiResponse<bool>> SendSms([FromBody] SendSmsRequest request, CancellationToken ct)
    {
        var success = await _notificationService.SendSmsAsync(request.UserId, request.Message, ct);
        return ApiResponse<bool>.Ok(success);
    }
}

public class SendEmailRequest
{
    public Guid UserId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

public class SendSmsRequest
{
    public Guid UserId { get; init; }
    public string Message { get; init; } = string.Empty;
}
