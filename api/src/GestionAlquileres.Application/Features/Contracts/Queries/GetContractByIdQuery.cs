using GestionAlquileres.Application.Features.Contracts.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Queries;

public record GetContractByIdQuery(Guid Id) : IRequest<ContractDto?>;
