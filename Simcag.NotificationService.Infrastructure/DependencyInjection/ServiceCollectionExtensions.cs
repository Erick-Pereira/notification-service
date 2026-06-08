using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Simcag.NotificationService.Application.Abstractions;
using Simcag.NotificationService.Domain.Interfaces;
using Simcag.NotificationService.Infrastructure.Persistence;
using Simcag.NotificationService.Infrastructure.Providers;
using Simcag.NotificationService.Infrastructure.Redis;
using StackExchange.Redis;

namespace Simcag.NotificationService.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgres = FirstNonEmpty(
            configuration.GetConnectionString("NotificationDb"),
            configuration.GetConnectionString("DefaultConnection"),
            Environment.GetEnvironmentVariable("POSTGRES_CONNECTION"),
            Environment.GetEnvironmentVariable("ConnectionStrings__NotificationDb"),
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));

        if (string.IsNullOrWhiteSpace(postgres))
        {
            services.AddDbContext<NotificationDbContext>(o =>
                o.UseInMemoryDatabase("notifications"));
        }
        else
        {
            services.AddDbContext<NotificationDbContext>(o => o.UseNpgsql(postgres));
        }

        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, EfNotificationPreferenceRepository>();

        services.AddSingleton<IEmailProvider, SmtpEmailProvider>();
        services.AddSingleton<ISmsProvider, TwilioSmsProvider>();

        services.Configure<RedisNotificationSendOptions>(o =>
        {
            o.DedupTtlHours = int.TryParse(
                Environment.GetEnvironmentVariable("NOTIFICATION_DEDUP_TTL_HOURS"), out var d) ? d : 24;
            o.MaxSendsPerUserPerHour = int.TryParse(
                Environment.GetEnvironmentVariable("NOTIFICATION_MAX_SENDS_PER_USER_PER_HOUR"), out var m) ? m : 30;
            o.KeyPrefix = Environment.GetEnvironmentVariable("REDIS_KEY_PREFIX") ?? "notif";
        });

        var redisConn = FirstNonEmpty(
            configuration.GetConnectionString("Redis"),
            Environment.GetEnvironmentVariable("REDIS_CONNECTION"),
            Environment.GetEnvironmentVariable("ConnectionStrings__Redis"));
        var disableRedis = string.Equals(
            Environment.GetEnvironmentVariable("REDIS_DISABLED"),
            "true", StringComparison.OrdinalIgnoreCase);

        if (!disableRedis && !string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var config = ConfigurationOptions.Parse(redisConn, true);
                return ConnectionMultiplexer.Connect(config);
            });
            services.AddSingleton<INotificationSendPolicy, RedisNotificationSendPolicy>();
        }
        else
        {
            services.AddSingleton<INotificationSendPolicy, NullNotificationSendPolicy>();
        }

        return services;
    }

    public static IHost RunNotificationMigrationsOnStartup(this IHost host)
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable("POSTGRES_MIGRATIONS_DISABLED"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return host;
        }

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }

        return host;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        }
        return null;
    }
}
