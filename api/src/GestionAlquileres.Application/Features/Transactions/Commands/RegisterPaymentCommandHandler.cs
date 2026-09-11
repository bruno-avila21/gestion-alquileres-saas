using GestionAlquileres.Application.Common.Billing;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

public class RegisterPaymentCommandHandler : IRequestHandler<RegisterPaymentCommand, TransactionDto>
{
    private readonly IContractRepository _contractRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly ILateFeeAccrualService _lateFees;

    public RegisterPaymentCommandHandler(
        IContractRepository contractRepo, ITransactionRepository txRepo, ILateFeeAccrualService lateFees)
    {
        _contractRepo = contractRepo;
        _txRepo = txRepo;
        _lateFees = lateFees;
    }

    public async Task<TransactionDto> Handle(RegisterPaymentCommand request, CancellationToken ct)
    {
        var contract = await _contractRepo.GetByIdAsync(request.ContractId, ct)
            ?? throw new BusinessException("Contrato no encontrado.");

        var tx = new Transaction
        {
            OrganizationId = contract.OrganizationId,
            ContractId = contract.Id,
            Type = TransactionType.Payment,
            Amount = request.Amount,
            Currency = contract.Currency,
            Period = request.Period,
            Notes = request.Notes?.Trim(),
            // A payment is settled the moment it's registered.
            Status = TransactionStatus.Paid,
            PaidAt = DateTimeOffset.UtcNow,
        };

        await _txRepo.AddAsync(tx, ct);

        // Reconciliation (audit A1): a charge's Status is the single source of truth for what the
        // tenant owes and what the owner has collected. Registering a payment must therefore settle
        // the matching charge(s), otherwise the charge stays Pending and shows up as false morosidad
        // while the payment sits as an unlinked credit. Allocate the payment greedily: the charge for
        // the paid period first (the common "pago del mes" case), then the oldest pending charges.
        // Charges only fully covered by the remaining amount are marked Paid (no partial settlement).
        // El punitorio se devenga hasta HOY antes de imputar, por dos motivos: queda congelado en el
        // día del pago (y no en el de la próxima corrida del job, que le sumaría un día que el
        // inquilino no debe), y entra en la cola de imputación de este mismo pago en vez de quedar
        // colgado como deuda nueva justo después de cobrar.
        await AccrueLateFeesAsync(contract, ct);

        var pending = (await _txRepo.GetPendingChargesAsync(contract.Id, ct))
            .OrderByDescending(c => c.Period == request.Period)
            .ThenBy(c => c.Period)
            .ThenBy(c => c.CreatedAt);

        var remaining = request.Amount;
        foreach (var charge in pending)
        {
            if (remaining < charge.Amount) continue;
            charge.Status = TransactionStatus.Paid;
            charge.PaidAt = tx.PaidAt;
            remaining -= charge.Amount;
            if (remaining <= 0m) break;
        }

        await _txRepo.SaveChangesAsync(ct);

        return ToDto(tx);
    }

    /// <summary>
    /// Devenga el punitorio de todos los cargos impagos del contrato hasta hoy. Compartido por los
    /// dos caminos de cobro para que ambos congelen el punitorio en el mismo momento.
    /// </summary>
    internal static async Task AccrueLateFeesAsync(
        Contract contract, ITransactionRepository txRepo, ILateFeeAccrualService lateFees, CancellationToken ct)
    {
        if (contract.LateFeeDailyRate is not { } rate || rate <= 0m) return;

        var today = ArgentinaTime.Today;
        foreach (var charge in await txRepo.GetPendingChargesAsync(contract.Id, ct))
            await lateFees.AccrueAsync(charge, rate, contract.LateFeeGraceDays, today, ct);
    }

    private Task AccrueLateFeesAsync(Contract contract, CancellationToken ct) =>
        AccrueLateFeesAsync(contract, _txRepo, _lateFees, ct);

    /// <summary>
    /// Shared mapper. Derives the Overdue view state on read (a Pending charge past its due date)
    /// rather than persisting it, so the stored status stays Pending/Paid/Cancelled.
    /// </summary>
    internal static TransactionDto ToDto(Transaction t)
    {
        var status = t.Status == TransactionStatus.Pending
                     && t.DueDate is { } due && due < ArgentinaTime.Today
            ? TransactionStatus.Overdue
            : t.Status;

        return new(t.Id, t.ContractId, t.Type, t.Amount, t.Currency, t.Period, t.Notes, t.CreatedAt,
            status, t.DueDate, t.PaidAt);
    }
}
