using Simcag.NotificationService.Application.Branding;
using Simcag.NotificationService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Net.Http.Headers;
using System.Text;

namespace Simcag.NotificationService.Infrastructure.Providers;

public class SmtpEmailProvider : IEmailProvider
{
    private readonly ILogger<SmtpEmailProvider> _logger;
    private readonly SmtpSettings _settings;

    private static string? FirstNonEmpty(params string[] keys)
    {
        foreach (var k in keys)
        {
            var v = Environment.GetEnvironmentVariable(k);
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }
        return null;
    }

    public SmtpEmailProvider(ILogger<SmtpEmailProvider> logger)
    {
        _logger = logger;
        var port = int.Parse(FirstNonEmpty("SMTP__PORT", "SMTP_PORT") ?? "587");
        var enableSsl = bool.Parse(FirstNonEmpty("SMTP__ENABLESSL", "SMTP_ENABLESSL") ?? "true");
        // Mailpit and other local capture servers on 1025 use plain SMTP (no STARTTLS).
        if (port == 1025 && enableSsl)
        {
            logger.LogWarning("SMTP port 1025 does not support STARTTLS; using EnableSsl=false (Mailpit/local capture)");
            enableSsl = false;
        }

        _settings = new SmtpSettings
        {
            Host = FirstNonEmpty("SMTP__HOST", "SMTP_HOST") ?? "smtp.gmail.com",
            Port = port,
            FromAddress = FirstNonEmpty("SMTP__FROM", "SMTP_FROM") ?? "",
            FromDisplayName = NotificationBranding.EmailSenderDisplayName,
            UserName = FirstNonEmpty("SMTP__USERNAME", "SMTP_USER") ?? "",
            Password = FirstNonEmpty("SMTP__PASSWORD", "SMTP_PASS", "SMTP__PASS") ?? "",
            EnableSsl = enableSsl
        };
    }

    public async Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.UserName))
        {
            _logger.LogWarning("SMTP not configured, skipping email");
            return true;
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
            };

            var fromAddress = string.IsNullOrWhiteSpace(_settings.FromAddress)
                ? _settings.UserName
                : _settings.FromAddress;

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, _settings.FromDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(to));

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    private class SmtpSettings
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; }
        public string FromAddress { get; init; } = string.Empty;
        public string FromDisplayName { get; init; } = NotificationBranding.ProductName;
        public string UserName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public bool EnableSsl { get; init; }
    }
}

public class TwilioSmsProvider : ISmsProvider
{
    private readonly ILogger<TwilioSmsProvider> _logger;
    private readonly TwilioSettings _settings;

    private static string? FirstNonEmpty(params string[] keys)
    {
        foreach (var k in keys)
        {
            var v = Environment.GetEnvironmentVariable(k);
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }
        return null;
    }

    public TwilioSmsProvider(ILogger<TwilioSmsProvider> logger)
    {
        _logger = logger;
        _settings = new TwilioSettings
        {
            AccountSid = FirstNonEmpty("TWILIO__ACCOUNTSID", "SMS_PROVIDER_ACCOUNTSID") ?? "",
            AuthToken = FirstNonEmpty("TWILIO__AUTHTOKEN", "SMS_PROVIDER_API_KEY", "SMS_PROVIDER_AUTHTOKEN") ?? "",
            FromNumber = FirstNonEmpty("TWILIO__FROMNUMBER", "SMS_PROVIDER_FROM") ?? ""
        };
    }

    public async Task<bool> SendAsync(string to, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccountSid) || string.IsNullOrWhiteSpace(_settings.AuthToken))
        {
            _logger.LogWarning("Twilio not configured, skipping SMS");
            return true;
        }

        try
        {
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Messages.json";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(_settings.AccountSid, _settings.AuthToken);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = to,
                ["From"] = _settings.FromNumber,
                ["Body"] = message
            });

            var response = await client.PostAsync(url, content, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent to {To}", to);
                return true;
            }

            _logger.LogWarning("Twilio API error: {Response}", responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}", to);
            return false;
        }
    }

    private class TwilioSettings
    {
        public string AccountSid { get; init; } = string.Empty;
        public string AuthToken { get; init; } = string.Empty;
        public string FromNumber { get; init; } = string.Empty;
    }
}

public class BasicAuthenticationHeaderValue : AuthenticationHeaderValue
{
    public BasicAuthenticationHeaderValue(string userName, string password)
        : base("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}")))
    {
    }
}