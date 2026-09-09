using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly AppDbContext _db;
    public LeadRepository(AppDbContext db) => _db = db;

    public Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Leads
            .Include(l => l.Listing)
            .Include(l => l.Property)
            .Include(l => l.Notes)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Lead?> GetForEditAsync(Guid id, CancellationToken ct) =>
        _db.Leads
            .Include(l => l.Listing)
            .Include(l => l.Property)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<(IReadOnlyList<Lead> Items, int Total)> GetPagedAsync(
        LeadStatus? status, string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Leads
            .Include(l => l.Listing)
            .Include(l => l.Property)
            .Include(l => l.Notes)
            .AsQueryable();

        if (status is { } s) query = query.Where(l => l.Status == s);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l =>
                l.Name.ToLower().Contains(term)
                || (l.Email != null && l.Email.ToLower().Contains(term))
                || (l.Phone != null && l.Phone.ToLower().Contains(term))
                || (l.Listing != null && l.Listing.Title.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(int Total, IReadOnlyDictionary<LeadStatus, int> ByStatus)> GetSummaryAsync(CancellationToken ct)
    {
        var counts = await _db.Leads
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byStatus = counts.ToDictionary(c => c.Status, c => c.Count);
        var total = counts.Sum(c => c.Count);
        return (total, byStatus);
    }

    public async Task AddAsync(Lead lead, CancellationToken ct) => await _db.Leads.AddAsync(lead, ct);

    public async Task AddNoteAsync(LeadNote note, CancellationToken ct) => await _db.LeadNotes.AddAsync(note, ct);

    public void Remove(Lead lead) => _db.Leads.Remove(lead);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
