using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Domain.Reports;

namespace GestionAlquileres.Application.Common.Reports;

/// <summary>Arma el <see cref="AgencyBrand"/> de un PDF a partir de la Organization vigente, bajando
/// el logo del storage cuando hay uno. Compartido por el recibo de pago y la liquidación al
/// propietario — ambos arrancan el encabezado igual.</summary>
public static class AgencyBrandFactory
{
    public static async Task<AgencyBrand> BuildAsync(Organization org, IStorageService storage, CancellationToken ct)
    {
        byte[]? logo = null;
        if (!string.IsNullOrWhiteSpace(org.LogoStorageKey))
        {
            await using var stream = await storage.DownloadAsync(org.LogoStorageKey, ct);
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            logo = memory.ToArray();
        }

        return new AgencyBrand(org.Name, org.LegalName, org.TaxId, org.Address, org.Phone, org.Email, logo, org.BrandColor);
    }
}
