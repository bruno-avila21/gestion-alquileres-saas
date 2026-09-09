using GestionAlquileres.Application.Common.Export;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;
    public TransactionRepository(AppDbContext db) => _db = db;

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Transaction>> GetByContractAsync(Guid contractId, CancellationToken ct) =>
        await _db.Transactions
            .Where(t => t.ContractId == contractId)
            .OrderByDescending(t => t.Period)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Transaction>> GetPendingChargesAsync(Guid contractId, CancellationToken ct) =>
        await _db.Transactions
            .Where(t => t.ContractId == contractId
                        && t.Status == TransactionStatus.Pending
                        && (t.Type == TransactionType.RentCharge || t.Type == TransactionType.ManualDebit))
            .OrderBy(t => t.Period)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit, CancellationToken ct) =>
        await _db.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken ct) =>
        await _db.Transactions
            .AsNoTracking()
            .OrderByDescending(t => t.Period)
            .ThenByDescending(t => t.CreatedAt)
            .Take(ExportLimits.FetchSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OwnerCollectedRow>> GetCollectedByOwnerAsync(
        Guid ownerId, DateOnly periodFrom, DateOnly periodTo, CancellationToken ct)
    {
        // Todos los DbSet llevan el filtro global de tenant, así que el join queda acotado a la
        // organización en curso.
        //
        // Lo "cobrado" para el propietario es la plata efectivamente recibida: los ingresos de tipo
        // Payment del período (modelo de caja). Los dos caminos de cobro generan una fila Payment
        // —RegisterPayment la crea directo, y mark-paid registra el ingreso espejo (auditoría A1)—
        // así que la comisión ya no queda en cero para los cargos saldados por mark-paid.
        // Se agrupa hacia un tipo anónimo y recién después se mapea al record: proyectar
        // directamente a un tipo con constructor dentro del GroupBy no lo traducen todos los
        // proveedores.
        var grouped = await (
            from t in _db.Transactions.AsNoTracking()
            join c in _db.Contracts on t.ContractId equals c.Id
            join p in _db.Properties on c.PropertyId equals p.Id
            where p.OwnerId == ownerId
                && t.Type == TransactionType.Payment
                && t.Period >= periodFrom
                && t.Period <= periodTo
            group t by new { PropertyId = p.Id, p.Address, p.CommissionPct, ContractId = c.Id } into g
            select new
            {
                g.Key.PropertyId,
                g.Key.Address,
                g.Key.CommissionPct,
                g.Key.ContractId,
                Collected = g.Sum(x => x.Amount),
            })
            .ToListAsync(ct);

        return grouped
            .OrderBy(r => r.Address)
            .ThenBy(r => r.ContractId)
            .Select(r => new OwnerCollectedRow(
                r.PropertyId, r.Address, r.CommissionPct, r.ContractId, r.Collected))
            .ToList();
    }

    public async Task<(IReadOnlyList<Transaction> Items, int Total, decimal NetBalance)> GetPagedAsync(
        TransactionType? type, string? search, int page, int pageSize, CancellationToken ct)
    {
        // All DbSets carry the global tenant filter, so the join stays scoped to the current org.
        var query =
            from t in _db.Transactions
            join c in _db.Contracts on t.ContractId equals c.Id
            join at in _db.AppTenants on c.AppTenantId equals at.Id
            join p in _db.Properties on c.PropertyId equals p.Id
            select new { t, at, p };

        if (type.HasValue)
            query = query.Where(x => x.t.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.at.FirstName + " " + x.at.LastName).ToLower().Contains(s) ||
                x.p.Address.ToLower().Contains(s) ||
                (x.t.Notes ?? "").ToLower().Contains(s));
        }

        // El total y el balance salen en la MISMA consulta. Antes eran dos, y cada una repetía el
        // escaneo completo del join con el filtro de búsqueda: tres barridos por request, sobre una
        // condición que ningún índice puede servir (ver la nota sobre el buscador más abajo).
        //
        // Cash model, consistent with the owner settlement and the tenant balance (audit A1): money in
        // (payments + credits) minus what's owed (charges, ignoring cancelled), over the WHOLE filtered
        // set — not just the page.
        var aggregate = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                NetBalance = g.Sum(x =>
                    x.t.Type == TransactionType.Payment || x.t.Type == TransactionType.ManualCredit
                        ? x.t.Amount
                        : x.t.Status == TransactionStatus.Cancelled
                            ? 0m
                            : -x.t.Amount),
            })
            .FirstOrDefaultAsync(ct);

        var total = aggregate?.Total ?? 0;
        var netBalance = aggregate?.NetBalance ?? 0m;

        var items = await query
            .OrderByDescending(x => x.t.Period)
            .ThenByDescending(x => x.t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.t)
            .ToListAsync(ct);

        return (items, total, netBalance);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken ct) =>
        await _db.Transactions.AddAsync(transaction, ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
