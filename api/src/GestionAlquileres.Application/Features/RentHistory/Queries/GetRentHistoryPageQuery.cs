using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Queries;

public record GetRentHistoryPageQuery(int Page, int PageSize, AdjustmentType? Type, string? Search)
    : IRequest<PagedResult<RentHistoryDto>>;
