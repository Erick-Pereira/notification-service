using Simcag.NotificationService.Application.Abstractions;
using Simcag.NotificationService.Application.Configuration;
using Simcag.NotificationService.Application.Services;
using Simcag.NotificationService.Application.Workers;
using Simcag.NotificationService.Infrastructure.DependencyInjection;
using Simcag.NotificationService.Infrastructure.Identity;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Extensions;
using Simcag.Shared.ErrorHandling;
using Simcag.Shared.Hosting;
using Simcag.Shared.Security;
using Simcag.Shared.Telemetry;

DotNetEnv.Env.NoClobber().Load();
ContainerListenConfiguration.NormalizeAspNetCoreListenUrlsInContainer();
var builder = WebApplication.CreateBuilder(args);
ContainerListenConfiguration.ApplyDockerListenUrls(builder);
builder.AddSimcagDistributedTelemetry("Simcag.NotificationService");
builder.Configuration.AddEnvironmentVariables();
var isTesting = builder.Environment.IsEnvironment("Testing");

static string? GetEnv(params string[] keys)
{
    foreach (var k in keys)
    {
        var v = Environment.GetEnvironmentVariable(k);
        if (!string.IsNullOrWhiteSpace(v))
            return v;
    }
    return null;
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "Econdomiza - Notification", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name        = "Authorization",
        In          = Microsoft.OpenApi.ParameterLocation.Header,
        Type        = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        Description = "Cole apenas o JWT (sem 'Bearer ')."
    });
    c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddNotificationInfrastructure(builder.Configuration);

static NotificationRecipientOptions ReadRecipientOptions()
{
    Guid? defaultUser = null;
    var rawUser = GetEnv("NOTIFICATION__DEFAULT_NOTIFY_USER_ID", "NOTIFICATION_DEFAULT_NOTIFY_USER_ID");
    if (!string.IsNullOrWhiteSpace(rawUser) && Guid.TryParse(rawUser.Trim(), out var parsed) && parsed != Guid.Empty)
        defaultUser = parsed;

    return new NotificationRecipientOptions
    {
        DefaultNotifyUserId = defaultUser,
        DevFallbackEmail = GetEnv("NOTIFICATION__DEV_FALLBACK_EMAIL", "NOTIFICATION_DEV_FALLBACK_EMAIL"),
    };
}

var recipientOptions = ReadRecipientOptions();
builder.Services.AddSingleton(recipientOptions);

if (isTesting)
{
    builder.Services.AddSingleton<IIdentityNotificationRecipientClient, NullIdentityNotificationRecipientClient>();
}
builder.Services.AddSingleton<IAlertNotificationRecipientResolver, AlertNotificationRecipientResolver>();
builder.Services.AddScoped<AlertNotificationDispatchService>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHealthChecks().AddSimcagLiveSelfCheck();

if (!isTesting)
{
var rabbitMqOptions = new RabbitMqOptions
{
    Host = GetEnv("RABBITMQ__HOST", "RABBITMQ_HOST") ?? "localhost",
    Port = int.Parse(GetEnv("RABBITMQ__PORT", "RABBITMQ_PORT") ?? "5672"),
    UserName = GetEnv("RABBITMQ__USERNAME", "RABBITMQ_USERNAME") ?? "guest",
    Password = GetEnv("RABBITMQ__PASSWORD", "RABBITMQ_PASSWORD") ?? "guest",
    VirtualHost = GetEnv("RABBITMQ__VIRTUALHOST", "RABBITMQ_VIRTUALHOST") ?? "/"
};
rabbitMqOptions.ApplyMessageSigningFromEnvironment();

builder.Services.AddRabbitMqMessaging(rabbitMqOptions);
builder.Services.AddRabbitMqRpcClient();
builder.Services.AddSingleton<IIdentityNotificationRecipientClient, IdentityNotificationRecipientRpcClient>();

var triggeredQueue = GetEnv("RABBITMQ_QUEUE_ALERT_TRIGGERED", "RABBITMQ__QUEUE_ALERT_TRIGGERED")
    ?? "alert-triggered-events";

builder.Services.AddRabbitMqEventConsumer<AlertTriggeredEvent>(triggeredQueue);

builder.Services.AddHostedService<AlertTriggeredEventConsumer>();
builder.Services.AddHostedService<DevNotificationPreferenceBootstrapHostedService>();
}

builder.Services.AddSimcagGatewayAuthentication(builder.Environment);

builder.Services.AddSimcagProblemDetails();

var app = builder.Build();

app.ValidateSimcagGatewayTrustAtStartup();

app.UseSimcagExceptionHandler();
app.UseSimcagHttpCorrelationActivityTags();

app.RunNotificationMigrationsOnStartup();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapSimcagHealthChecks();

app.UseSimcagTelemetryEndpoints();

app.Run();

public partial class Program
{
}
