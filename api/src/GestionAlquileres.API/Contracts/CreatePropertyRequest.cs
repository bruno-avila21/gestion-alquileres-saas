using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.API.Contracts;

public record CreatePropertyRequest(
    string Address,
    string City,
    string Province,
    PropertyType PropertyType,
    decimal? AreaM2,
    string? Notes,
    Guid? OwnerId,
    decimal? CommissionPct);
