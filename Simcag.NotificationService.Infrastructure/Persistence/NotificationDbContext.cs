using Microsoft.EntityFrameworkCore;

namespace Simcag.NotificationService.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();
    public DbSet<NotificationPreferenceRecord> NotificationPreferences => Set<NotificationPreferenceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationRecord>(b =>
        {
            b.ToTable("notifications");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.CreatedAt });
        });

        modelBuilder.Entity<NotificationPreferenceRecord>(b =>
        {
            b.ToTable("notification_preferences");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId).IsUnique();
        });
    }
}
