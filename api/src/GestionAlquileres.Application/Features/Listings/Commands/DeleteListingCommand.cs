using MediatR;

namespace GestionAlquileres.Application.Features.Listings.Commands;

public record DeleteListingCommand(Guid Id) : IRequest;
