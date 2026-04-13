using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(Guid organizationId, string email, CancellationToken ct) =>
        _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.OrganizationId == organizationId && u.Email == email, ct);

    public Task<User?> GetByEmailAcrossOrgsAsync(string email, CancellationToken ct) =>
        _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _db.Users.AddAsync(user, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
