using GestionAlquileres.Application.Features.Owners.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

public record GetOwnersQuery() : IRequest<IReadOnlyList<OwnerDto>>;
