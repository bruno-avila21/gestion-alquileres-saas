using GestionAlquileres.Application.Features.RentHistory.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Me;

public record GetMyRentHistoryQuery(Guid UserId) : IRequest<IReadOnlyList<RentHistoryDto>>;
