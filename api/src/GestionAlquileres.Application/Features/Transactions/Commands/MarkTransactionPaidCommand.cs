using GestionAlquileres.Application.Features.Transactions.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

/// <summary>Settles a pending charge: marks it Paid and stamps PaidAt.</summary>
public record MarkTransactionPaidCommand(
    Guid ContractId,
    Guid TransactionId
) : IRequest<TransactionDto>;
