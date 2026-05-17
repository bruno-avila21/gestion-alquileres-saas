using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Queries;

public record ListContractsQuery(
    Guid? AppTenantId,
    Guid? PropertyId,
    ContractStatus? Status
) : IRequest<IReadOnlyList<ContractDto>>;
