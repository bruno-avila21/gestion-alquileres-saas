using GestionAlquileres.Application.Features.Transactions.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Queries;

public record ListAllTransactionsQuery : IRequest<IReadOnlyList<TransactionDto>>;
