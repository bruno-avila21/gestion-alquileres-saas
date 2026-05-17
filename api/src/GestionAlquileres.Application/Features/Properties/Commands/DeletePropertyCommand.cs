using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public record DeletePropertyCommand(Guid Id) : IRequest;
