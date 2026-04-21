using Simcag.NotificationService.Application.Services;
using Simcag.NotificationService.Application.Workers;
using Simcag.NotificationService.Domain.Entities;
using Simcag.NotificationService.Domain.Interfaces;
using Simcag.NotificationService.Infrastructure.Providers;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Contracts;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging.Extensions;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IEmailProvider, SmtpEmailProvider>();
builder.Services.AddSingleton<ISmsProvider, TwilioSmsProvider>();
builder.Services.AddSingleton<INotificationRepository, MockNotificationRepository>();
builder.Services.AddSingleton<INotificationPreferenceRepository, MockNotificationPreferenceRepository>();

builder.Services.AddHealthChecks();

// RabbitMQ Configuration
var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ__HOST") ?? "localhost";
var rabbitMqPort = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ__PORT") ?? "5672");
var rabbitMqUserName = Environment.GetEnvironmentVariable("RABBITMQ__USERNAME") ?? "guest";
var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ__PASSWORD") ?? "guest";

var rabbitMqOptions = new RabbitMqOptions
{
    Host = rabbitMqHost,
    Port = rabbitMqPort,
    UserName = rabbitMqUserName,
    Password = rabbitMqPassword,
    VirtualHost = "/"
};

builder.Services.AddRabbitMqMessaging(rabbitMqOptions);
builder.Services.AddRabbitMqEventConsumer<AlertCreatedEvent>("alerts");

// Background Services
builder.Services.AddHostedService<AlertCreatedEventConsumer>();

var app = builder.Build();

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

public class MockNotificationRepository : INotificationRepository
{
    private readonly List<Notification> _notifications = new();
    public Task AddAsync(Notification notification, CancellationToken ct) { _notifications.Add(notification); return Task.CompletedTask; }
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(_notifications.FirstOrDefault(n => n.Id == id));
    public Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int limit, CancellationToken ct) => Task.FromResult<IEnumerable<Notification>>(_notifications.Where(n => n.UserId == userId).Take(limit));
    public Task<IEnumerable<Notification>> GetPendingAsync(int limit, CancellationToken ct) => Task.FromResult<IEnumerable<Notification>>(_notifications.Where(n => n.Status == "Pending").Take(limit));
    public Task UpdateAsync(Notification notification, CancellationToken ct) => Task.CompletedTask;
}

public class MockNotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly List<NotificationPreference> _preferences = new();
    public Task<NotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct) => Task.FromResult(_preferences.FirstOrDefault(p => p.UserId == userId));
    public Task AddAsync(NotificationPreference preference, CancellationToken ct) { _preferences.Add(preference); return Task.CompletedTask; }
    public Task UpdateAsync(NotificationPreference preference, CancellationToken ct) => Task.CompletedTask;
}