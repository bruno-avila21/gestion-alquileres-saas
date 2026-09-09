using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class SentNotificationRepository : ISentNotificationRepository
{
    private readonly AppDbContext _db;
    public SentNotificationRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsRawAsync(
        Guid contractId, Guid organizationId, NotificationKind kind, string dedupeKey, CancellationToken ct) =>
        _db.SentNotifications
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(n => n.ContractId == contractId
                        && n.OrganizationId == organizationId
                        && n.Kind == kind
                        && n.DedupeKey == dedupeKey, ct);

    public async Task AddAsync(SentNotification notification, CancellationToken ct) =>
        await _db.SentNotifications.AddAsync(notification, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
