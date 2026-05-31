using GestionAlquileres.Application.Features.Transactions.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

public record RegisterPaymentCommand(
    Guid ContractId,
    decimal Amount,
    DateOnly Period,
    string? Notes
) : IRequest<TransactionDto>;
