using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.API.Contracts;

public record UpdatePropertyRequest(
    string Address,
    string City,
    string Province,
    PropertyType PropertyType,
    decimal? AreaM2,
    string? Notes,
    bool IsActive);
