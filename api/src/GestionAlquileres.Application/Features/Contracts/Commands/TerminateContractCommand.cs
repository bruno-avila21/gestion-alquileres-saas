using GestionAlquileres.Application.Features.Contracts.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

public record TerminateContractCommand(Guid Id, string? Notes) : IRequest<ContractDto?>;
