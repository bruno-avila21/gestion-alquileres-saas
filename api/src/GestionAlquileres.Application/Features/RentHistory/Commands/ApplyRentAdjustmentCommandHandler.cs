using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using Entities = GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Application.Features.RentHistory.Commands;

public class ApplyRentAdjustmentCommandHandler : IRequestHandler<ApplyRentAdjustmentCommand, RentHistoryDto>
{
    private readonly IContractRepository _contractRepo;
    private readonly IRentHistoryRepository _historyRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly IIndexRepository _indexRepo;
    private readonly IEmailService _email;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<ApplyRentAdjustmentCommandHandler> _logger;

    public ApplyRentAdjustmentCommandHandler(
        IContractRepository contractRepo,
        IRentHistoryRepository historyRepo,
        ITransactionRepository txRepo,
        IIndexRepository indexRepo,
        IEmailService email,
        ICurrentTenant currentTenant,
        ILogger<ApplyRentAdjustmentCommandHandler> logger)
    {
        _contractRepo = contractRepo;
        _historyRepo = historyRepo;
        _txRepo = txRepo;
        _indexRepo = indexRepo;
        _email = email;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<RentHistoryDto> Handle(ApplyRentAdjustmentCommand request, CancellationToken ct)
    {
        // In background jobs (Hangfire) ICurrentTenant returns Guid.Empty — fall back to a raw lookup
        // that the job pins to the contract's organization, so it can never resolve another tenant's contract.
        Entities.Contract? contract;
        if (_currentTenant.OrganizationId != Guid.Empty)
        {
            contract = await _contractRepo.GetByIdAsync(request.ContractId, ct);
        }
        else if (request.OrganizationId is { } orgId && orgId != Guid.Empty)
        {
            contract = await _contractRepo.GetByIdRawAsync(request.ContractId, orgId, ct);
        }
        else
        {
            throw new BusinessException("No hay organización en contexto para resolver el contrato.");
        }

        if (contract is null)
            throw new BusinessException("Contrato no encontrado.");

        if (contract.Status != ContractStatus.Active)
            throw new BusinessException("Solo se pueden ajustar contratos vigentes.");

        var effectiveDate = request.EffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Idempotency: never apply two adjustments for the same contract+effective date.
        // Guards against job retries, manual + scheduled overlap, and concurrent requests.
        // (The DB also enforces this with a unique (ContractId, EffectiveDate) index.)
        if (await _historyRepo.ExistsForPeriodAsync(contract.Id, effectiveDate, ct))
            throw new BusinessException(
                $"Ya existe un ajuste para este contrato con fecha efectiva {effectiveDate:yyyy-MM-dd}.");

        decimal previousRent = contract.MonthlyRent;
        decimal newRent;
        decimal factor;
        Guid? indexValueId = null;

        if (contract.AdjustmentType == AdjustmentType.Manual)
        {
            if (!request.ManualNewRent.HasValue || request.ManualNewRent.Value <= 0)
                throw new BusinessException("Para ajuste manual se debe indicar el nuevo valor del alquiler.");
            if (string.IsNullOrWhiteSpace(request.Notes))
                throw new BusinessException("El ajuste manual requiere una nota explicativa (campo notes obligatorio).");

            newRent = request.ManualNewRent.Value;
            factor = Math.Round(newRent / previousRent, 6);
        }
        else if (contract.AdjustmentType == AdjustmentType.FixedPercent)
        {
            // Porcentaje pactado: no depende de ningún índice ni de un servicio externo.
            if (contract.AdjustmentPercent is not { } percent || percent <= 0)
                throw new BusinessException(
                    "El contrato es de porcentaje fijo pero no tiene un porcentaje de ajuste configurado.");

            // Igual que en la rama indexada: NO se pre-redondea el factor para después multiplicar.
            // Redondear el factor primero sesga el importe, y el error se acumula porque cada ajuste
            // toma como base el alquiler ya redondeado del anterior.
            newRent = Math.Round(previousRent * (100m + percent) / 100m, 2);
            factor = Math.Round((100m + percent) / 100m, 6);
        }
        else
        {
            var indexType = contract.AdjustmentType == AdjustmentType.ICL ? IndexType.ICL : IndexType.IPC;
            // Index lookback (audit C-1, resolved with product as "T-4 literal"):
            //   ICL → 12 months. The Ley de Alquileres ICL adjustment is a year-over-year ratio
            //         (ICL_T / ICL_{T-4 quarters} = ICL_T / ICL_{12 months ago}), independent of how
            //         often the rent is adjusted (the adjustment *cadence* lives in the scheduler).
            //   IPC → accumulate the variation over the contract's adjustment period (frequency).
            // NOTE: this lookback is the *index* window, not the adjustment cadence. The indices-api
            // projection still uses a frequency-step window for ICL and will diverge for ICL until its
            // calculator separates step from lookback (cross-repo follow-up — audit C-7).
            int lookbackMonths = indexType == IndexType.ICL
                ? 12
                : contract.AdjustmentFrequency.ToMonths();

            var periodT = new DateOnly(effectiveDate.Year, effectiveDate.Month, 1);
            var periodBase = periodT.AddMonths(-lookbackMonths);

            var currentIndex = await _indexRepo.GetByPeriodAsync(indexType, periodT, ct);
            if (currentIndex is null)
                throw new BusinessException($"No hay índice {indexType} disponible para {periodT:yyyy-MM}. Sincronice los índices primero.");

            var baseIndex = await _indexRepo.GetByPeriodAsync(indexType, periodBase, ct);
            if (baseIndex is null)
                throw new BusinessException($"No hay índice {indexType} disponible para {periodBase:yyyy-MM} (período base). Sincronice los índices primero.");

            // Compute the new rent from the raw index ratio. Do NOT pre-round the factor and then
            // multiply: rounding the ratio to 6 decimals first skews the amount (up to ~0.10 ARS on
            // a 200k rent) and that error compounds across successive adjustments, since each
            // adjustment reads the previous (already-rounded) rent as its base (audit C-2). The
            // rounded factor is persisted only as informational metadata on RentHistory.
            newRent = Math.Round(previousRent * currentIndex.Value / baseIndex.Value, 2);
            factor = Math.Round(currentIndex.Value / baseIndex.Value, 6);
            indexValueId = currentIndex.Id;
        }

        // Update contract rent
        contract.MonthlyRent = newRent;

        // Create history record
        var history = new Entities.RentHistory
        {
            OrganizationId = contract.OrganizationId,
            ContractId = contract.Id,
            PreviousRent = previousRent,
            NewRent = newRent,
            AdjustmentFactor = factor,
            AdjustmentType = contract.AdjustmentType,
            IndexValueId = indexValueId,
            EffectiveDate = effectiveDate,
            Notes = request.Notes?.Trim(),
        };
        await _historyRepo.AddAsync(history, ct);

        // Create RentCharge transaction for the new period — a charge owed by the tenant.
        var tx = new Entities.Transaction
        {
            OrganizationId = contract.OrganizationId,
            ContractId = contract.Id,
            Type = TransactionType.RentCharge,
            Amount = newRent,
            Currency = contract.Currency,
            Period = effectiveDate,
            Status = TransactionStatus.Pending,
            DueDate = Entities.Transaction.DueDateFor(effectiveDate, contract.DayOfMonth),
            Notes = $"Ajuste {contract.AdjustmentType} · factor {factor}",
        };
        await _txRepo.AddAsync(tx, ct);

        await _contractRepo.SaveChangesAsync(ct);

        // Step 7: notify tenant by email if they have an address
        if (!string.IsNullOrWhiteSpace(contract.AppTenant?.Email))
        {
            try
            {
                await _email.SendRentAdjustmentNotificationAsync(
                    toEmail: contract.AppTenant.Email,
                    tenantName: $"{contract.AppTenant.FirstName} {contract.AppTenant.LastName}",
                    propertyAddress: $"{contract.Property?.Address}",
                    previousRent: previousRent,
                    newRent: newRent,
                    adjustmentType: contract.AdjustmentType,
                    adjustmentFactor: factor,
                    effectiveDate: effectiveDate,
                    ct: ct);
            }
            catch (Exception ex)
            {
                // Email failure must never roll back the adjustment
                _logger.LogWarning(ex, "Rent adjustment saved but email notification failed for contract {ContractId}", contract.Id);
            }
        }

        return ToDto(history);
    }

    internal static RentHistoryDto ToDto(Entities.RentHistory r) =>
        new(r.Id, r.ContractId, r.PreviousRent, r.NewRent, r.AdjustmentFactor,
            r.AdjustmentType, r.IndexValueId, r.EffectiveDate, r.Notes, r.CreatedAt);
}
