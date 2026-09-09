using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Domain.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionAlquileres.Infrastructure.Reports;

/// <summary>
/// Genera los PDF del negocio con QuestPDF (licencia Community). El PDF no se persiste: se arma en
/// cada descarga a partir de los modelos de <see cref="GestionAlquileres.Domain.Reports"/>, ya
/// resueltos por el handler de Application. Registrado como singleton — QuestPDF es sin estado.
/// </summary>
public class QuestPdfReportGenerator : IPdfReportGenerator
{
    private const string LegalNotice = "Documento no válido como factura.";
    private static readonly string DefaultBrandColor = Colors.Grey.Darken3;

    public byte[] RenderReceipt(ReceiptReport report)
    {
        var brandColor = ResolveColor(report.Agency.BrandColor);
        var currencySymbol = report.CurrencyCode == "USD" ? "USD" : "$";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Un recibo es un documento compacto: va enmarcado y pegado al borde superior,
                // para que se pueda recortar. Si se usara page.Header/Content/Footer, la firma
                // quedaría flotando a un tercio de la hoja y el resto en blanco.
                page.Content().AlignTop().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(18).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Element(header => ComposeReceiptHeader(header, report, brandColor));

                    column.Item().PaddingTop(5).Element(c => ComposeReceiptParty(c, report));

                    column.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"SON: {report.AmountInWords}").FontSize(11);
                        row.ConstantItem(140).AlignRight().Text($"{currencySymbol} {report.Amount:N2}")
                            .FontSize(14).Bold().FontColor(brandColor);
                    });

                    if (!string.IsNullOrWhiteSpace(report.Notes))
                    {
                        column.Item().Column(notes =>
                        {
                            notes.Item().Text("Observaciones").Bold().FontSize(9);
                            notes.Item().Text(report.Notes).FontSize(9);
                        });
                    }

                    column.Item().PaddingTop(45).AlignRight().Column(sign =>
                    {
                        sign.Item().Width(220).LineHorizontal(0.75f).LineColor(Colors.Grey.Darken1);
                        sign.Item().AlignCenter().Text("Firma y sello").FontSize(8).FontColor(Colors.Grey.Darken2);
                    });

                    column.Item().PaddingTop(6).Text(LegalNotice)
                        .FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf();
    }

    public byte[] RenderOwnerSettlement(OwnerSettlementReport report)
    {
        var brandColor = ResolveColor(report.Agency.BrandColor);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header => ComposeSettlementHeader(header, report, brandColor));

                page.Content().PaddingTop(15).Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Text($"Propietario: {report.OwnerName}{(string.IsNullOrWhiteSpace(report.OwnerTaxId) ? "" : $" (CUIT {report.OwnerTaxId})")}")
                        .Bold().FontSize(11);

                    column.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);

                    if (report.Lines.Count == 0)
                    {
                        column.Item().PaddingVertical(20).AlignCenter()
                            .Text("Sin cobranzas registradas en el período.")
                            .FontSize(10).FontColor(Colors.Grey.Darken2);
                    }
                    else
                    {
                        column.Item().Element(c => ComposeSettlementTable(c, report, brandColor));
                    }

                    column.Item().PaddingTop(10).Column(totals =>
                    {
                        totals.Spacing(2);
                        ComposeTotalRow(totals, "Total cobrado", report.GrossCollected, bold: false);
                        ComposeTotalRow(totals, "Comisión de administración", -report.Commission, bold: false);
                        totals.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Darken1);
                        ComposeTotalRow(totals, "NETO A LIQUIDAR", report.NetToOwner, bold: true, color: brandColor);
                    });

                    if (!string.IsNullOrWhiteSpace(report.OwnerCbu))
                    {
                        column.Item().PaddingTop(10).Text($"CBU para la transferencia: {report.OwnerCbu}").FontSize(9);
                    }
                });

                page.Footer().PaddingTop(10).Text(LegalNotice)
                    .FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();
    }

    // ---- Encabezados ----

    private static void ComposeReceiptHeader(IContainer container, ReceiptReport report, string brandColor)
    {
        container.Row(row =>
        {
            if (report.Agency.Logo is { Length: > 0 })
                row.ConstantItem(60).Image(report.Agency.Logo).FitArea();

            row.RelativeItem().Column(agency =>
            {
                agency.Item().Text(report.Agency.Name.ToUpperInvariant()).Bold().FontSize(14).FontColor(brandColor);
                if (!string.IsNullOrWhiteSpace(report.Agency.LegalName) || !string.IsNullOrWhiteSpace(report.Agency.TaxId))
                {
                    agency.Item().Text(string.Join(" · ", new[] { report.Agency.LegalName, FormatTaxId(report.Agency.TaxId) }
                        .Where(s => !string.IsNullOrWhiteSpace(s)))).FontSize(8);
                }
                var contactLine = string.Join(" · ", new[] { report.Agency.Address, report.Agency.Phone, report.Agency.Email }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(contactLine))
                    agency.Item().Text(contactLine).FontSize(8);
            });

            row.ConstantItem(160).Column(meta =>
            {
                meta.Item().AlignRight().Text($"RECIBO N° {report.Number}").Bold().FontSize(12);
                meta.Item().AlignRight().Text($"Fecha: {report.IssuedOn:dd/MM/yyyy}").FontSize(9);
            });
        });
    }

    private static void ComposeReceiptParty(IContainer container, ReceiptReport report)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text(t =>
            {
                t.Span("Recibí de: ").SemiBold();
                t.Span(report.PayerName + (string.IsNullOrWhiteSpace(report.PayerDocument) ? "" : $" (DNI {report.PayerDocument})"));
            });
            column.Item().Text(t =>
            {
                t.Span("Por el inmueble: ").SemiBold();
                t.Span(report.PropertyAddress);
            });
            column.Item().Text(t =>
            {
                t.Span("En concepto de: ").SemiBold();
                t.Span(report.Concept);
            });
        });
    }

    private static void ComposeSettlementHeader(IContainer container, OwnerSettlementReport report, string brandColor)
    {
        container.Row(row =>
        {
            if (report.Agency.Logo is { Length: > 0 })
                row.ConstantItem(60).Image(report.Agency.Logo).FitArea();

            row.RelativeItem().Column(agency =>
            {
                agency.Item().Text(report.Agency.Name.ToUpperInvariant()).Bold().FontSize(14).FontColor(brandColor);
                var contactLine = string.Join(" · ", new[] { FormatTaxId(report.Agency.TaxId), report.Agency.Address, report.Agency.Phone }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(contactLine))
                    agency.Item().Text(contactLine).FontSize(8);
            });

            row.ConstantItem(180).Column(meta =>
            {
                meta.Item().AlignRight().Text("LIQUIDACIÓN AL PROPIETARIO").Bold().FontSize(12);
                meta.Item().AlignRight().Text($"Período {report.PeriodFrom:MM/yyyy}–{report.PeriodTo:MM/yyyy}").FontSize(9);
            });
        });
    }

    private static void ComposeSettlementTable(IContainer container, OwnerSettlementReport report, string brandColor)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1.3f);
                columns.RelativeColumn(1.3f);
            });

            // Encabezado de tabla repetido en cada página.
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Inmueble");
                header.Cell().Element(HeaderCell).AlignRight().Text("Cobrado");
                header.Cell().Element(HeaderCell).AlignRight().Text("Comisión %");
                header.Cell().Element(HeaderCell).AlignRight().Text("Comisión");
                header.Cell().Element(HeaderCell).AlignRight().Text("Neto");

                IContainer HeaderCell(IContainer c) => c
                    .DefaultTextStyle(x => x.Bold().FontColor(Colors.White).FontSize(9))
                    .Background(brandColor)
                    .PaddingVertical(5).PaddingHorizontal(4);
            });

            foreach (var line in report.Lines)
            {
                table.Cell().Element(BodyCell).Text(line.PropertyAddress);
                table.Cell().Element(BodyCell).AlignRight().Text($"$ {line.Collected:N2}");
                table.Cell().Element(BodyCell).AlignRight().Text($"{line.CommissionPct:0.##}%");
                table.Cell().Element(BodyCell).AlignRight().Text($"$ {line.Commission:N2}");
                table.Cell().Element(BodyCell).AlignRight().Text($"$ {line.Net:N2}");
            }

            IContainer BodyCell(IContainer c) => c
                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(4).PaddingHorizontal(4);
        });
    }

    private static void ComposeTotalRow(ColumnDescriptor totals, string label, decimal amount, bool bold, string? color = null)
    {
        totals.Item().Row(row =>
        {
            var textStyle = TextStyle.Default.FontSize(bold ? 12 : 10);
            if (bold) textStyle = textStyle.Bold();
            if (color is not null) textStyle = textStyle.FontColor(color);

            row.RelativeItem().Text(label).Style(textStyle);
            row.ConstantItem(140).AlignRight().Text($"{(amount < 0 ? "-" : "")}$ {Math.Abs(amount):N2}").Style(textStyle);
        });
    }

    private static string FormatTaxId(string? taxId) =>
        string.IsNullOrWhiteSpace(taxId) ? "" : $"CUIT {taxId}";

    private static string ResolveColor(string? brandColorHex) =>
        string.IsNullOrWhiteSpace(brandColorHex) ? DefaultBrandColor : brandColorHex;
}
