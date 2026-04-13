using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Services;

public interface IJwtService
{
    /// <summary>Creates a signed JWT with sub, email, org_id, and role claims.</summary>
    string GenerateToken(User user);
}
