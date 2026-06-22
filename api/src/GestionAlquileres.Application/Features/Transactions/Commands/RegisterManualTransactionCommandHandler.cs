using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

public class RegisterManualTransactionCommandHandler : IRequestHandler<RegisterManualTransactionCommand, TransactionDto>
{
    private readonly IContractRepository _contractRepo;
    private readonly ITransactionRepository _txRepo;

    public RegisterManualTransactionCommandHandler(IContractRepository contractRepo, ITransactionRepository txRepo)
    {
        _contractRepo = contractRepo;
        _txRepo = txRepo;
    }

    public async Task<TransactionDto> Handle(RegisterManualTransactionCommand request, CancellationToken ct)
    {
        var contract = await _contractRepo.GetByIdAsync(request.ContractId, ct)
            ?? throw new BusinessException("Contrato no encontrado.");

        // A ManualDebit is a charge owed by the tenant (Pending + due date); a ManualCredit is in
        // the tenant's favour and is settled on creation.
        var isCharge = request.Type == TransactionType.ManualDebit;
        var tx = new Transaction
        {
            OrganizationId = contract.OrganizationId,
            ContractId = contract.Id,
            Type = request.Type,
            Amount = request.Amount,
            Currency = contract.Currency,
            Period = request.Period,
            Notes = request.Notes.Trim(),
            Status = isCharge ? TransactionStatus.Pending : TransactionStatus.Paid,
            DueDate = isCharge ? Transaction.DueDateFor(request.Period, contract.DayOfMonth) : null,
            PaidAt = isCharge ? null : DateTimeOffset.UtcNow,
        };

        await _txRepo.AddAsync(tx, ct);
        await _txRepo.SaveChangesAsync(ct);

        return RegisterPaymentCommandHandler.ToDto(tx);
    }
}
