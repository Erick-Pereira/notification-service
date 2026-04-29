using Simcag.NotificationService.Application.Services;
using Simcag.NotificationService.Application.Workers;
using Simcag.NotificationService.Infrastructure.DependencyInjection;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Extensions;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

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
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "SIMC-AG Service", Version = "v1" });
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

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHealthChecks();

var rabbitMqOptions = new RabbitMqOptions
{
    Host = GetEnv("RABBITMQ__HOST", "RABBITMQ_HOST") ?? "localhost",
    Port = int.Parse(GetEnv("RABBITMQ__PORT", "RABBITMQ_PORT") ?? "5672"),
    UserName = GetEnv("RABBITMQ__USERNAME", "RABBITMQ_USERNAME") ?? "guest",
    Password = GetEnv("RABBITMQ__PASSWORD", "RABBITMQ_PASSWORD") ?? "guest",
    VirtualHost = GetEnv("RABBITMQ__VIRTUALHOST", "RABBITMQ_VIRTUALHOST") ?? "/"
};

builder.Services.AddRabbitMqMessaging(rabbitMqOptions);

var triggeredQueue = GetEnv("RABBITMQ_QUEUE_ALERT_TRIGGERED", "RABBITMQ__QUEUE_ALERT_TRIGGERED")
    ?? "alert-triggered-events";
var createdQueue = GetEnv("RABBITMQ_QUEUE_ALERT_CREATED", "RABBITMQ__QUEUE_ALERT_CREATED")
    ?? "alerts";

builder.Services.AddRabbitMqEventConsumer<AlertTriggeredEvent>(triggeredQueue);
builder.Services.AddRabbitMqEventConsumer<AlertCreatedEvent>(createdQueue);

builder.Services.AddHostedService<AlertTriggeredEventConsumer>();
builder.Services.AddHostedService<AlertCreatedEventConsumer>();

var app = builder.Build();
app.RunNotificationMigrationsOnStartup();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
