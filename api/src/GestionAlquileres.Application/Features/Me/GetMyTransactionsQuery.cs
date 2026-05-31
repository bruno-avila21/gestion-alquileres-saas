using GestionAlquileres.Application.Features.Transactions.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Me;

public record GetMyTransactionsQuery(Guid UserId) : IRequest<IReadOnlyList<TransactionDto>>;
