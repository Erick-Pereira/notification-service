using Microsoft.AspNetCore.Mvc;
using Simcag.NotificationService.Application.DTOs;
using Simcag.NotificationService.Application.Services;
using Simcag.Shared.Contracts;

namespace Simcag.NotificationService.Api.Controllers;

[ApiController]
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
    public async Task<ApiResponse<PreferencesResponseDto>> GetPreferences(Guid userId, CancellationToken ct)
    {
        var preferences = await _notificationService.GetUserPreferencesAsync(userId, ct);
        if (preferences == null)
        {
            return ApiResponse<PreferencesResponseDto>.Ok(new PreferencesResponseDto());
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
            MinimumSeverity = preferences.MinimumSeverity
        });
    }

    [HttpGet("preferences")]
    public async Task<ApiResponse<PreferencesResponseDto>> GetPreferencesByQuery(
        [FromQuery] Guid userId,
        CancellationToken ct = default) =>
        await GetPreferences(userId, ct);

    [HttpPut("preferences")]
    public async Task<ApiResponse<bool>> UpdatePreferences([FromBody] UpdatePreferencesDto preferences, CancellationToken ct)
    {
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
