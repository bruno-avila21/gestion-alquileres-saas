using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

public record RegisterManualTransactionCommand(
    Guid ContractId,
    TransactionType Type,
    decimal Amount,
    DateOnly Period,
    string Notes
) : IRequest<TransactionDto>;
