using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

public class MarkTransactionPaidCommandHandler : IRequestHandler<MarkTransactionPaidCommand, TransactionDto>
{
    private readonly ITransactionRepository _txRepo;

    public MarkTransactionPaidCommandHandler(ITransactionRepository txRepo) => _txRepo = txRepo;

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

        tx.Status = TransactionStatus.Paid;
        tx.PaidAt = DateTimeOffset.UtcNow;
        await _txRepo.SaveChangesAsync(ct);

        return RegisterPaymentCommandHandler.ToDto(tx);
    }
}
