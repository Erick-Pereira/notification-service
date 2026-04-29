using Microsoft.EntityFrameworkCore;
using Simcag.NotificationService.Domain.Entities;
using Simcag.NotificationService.Domain.Interfaces;

namespace Simcag.NotificationService.Infrastructure.Persistence;

public sealed class EfNotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly NotificationDbContext _db;

    public EfNotificationPreferenceRepository(NotificationDbContext db) => _db = db;

    public async Task<NotificationPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var row = await _db.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return row == null ? null : EntityMappers.ToDomain(row);
    }

    public async Task AddAsync(NotificationPreference preference, CancellationToken ct)
    {
        await _db.NotificationPreferences.AddAsync(EntityMappers.ToRecord(preference), ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationPreference preference, CancellationToken ct)
    {
        var rec = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == preference.UserId, ct);
        if (rec == null) return;
        var mapped = EntityMappers.ToRecord(preference);
        _db.Entry(rec).CurrentValues.SetValues(mapped);
        await _db.SaveChangesAsync(ct);
    }
}
