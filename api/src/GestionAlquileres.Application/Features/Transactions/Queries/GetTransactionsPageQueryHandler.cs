using GestionAlquileres.Application.Common.Paging;
using GestionAlquileres.Application.Features.Transactions.Commands;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Queries;

public class GetTransactionsPageQueryHandler
    : IRequestHandler<GetTransactionsPageQuery, TransactionsPageDto>
{
    private readonly ITransactionRepository _repo;
    public GetTransactionsPageQueryHandler(ITransactionRepository repo) => _repo = repo;

    public async Task<TransactionsPageDto> Handle(GetTransactionsPageQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);
        var (items, total, netBalance) = await _repo.GetPagedAsync(request.Type, request.Search, page, pageSize, ct);
        var dtos = items.Select(RegisterPaymentCommandHandler.ToDto).ToList();
        return new TransactionsPageDto(dtos, total, page, pageSize, netBalance);
    }
}
