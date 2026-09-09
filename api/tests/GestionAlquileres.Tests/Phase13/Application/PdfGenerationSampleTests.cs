using GestionAlquileres.Domain.Reports;
using GestionAlquileres.Infrastructure.Reports;

namespace GestionAlquileres.Tests.Phase13.Application;

/// <summary>
/// Bloque PDF recibos/liquidaciones, criterio de terminado #3: "los dos PDF abren en un lector real
/// y muestran la marca cargada". Este test genera los dos documentos con datos de ejemplo (incluida
/// una marca con color propio, para probar el encabezado con color) usando el generador real de
/// QuestPDF —no un fake— y los guarda en api/artifacts-qa/ para inspección manual, además de
/// verificar automáticamente que los bytes son un PDF válido (empiezan con "%PDF") y pesan más de 1 KB.
/// </summary>
[Trait("Phase", "Phase13")]
public class PdfGenerationSampleTests
{
    private static readonly string ArtifactsDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "artifacts-qa");

    static PdfGenerationSampleTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPDF.Settings.UseEnvironmentFonts = false;
    }

    private static void AssertValidPdf(byte[] bytes)
    {
        Assert.True(bytes.Length > 1024, $"El PDF pesa {bytes.Length} bytes, se esperaba más de 1 KB.");
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Recibo_de_pago_de_muestra_es_un_PDF_valido()
    {
        var generator = new QuestPdfReportGenerator();

        var agency = new AgencyBrand(
            Name: "Inmobiliaria del Sur",
            LegalName: "Del Sur Propiedades S.R.L.",
            TaxId: "30-71234567-9",
            Address: "Av. Corrientes 1234, CABA",
            Phone: "011 4444-5555",
            Email: "contacto@delsur.com.ar",
            Logo: null, // sin logo: el encabezado debe salir sólo con texto, sin romperse
            BrandColor: "#1B4F72");

        var report = new ReceiptReport(
            Number: "REC-00000042",
            IssuedOn: new DateOnly(2026, 3, 10),
            Agency: agency,
            PayerName: "Ana López",
            PayerDocument: "28.888.111",
            PropertyAddress: "Rivadavia 1000, Villa Pueyrredón, CABA",
            Concept: "Alquiler período 03/2026",
            Amount: 120000m,
            CurrencyCode: "ARS",
            AmountInWords: $"Pesos {AmountInWords.Convert(120000m)}",
            Notes: "Pago recibido en efectivo.");

        var bytes = generator.RenderReceipt(report);
        AssertValidPdf(bytes);

        Directory.CreateDirectory(ArtifactsDir);
        File.WriteAllBytes(Path.Combine(ArtifactsDir, "recibo-muestra.pdf"), bytes);
    }

    [Fact]
    public void Liquidacion_al_propietario_de_muestra_es_un_PDF_valido()
    {
        var generator = new QuestPdfReportGenerator();

        var agency = new AgencyBrand(
            Name: "Inmobiliaria del Sur",
            LegalName: "Del Sur Propiedades S.R.L.",
            TaxId: "30-71234567-9",
            Address: "Av. Corrientes 1234, CABA",
            Phone: "011 4444-5555",
            Email: null,
            Logo: null,
            BrandColor: "#1B4F72");

        var lines = new List<OwnerSettlementReportLine>
        {
            new("Rivadavia 1000, CABA", 120000m, 8m, 9600m, 110400m),
            new("San Martín 250, San Isidro", 95000m, 8m, 7600m, 87400m),
        };

        var report = new OwnerSettlementReport(
            Agency: agency,
            OwnerName: "Carlos Fernández",
            OwnerTaxId: "20-12345678-3",
            OwnerCbu: "0170099220000012345678",
            PeriodFrom: new DateOnly(2026, 1, 1),
            PeriodTo: new DateOnly(2026, 3, 1),
            GrossCollected: 215000m,
            Commission: 17200m,
            NetToOwner: 197800m,
            Lines: lines);

        var bytes = generator.RenderOwnerSettlement(report);
        AssertValidPdf(bytes);

        Directory.CreateDirectory(ArtifactsDir);
        File.WriteAllBytes(Path.Combine(ArtifactsDir, "liquidacion-muestra.pdf"), bytes);
    }

    [Fact]
    public void Liquidacion_sin_cobranzas_muestra_el_estado_vacio_y_es_un_PDF_valido()
    {
        var generator = new QuestPdfReportGenerator();
        var agency = new AgencyBrand("Inmobiliaria del Sur", null, null, null, null, null, null, null);

        var report = new OwnerSettlementReport(
            Agency: agency,
            OwnerName: "Carlos Fernández",
            OwnerTaxId: null,
            OwnerCbu: null,
            PeriodFrom: new DateOnly(2026, 4, 1),
            PeriodTo: new DateOnly(2026, 4, 1),
            GrossCollected: 0m,
            Commission: 0m,
            NetToOwner: 0m,
            Lines: Array.Empty<OwnerSettlementReportLine>());

        var bytes = generator.RenderOwnerSettlement(report);
        AssertValidPdf(bytes);
    }
}
