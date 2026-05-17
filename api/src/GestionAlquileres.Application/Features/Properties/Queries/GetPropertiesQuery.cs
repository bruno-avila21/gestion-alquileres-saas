using GestionAlquileres.Application.Features.Properties.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Queries;

public record GetPropertiesQuery : IRequest<IReadOnlyList<PropertyDto>>;
