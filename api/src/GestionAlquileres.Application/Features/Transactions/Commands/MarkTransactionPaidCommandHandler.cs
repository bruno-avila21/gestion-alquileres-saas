using GestionAlquileres.Application.Common.Billing;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

public class MarkTransactionPaidCommandHandler : IRequestHandler<MarkTransactionPaidCommand, TransactionDto>
{
    private readonly ITransactionRepository _txRepo;
    private readonly IContractRepository _contractRepo;
    private readonly ILateFeeAccrualService _lateFees;

    public MarkTransactionPaidCommandHandler(
        ITransactionRepository txRepo, IContractRepository contractRepo, ILateFeeAccrualService lateFees)
    {
        _txRepo = txRepo;
        _contractRepo = contractRepo;
        _lateFees = lateFees;
    }

    public async Task<TransactionDto> Handle(MarkTransactionPaidCommand request, CancellationToken ct)
    {
        // GetByIdAsync is tenant-filtered, so a cross-org id simply resolves to null.
        var tx = await _txRepo.GetByIdAsync(request.TransactionId, ct);
        if (tx is null || tx.ContractId != request.ContractId)
            throw new BusinessException("Transacción no encontrada.");

        if (!tx.IsCharge)
            throw new BusinessException("Solo se pueden saldar cargos (alquiler o débitos manuales).");
        if (tx.Status == TransactionStatus.Paid)
            throw new BusinessException("La transacción ya está saldada.");
        if (tx.Status == TransactionStatus.Cancelled)
            throw new BusinessException("No se puede saldar una transacción cancelada.");

        // Devengar ANTES de saldar: una vez que el cargo pasa a Paid deja de devengar, y el
        // punitorio quedaría congelado en la última corrida del job en vez de en el día del pago.
        // Saldar el cargo no salda su punitorio — es deuda aparte, y sigue exigible.
        var contract = await _contractRepo.GetByIdAsync(tx.ContractId, ct);
        if (contract is not null)
            await RegisterPaymentCommandHandler.AccrueLateFeesAsync(contract, _txRepo, _lateFees, ct);

        var paidAt = DateTimeOffset.UtcNow;
        tx.Status = TransactionStatus.Paid;
        tx.PaidAt = paidAt;

        // Record the money-in so the cash ledger stays consistent across both settlement paths
        // (audit A1): owner settlement and the tenant balance count Payment inflows, so marking a
        // charge paid without a matching Payment would silently drop it from those totals.
        var payment = new Transaction
        {
            OrganizationId = tx.OrganizationId,
            ContractId = tx.ContractId,
            Type = TransactionType.Payment,
            Amount = tx.Amount,
            Currency = tx.Currency,
            Period = tx.Period,
            Status = TransactionStatus.Paid,
            PaidAt = paidAt,
            Notes = "Pago registrado al saldar el cargo",
        };
        await _txRepo.AddAsync(payment, ct);
        await _txRepo.SaveChangesAsync(ct);

        return RegisterPaymentCommandHandler.ToDto(tx);
    }
}
