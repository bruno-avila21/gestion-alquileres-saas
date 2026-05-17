using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.API.Contracts;

public record SyncIndexRequest(IndexType IndexType, DateOnly Period);
