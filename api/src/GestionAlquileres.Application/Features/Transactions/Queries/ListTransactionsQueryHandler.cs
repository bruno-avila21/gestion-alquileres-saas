using GestionAlquileres.Application.Features.Transactions.Commands;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Queries;

public class ListTransactionsQueryHandler : IRequestHandler<ListTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    private readonly ITransactionRepository _repo;
    public ListTransactionsQueryHandler(ITransactionRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<TransactionDto>> Handle(ListTransactionsQuery request, CancellationToken ct)
    {
        var txs = await _repo.GetByContractAsync(request.ContractId, ct);
        return txs.Select(RegisterPaymentCommandHandler.ToDto).ToList();
    }
}
