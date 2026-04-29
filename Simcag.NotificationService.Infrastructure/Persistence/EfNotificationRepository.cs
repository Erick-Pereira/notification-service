using Microsoft.EntityFrameworkCore;
using Simcag.NotificationService.Domain.Entities;
using Simcag.NotificationService.Domain.Interfaces;

namespace Simcag.NotificationService.Infrastructure.Persistence;

public sealed class EfNotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public EfNotificationRepository(NotificationDbContext db) => _db = db;

    public async Task AddAsync(Notification notification, CancellationToken ct)
    {
        await _db.Notifications.AddAsync(EntityMappers.ToRecord(notification), ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var row = await _db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
        return row == null ? null : EntityMappers.ToDomain(row);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int limit, CancellationToken ct)
    {
        var rows = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
        return rows.Select(EntityMappers.ToDomain);
    }

    public async Task<IEnumerable<Notification>> GetPendingAsync(int limit, CancellationToken ct)
    {
        var rows = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.Status == "Pending")
            .OrderBy(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
        return rows.Select(EntityMappers.ToDomain);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken ct)
    {
        var rec = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notification.Id, ct);
        if (rec == null) return;
        var mapped = EntityMappers.ToRecord(notification);
        _db.Entry(rec).CurrentValues.SetValues(mapped);
        await _db.SaveChangesAsync(ct);
    }
}
