using Moq;
using Xunit;
using Simcag.NotificationService.Application.DTOs;
using Simcag.NotificationService.Domain.Entities;
using Simcag.NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Simcag.NotificationService.Application.Abstractions;
using AppNotificationService = Simcag.NotificationService.Application.Services.NotificationService;
using AppNotificationServiceInterface = Simcag.NotificationService.Application.Services.INotificationService;

namespace Simcag.NotificationService.Tests;

public class NotificationServiceTests
{
    private readonly Mock<IEmailProvider> _emailProviderMock;
    private readonly Mock<ISmsProvider> _smsProviderMock;
    private readonly Mock<INotificationPreferenceRepository> _preferenceRepoMock;
    private readonly Mock<INotificationRepository> _notificationRepoMock;
    private readonly Mock<INotificationSendPolicy> _sendPolicyMock;
    private readonly Mock<ILogger<AppNotificationService>> _loggerMock;
    private readonly AppNotificationServiceInterface _service;

    public NotificationServiceTests()
    {
        _emailProviderMock = new Mock<IEmailProvider>();
        _smsProviderMock = new Mock<ISmsProvider>();
        _preferenceRepoMock = new Mock<INotificationPreferenceRepository>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _sendPolicyMock = new Mock<INotificationSendPolicy>();
        _sendPolicyMock
            .Setup(p => p.TryAcquireAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _loggerMock = new Mock<ILogger<AppNotificationService>>();
        _service = new AppNotificationService(
            _emailProviderMock.Object,
            _smsProviderMock.Object,
            _preferenceRepoMock.Object,
            _notificationRepoMock.Object,
            _sendPolicyMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_WithNoPreferences_ReturnsFalse()
    {
        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var alert = CreateAlertNotification();

        var result = await _service.SendAlertNotificationAsync(alert);

        Assert.False(result);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_WithDisabledAlertType_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, "test@example.com", "+1234567890");
        preference.UpdateAlertPreferences(dropEnabled: false, riseEnabled: true, trendEnabled: true);

        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var alert = CreateAlertNotification(userId, "DROP");

        var result = await _service.SendAlertNotificationAsync(alert);

        Assert.False(result);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_WhenSeverityBelowMinimum_DoesNotSend()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, "a@b.com", null);
        preference.UpdateMinimumSeverity("Critical");
        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var alert = CreateAlertNotification(userId, "DROP", severity: "Info");

        var result = await _service.SendAlertNotificationAsync(alert);

        Assert.False(result);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_WithEmailEnabled_SendsEmail()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, "test@example.com", null);

        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _emailProviderMock.Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var alert = CreateAlertNotification(userId);

        var result = await _service.SendAlertNotificationAsync(alert);

        Assert.True(result);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailProviderMock.Verify(p => p.SendAsync("test@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_WithSmsEnabled_SendsSms()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, null, "+1234567890");

        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _smsProviderMock.Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var alert = CreateAlertNotification(userId);

        var result = await _service.SendAlertNotificationAsync(alert);

        Assert.True(result);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _smsProviderMock.Verify(p => p.SendAsync("+1234567890", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_EmailFails_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, "test@example.com", null);

        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);
        _emailProviderMock.Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var alert = CreateAlertNotification(userId);

        var result = await _service.SendAlertNotificationAsync(alert);

        Assert.False(result);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_WithNoEmailPreference_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var result = await _service.SendEmailAsync(userId, "Subject", "Body");

        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_WithDisabledEmail_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, null, null);

        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var result = await _service.SendEmailAsync(userId, "Subject", "Body");

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_CreatesNewPreference()
    {
        var userId = Guid.NewGuid();
        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        var dto = new UpdatePreferencesDto
        {
            UserId = userId,
            EmailEnabled = true,
            SmsEnabled = false,
            EmailAddress = "new@example.com"
        };

        await _service.UpdateUserPreferencesAsync(dto, CancellationToken.None);

        _preferenceRepoMock.Verify(r => r.AddAsync(It.Is<NotificationPreference>(p => p.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_UpdatesExistingPreference()
    {
        var userId = Guid.NewGuid();
        var existing = NotificationPreference.Create(userId, "old@example.com", "+1234567890");
        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var dto = new UpdatePreferencesDto
        {
            UserId = userId,
            EmailEnabled = true,
            SmsEnabled = true,
            EmailAddress = "new@example.com",
            PhoneNumber = "+1234567890",
        };

        await _service.UpdateUserPreferencesAsync(dto, CancellationToken.None);

        _preferenceRepoMock.Verify(r => r.UpdateAsync(It.Is<NotificationPreference>(p => p.EmailAddress == "new@example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAlertNotificationAsync_WithNoActiveChannels_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, "test@example.com", "+1234567890");
        preference.UpdatePreferences(emailEnabled: false, smsEnabled: false, emailAddress: "test@example.com", phoneNumber: "+1234567890");

        _preferenceRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var alert = CreateAlertNotification(userId);

        var result = await _service.SendAlertNotificationAsync(alert);

        Assert.False(result);
        _notificationRepoMock.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_RejectsAllChannelsDisabled()
    {
        var dto = new UpdatePreferencesDto
        {
            UserId = Guid.NewGuid(),
            EmailEnabled = false,
            SmsEnabled = false,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateUserPreferencesAsync(dto, CancellationToken.None));
    }

    private static AlertNotificationDto CreateAlertNotification(
        Guid? userId = null,
        string alertType = "DROP",
        string? severity = "Warning")
    {
        return new AlertNotificationDto
        {
            UserId = userId ?? Guid.NewGuid(),
            ProductName = "Test Product",
            AlertType = alertType,
            Severity = severity,
            CurrentPrice = 100.00m,
            PriceChange = 0.05m,
            Source = "TestSource",
            OccurredAt = DateTime.UtcNow
        };
    }
}