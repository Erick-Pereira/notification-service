using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Simcag.NotificationService.Infrastructure.Persistence;

/// <summary>Design-time: <c>dotnet ef migrations add / update</c> a partir do projeto API.</summary>
public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=simcag_notifications;Username=postgres;Password=postgres";
        var b = new DbContextOptionsBuilder<NotificationDbContext>();
        b.UseNpgsql(connectionString);
        return new NotificationDbContext(b.Options);
    }
}
