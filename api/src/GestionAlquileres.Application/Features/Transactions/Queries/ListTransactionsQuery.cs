using GestionAlquileres.Application.Features.Transactions.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Queries;

public record ListTransactionsQuery(Guid ContractId) : IRequest<IReadOnlyList<TransactionDto>>;
