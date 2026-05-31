namespace GestionAlquileres.API.Contracts;

public record RegisterPaymentRequest(decimal Amount, DateOnly Period, string? Notes);
