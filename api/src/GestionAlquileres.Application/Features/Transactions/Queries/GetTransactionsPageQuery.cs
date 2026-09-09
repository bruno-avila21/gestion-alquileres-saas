using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Queries;

public record GetTransactionsPageQuery(int Page, int PageSize, TransactionType? Type, string? Search)
    : IRequest<TransactionsPageDto>;
