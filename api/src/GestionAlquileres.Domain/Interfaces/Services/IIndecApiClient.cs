namespace GestionAlquileres.Domain.Interfaces.Services;

public interface IIndecApiClient
{
    Task<IReadOnlyList<IndecIndexPoint>> GetIpcAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);
}

public record IndecIndexPoint(DateOnly Fecha, decimal Valor);
