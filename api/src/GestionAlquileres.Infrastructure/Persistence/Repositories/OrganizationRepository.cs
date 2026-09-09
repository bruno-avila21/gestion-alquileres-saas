using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _db;
    public OrganizationRepository(AppDbContext db) => _db = db;

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) =>
        _db.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Slug == slug, ct);

    public async Task AddAsync(Organization org, CancellationToken ct)
    {
        await _db.Organizations.AddAsync(org, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<long> IncrementReceiptSequenceAsync(Guid organizationId, CancellationToken ct)
    {
        // En Postgres (proveedor relacional): UPDATE atómico de una sola fila con ExecuteUpdateAsync
        // (nada de FromSqlRaw), envuelto en una transacción explícita que se mantiene abierta hasta
        // releer el valor. El UPDATE toma el lock de fila y lo retiene hasta el commit: un segundo
        // pedido concurrente para la misma organización espera ahí, así que su propio re-read (tras
        // su propio UPDATE) siempre ve el valor que ÉL incrementó, nunca el de otro pedido. Así no
        // se puede saltear un número ni repetirlo.
        if (_db.Database.IsRelational())
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            await _db.Organizations
                .Where(o => o.Id == organizationId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.ReceiptSequence, o => o.ReceiptSequence + 1), ct);

            var newValue = await _db.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.ReceiptSequence)
                .FirstAsync(ct);

            await tx.CommitAsync(ct);
            return newValue;
        }

        // El proveedor InMemory (usado en la suite de tests) no traduce ExecuteUpdateAsync ni
        // soporta transacciones reales — lanza al llamar BeginTransactionAsync. Ahí no hay
        // concurrencia real que proteger, así que alcanza con cargar, incrementar y guardar.
        var org = await _db.Organizations.FirstAsync(o => o.Id == organizationId, ct);
        org.ReceiptSequence++;
        await _db.SaveChangesAsync(ct);
        return org.ReceiptSequence;
    }
}
