using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Indexes.DTOs;

public record IndexValueDto(
    Guid Id,
    IndexType IndexType,
    DateOnly Period,
    decimal Value,
    decimal? VariationPct,
    string Source,
    DateTimeOffset FetchedAt);
