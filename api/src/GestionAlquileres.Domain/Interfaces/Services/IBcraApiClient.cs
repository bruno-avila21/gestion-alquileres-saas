namespace GestionAlquileres.Domain.Interfaces.Services;

public interface IBcraApiClient
{
    Task<IReadOnlyList<BcraIndexPoint>> GetIclAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);
}

public record BcraIndexPoint(DateOnly Fecha, decimal Valor);
