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

    public async Task<(IReadOnlyList<Notification> Items, int Total)> GetDeliveriesPageAsync(
        Guid userId,
        string? status,
        string? channel,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var q = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            q = q.Where(n => n.Status == s);
        }

        if (!string.IsNullOrWhiteSpace(channel))
        {
            var c = channel.Trim();
            q = q.Where(n => n.Channel == c);
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var skip = (page - 1) * pageSize;
        var rows = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var items = rows.Select(EntityMappers.ToDomain).ToList();
        return (items, total);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountByStatusForUserAsync(Guid userId, CancellationToken ct)
    {
        var rows = await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .GroupBy(n => n.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows.ToDictionary(x => x.Status, x => x.Count);
    }
}
