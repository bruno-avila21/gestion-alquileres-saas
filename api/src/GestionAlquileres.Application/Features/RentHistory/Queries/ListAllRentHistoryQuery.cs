using GestionAlquileres.Application.Features.RentHistory.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Queries;

public record ListAllRentHistoryQuery : IRequest<IReadOnlyList<RentHistoryDto>>;
