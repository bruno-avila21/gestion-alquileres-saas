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
        // In background jobs (Hangfire) ICurrentTenant returns Guid.Empty — fall back to raw lookup.
        var contract = (_currentTenant.OrganizationId == Guid.Empty
            ? await _contractRepo.GetByIdRawAsync(request.ContractId, ct)
            : await _contractRepo.GetByIdAsync(request.ContractId, ct))
            ?? throw new BusinessException("Contrato no encontrado.");

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
        else
        {
            var indexType = contract.AdjustmentType == AdjustmentType.ICL ? IndexType.ICL : IndexType.IPC;
            int lookbackMonths = contract.AdjustmentFrequency switch
            {
                AdjustmentFrequency.Monthly => 1,
                AdjustmentFrequency.Quarterly => 3,
                AdjustmentFrequency.Annual => 12,
                _ => 3,
            };

            var periodT = new DateOnly(effectiveDate.Year, effectiveDate.Month, 1);
            var periodBase = periodT.AddMonths(-lookbackMonths);

            var currentIndex = await _indexRepo.GetByPeriodAsync(indexType, periodT, ct);
            if (currentIndex is null)
                throw new BusinessException($"No hay índice {indexType} disponible para {periodT:yyyy-MM}. Sincronice los índices primero.");

            var baseIndex = await _indexRepo.GetByPeriodAsync(indexType, periodBase, ct);
            if (baseIndex is null)
                throw new BusinessException($"No hay índice {indexType} disponible para {periodBase:yyyy-MM} (período base). Sincronice los índices primero.");

            factor = Math.Round(currentIndex.Value / baseIndex.Value, 6);
            newRent = Math.Round(previousRent * factor, 2);
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

        // Create RentCharge transaction for the new period
        var tx = new Entities.Transaction
        {
            OrganizationId = contract.OrganizationId,
            ContractId = contract.Id,
            Type = TransactionType.RentCharge,
            Amount = newRent,
            Currency = contract.Currency,
            Period = effectiveDate,
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
