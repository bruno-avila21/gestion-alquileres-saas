using GestionAlquileres.Application.Features.Owners.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

public record GetOwnerByIdQuery(Guid Id) : IRequest<OwnerDto?>;
