using Simcag.NotificationService.Domain.Entities;
using Xunit;

namespace Simcag.NotificationService.Tests;

public class NotificationEntityTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsNotification()
    {
        var userId = Guid.NewGuid();

        var notification = Notification.Create(userId, "Email", "Email", "test@example.com", "Subject", "Body");

        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal("Email", notification.Type);
        Assert.Equal("Email", notification.Channel);
        Assert.Equal("test@example.com", notification.Recipient);
        Assert.Equal("Subject", notification.Subject);
        Assert.Equal("Body", notification.Body);
        Assert.Equal("Pending", notification.Status);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(Guid.Empty, "Email", "Email", "test@example.com", "Subject", "Body"));
    }

    [Fact]
    public void Create_WithEmptyChannel_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(Guid.NewGuid(), "Email", "", "test@example.com", "Subject", "Body"));
    }

    [Fact]
    public void Create_WithEmptyRecipient_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(Guid.NewGuid(), "Email", "Email", "", "Subject", "Body"));
    }

    [Fact]
    public void MarkAsSent_UpdatesStatusAndSentAt()
    {
        var notification = Notification.Create(Guid.NewGuid(), "Email", "Email", "test@example.com", "Subject", "Body");

        notification.MarkAsSent();

        Assert.Equal("Sent", notification.Status);
        Assert.NotNull(notification.SentAt);
    }

    [Fact]
    public void MarkAsFailed_UpdatesStatusAndErrorMessage()
    {
        var notification = Notification.Create(Guid.NewGuid(), "Email", "Email", "test@example.com", "Subject", "Body");

        notification.MarkAsFailed("Test error");

        Assert.Equal("Failed", notification.Status);
        Assert.Equal("Test error", notification.ErrorMessage);
    }
}

public class NotificationPreferenceEntityTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsPreference()
    {
        var userId = Guid.NewGuid();

        var preference = NotificationPreference.Create(userId, "test@example.com", "+1234567890");

        Assert.NotEqual(Guid.Empty, preference.Id);
        Assert.Equal(userId, preference.UserId);
        Assert.Equal("test@example.com", preference.EmailAddress);
        Assert.Equal("+1234567890", preference.PhoneNumber);
        Assert.True(preference.EmailEnabled);
        Assert.True(preference.SmsEnabled);
        Assert.True(preference.AlertDropEnabled);
        Assert.True(preference.AlertRiseEnabled);
        Assert.True(preference.AlertTrendEnabled);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            NotificationPreference.Create(Guid.Empty, "test@example.com", "+1234567890"));
    }

    [Fact]
    public void Create_WithNoContact_SetsDisabledChannels()
    {
        var preference = NotificationPreference.Create(Guid.NewGuid(), null, null);

        Assert.False(preference.EmailEnabled);
        Assert.False(preference.SmsEnabled);
    }

    [Fact]
    public void UpdatePreferences_UpdatesFields()
    {
        var preference = NotificationPreference.Create(Guid.NewGuid(), null, null);

        preference.UpdatePreferences(true, true, "new@example.com", "+9876543210");

        Assert.True(preference.EmailEnabled);
        Assert.True(preference.SmsEnabled);
        Assert.Equal("new@example.com", preference.EmailAddress);
        Assert.Equal("+9876543210", preference.PhoneNumber);
    }

    [Fact]
    public void UpdateAlertPreferences_UpdatesAlertFlags()
    {
        var preference = NotificationPreference.Create(Guid.NewGuid(), null, null);

        preference.UpdateAlertPreferences(false, true, false);

        Assert.False(preference.AlertDropEnabled);
        Assert.True(preference.AlertRiseEnabled);
        Assert.False(preference.AlertTrendEnabled);
    }
}