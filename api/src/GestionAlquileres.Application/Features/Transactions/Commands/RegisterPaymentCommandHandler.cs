using GestionAlquileres.Application.Common.Exceptions;
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

    public RegisterPaymentCommandHandler(IContractRepository contractRepo, ITransactionRepository txRepo)
    {
        _contractRepo = contractRepo;
        _txRepo = txRepo;
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
        };

        await _txRepo.AddAsync(tx, ct);
        await _txRepo.SaveChangesAsync(ct);

        return ToDto(tx);
    }

    internal static TransactionDto ToDto(Transaction t) =>
        new(t.Id, t.ContractId, t.Type, t.Amount, t.Currency, t.Period, t.Notes, t.CreatedAt);
}
