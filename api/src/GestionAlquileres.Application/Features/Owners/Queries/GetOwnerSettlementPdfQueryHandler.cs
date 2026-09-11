using System.Globalization;
using System.Text;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Reports;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Domain.Reports;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

public class GetOwnerSettlementPdfQueryHandler : IRequestHandler<GetOwnerSettlementPdfQuery, PdfFileDto?>
{
    private readonly IOwnerRepository _ownerRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly IOrganizationRepository _organizations;
    private readonly IStorageService _storage;
    private readonly IPdfReportGenerator _pdf;
    private readonly ICurrentTenant _currentTenant;

    public GetOwnerSettlementPdfQueryHandler(
        IOwnerRepository ownerRepo,
        ITransactionRepository txRepo,
        IOrganizationRepository organizations,
        IStorageService storage,
        IPdfReportGenerator pdf,
        ICurrentTenant currentTenant)
    {
        _ownerRepo = ownerRepo;
        _txRepo = txRepo;
        _organizations = organizations;
        _storage = storage;
        _pdf = pdf;
        _currentTenant = currentTenant;
    }

    public async Task<PdfFileDto?> Handle(GetOwnerSettlementPdfQuery request, CancellationToken ct)
    {
        // A diferencia de GetOwnerSettlementQuery (JSON, 409 "Propietario no encontrado."), este
        // endpoint es un GET: la convención del repo es que sólo el GET resuelve con 404.
        var owner = await _ownerRepo.GetByIdAsync(request.OwnerId, ct);
        if (owner is null) return null;

        // to < from sigue siendo una regla de negocio -> 409 (comparte el mensaje con el JSON).
        GetOwnerSettlementQueryHandler.ValidatePeriod(request.From, request.To);

        var collectedRows = await _txRepo.GetCollectedByOwnerAsync(owner.Id, request.From, request.To, ct);
        var dto = GetOwnerSettlementQueryHandler.BuildDto(owner, request.From, request.To, collectedRows);

        var org = await _organizations.GetByIdAsync(_currentTenant.OrganizationId, ct)
            ?? throw new InvalidOperationException("La organización del token no existe.");
        var agency = await AgencyBrandFactory.BuildAsync(org, _storage, ct);

        var lines = dto.Lines
            .Select(l => new OwnerSettlementReportLine(l.PropertyAddress, l.Collected, l.CommissionPct, l.Commission, l.Net))
            .ToList();

        var report = new OwnerSettlementReport(
            Agency: agency,
            OwnerName: owner.Name,
            OwnerTaxId: owner.TaxId,
            OwnerCbu: owner.Cbu,
            PeriodFrom: request.From,
            PeriodTo: request.To,
            GrossCollected: dto.GrossCollected,
            Commission: dto.CommissionAmount,
            NetToOwner: dto.NetToOwner,
            Lines: lines);

        var bytes = _pdf.RenderOwnerSettlement(report);
        var fileName = $"liquidacion-{Slugify(owner.Name)}-{request.From:yyyyMM}-{request.To:yyyyMM}.pdf";
        return new PdfFileDto(bytes, fileName);
    }

    /// <summary>
    /// Slug del nombre del propietario para el nombre de archivo ("apellido-o-slug" del contrato):
    /// Owner sólo tiene un campo Name (puede ser una razón social, no necesariamente "nombre y
    /// apellido"), así que se slugifica el nombre completo en vez de intentar separar un apellido.
    /// </summary>
    private static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        }

        var slug = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "propietario" : slug;
    }
}
