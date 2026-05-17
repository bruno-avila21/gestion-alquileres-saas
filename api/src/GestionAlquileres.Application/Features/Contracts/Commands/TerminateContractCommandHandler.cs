using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

public class TerminateContractCommandHandler : IRequestHandler<TerminateContractCommand, ContractDto?>
{
    private readonly IContractRepository _repo;

    public TerminateContractCommandHandler(IContractRepository repo) => _repo = repo;

    public async Task<ContractDto?> Handle(TerminateContractCommand request, CancellationToken ct)
    {
        var contract = await _repo.GetByIdAsync(request.Id, ct);
        if (contract is null) return null;

        if (contract.Status == ContractStatus.Terminated)
            throw new BusinessException("Este contrato ya está rescindido.");

        contract.Status = ContractStatus.Terminated;
        contract.TerminatedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            contract.Notes = request.Notes.Trim();

        await _repo.SaveChangesAsync(ct);

        var full = await _repo.GetByIdAsync(contract.Id, ct);
        return CreateContractCommandHandler.ToDto(full!);
    }
}
