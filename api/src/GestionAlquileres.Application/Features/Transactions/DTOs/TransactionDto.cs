using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Transactions.DTOs;

public record TransactionDto(
    Guid Id,
    Guid ContractId,
    TransactionType Type,
    decimal Amount,
    Currency Currency,
    DateOnly Period,
    string? Notes,
    DateTimeOffset CreatedAt
);
