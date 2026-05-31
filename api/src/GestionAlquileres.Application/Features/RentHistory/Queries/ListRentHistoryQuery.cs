using GestionAlquileres.Application.Features.RentHistory.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Queries;

public record ListRentHistoryQuery(Guid ContractId) : IRequest<IReadOnlyList<RentHistoryDto>>;
