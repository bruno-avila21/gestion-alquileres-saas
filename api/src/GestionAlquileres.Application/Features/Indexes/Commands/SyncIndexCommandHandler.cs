using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Indexes.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Application.Features.Indexes.Commands;

public class SyncIndexCommandHandler : IRequestHandler<SyncIndexCommand, SyncIndexResult>
{
    private readonly IIndexRepository _indexRepo;
    private readonly IBcraApiClient _bcra;
    private readonly IIndecApiClient _indec;
    private readonly ILogger<SyncIndexCommandHandler> _logger;

    public SyncIndexCommandHandler(
        IIndexRepository indexRepo,
        IBcraApiClient bcra,
        IIndecApiClient indec,
        ILogger<SyncIndexCommandHandler> logger)
    {
        _indexRepo = indexRepo;
        _bcra = bcra;
        _indec = indec;
        _logger = logger;
    }

    public async Task<SyncIndexResult> Handle(SyncIndexCommand request, CancellationToken ct)
    {
        // Normalize: always first day of month.
        var period = new DateOnly(request.Period.Year, request.Period.Month, 1);

        // Idempotency: skip external call if already persisted.
        var existing = await _indexRepo.GetByPeriodAsync(request.IndexType, period, ct);
        if (existing is not null)
        {
            return SyncIndexResults.AlreadyExisted(ToDto(existing));
        }

        // Compute month range for external API (daily API returns many points per month).
        var firstOfMonth = period;
        var lastOfMonth = firstOfMonth.AddDays(DateTime.DaysInMonth(period.Year, period.Month) - 1);

        IndexValue? fetched = null;
        try
        {
            fetched = request.IndexType switch
            {
                IndexType.ICL => await FetchIclAsync(firstOfMonth, lastOfMonth, period, ct),
                IndexType.IPC => await FetchIpcAsync(firstOfMonth, lastOfMonth, period, ct),
                _ => throw new BusinessException($"Unsupported IndexType: {request.IndexType}")
            };

            if (fetched is null)
                throw new InvalidOperationException("External API returned no data for period.");
        }
        catch (Exception ex) when (ex is not BusinessException)
        {
            // IDX-04: external API unavailable → fallback to last known value.
            _logger.LogWarning(ex,
                "External API unavailable for {IndexType} period {Period}. Attempting fallback.",
                request.IndexType, period);

            var fallback = await _indexRepo.GetLastAvailableAsync(request.IndexType, ct);
            if (fallback is null)
            {
                throw new BusinessException(
                    $"No se pudo obtener {request.IndexType} para {period:yyyy-MM} " +
                    "y no hay valor previo disponible.");
            }
            return SyncIndexResults.Fallback(ToDto(fallback));
        }

        // Persist the fetched value (CLAUDE.md: "Índices persistidos — NUNCA calcular on-the-fly").
        await _indexRepo.AddAsync(fetched, ct);
        await _indexRepo.SaveChangesAsync(ct);

        return SyncIndexResults.NewlySynced(ToDto(fetched));
    }

    private async Task<IndexValue?> FetchIclAsync(
        DateOnly desde, DateOnly hasta, DateOnly period, CancellationToken ct)
    {
        var points = await _bcra.GetIclAsync(desde, hasta, ct);
        if (points.Count == 0) return null;

        // RESEARCH Pitfall 2: BCRA returns daily values — normalize to one per month.
        // Convention: take the latest business day of the month (last published value).
        var latest = points.OrderByDescending(p => p.Fecha).First();

        return new IndexValue
        {
            IndexType = IndexType.ICL,
            Period = period, // first day of month
            Value = latest.Valor,
            Source = "BCRA",
            FetchedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<IndexValue?> FetchIpcAsync(
        DateOnly desde, DateOnly hasta, DateOnly period, CancellationToken ct)
    {
        var points = await _indec.GetIpcAsync(desde, hasta, ct);
        if (points.Count == 0) return null;

        // IPC is already monthly — one point per month.
        var match = points.FirstOrDefault(p => p.Fecha.Year == period.Year && p.Fecha.Month == period.Month)
                    ?? points.OrderByDescending(p => p.Fecha).First();

        return new IndexValue
        {
            IndexType = IndexType.IPC,
            Period = period,
            Value = match.Valor,
            Source = "INDEC",
            FetchedAt = DateTimeOffset.UtcNow
        };
    }

    private static IndexValueDto ToDto(IndexValue v) =>
        new(v.Id, v.IndexType, v.Period, v.Value, v.VariationPct, v.Source, v.FetchedAt);
}
