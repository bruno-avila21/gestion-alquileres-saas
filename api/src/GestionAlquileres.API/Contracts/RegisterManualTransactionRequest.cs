using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.API.Contracts;

public record RegisterManualTransactionRequest(
    TransactionType Type,
    decimal Amount,
    DateOnly Period,
    string Notes
);
