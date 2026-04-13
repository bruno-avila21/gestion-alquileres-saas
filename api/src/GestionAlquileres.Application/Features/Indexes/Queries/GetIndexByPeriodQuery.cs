using GestionAlquileres.Application.Features.Indexes.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.Indexes.Queries;

public record GetIndexByPeriodQuery(IndexType IndexType, DateOnly From, DateOnly To)
    : IRequest<IReadOnlyList<IndexValueDto>>;
