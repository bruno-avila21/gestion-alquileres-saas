using GestionAlquileres.Application.Features.Transactions.Commands;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Queries;

public class ListAllTransactionsQueryHandler : IRequestHandler<ListAllTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    private readonly ITransactionRepository _repo;
    public ListAllTransactionsQueryHandler(ITransactionRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TransactionDto>> Handle(ListAllTransactionsQuery request, CancellationToken ct)
    {
        var txs = await _repo.GetAllAsync(ct);
        return txs.Select(RegisterPaymentCommandHandler.ToDto).ToList();
    }
}
