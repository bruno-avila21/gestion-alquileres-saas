using GestionAlquileres.Application.Features.Contracts.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Me;

public record GetMyContractQuery(Guid UserId) : IRequest<ContractDto?>;
