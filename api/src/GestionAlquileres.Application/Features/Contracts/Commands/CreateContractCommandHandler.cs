using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, ContractDto>
{
    private readonly IContractRepository _repo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly IAppTenantRepository _tenantRepo;

    public CreateContractCommandHandler(
        IContractRepository repo,
        IPropertyRepository propertyRepo,
        IAppTenantRepository tenantRepo)
    {
        _repo = repo;
        _propertyRepo = propertyRepo;
        _tenantRepo = tenantRepo;
    }

    public async Task<ContractDto> Handle(CreateContractCommand request, CancellationToken ct)
    {
        if (!await _propertyRepo.ExistsAsync(request.PropertyId, ct))
            throw new Common.Exceptions.BusinessException("Propiedad no encontrada en esta organización.");

        if (!await _tenantRepo.ExistsAsync(request.AppTenantId, ct))
            throw new Common.Exceptions.BusinessException("Inquilino no encontrado en esta organización.");

        if (await _repo.HasActiveOverlapAsync(request.PropertyId, request.StartDate, request.EndDate, null, ct))
            throw new Common.Exceptions.BusinessException(
                "Ya existe un contrato activo para esta propiedad cuyo período se solapa con el indicado.");

        var contract = new Contract
        {
            OrganizationId = request.OrganizationId,
            PropertyId = request.PropertyId,
            AppTenantId = request.AppTenantId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MonthlyRent = request.MonthlyRent,
            Currency = request.Currency,
            AdjustmentType = request.AdjustmentType,
            AdjustmentFrequency = request.AdjustmentFrequency,
            AdjustmentPercent = request.AdjustmentPercent,
            DayOfMonth = request.DayOfMonth,
            DepositAmount = request.DepositAmount,
            Notes = request.Notes?.Trim(),
        };

        await _repo.AddAsync(contract, ct);
        await _repo.SaveChangesAsync(ct);

        var full = await _repo.GetByIdAsync(contract.Id, ct);
        return ToDto(full!);
    }

    internal static ContractDto ToDto(Contract c) =>
        new(c.Id, c.OrganizationId,
            c.PropertyId, c.Property?.Address ?? "", c.Property?.City ?? "",
            c.AppTenantId, $"{c.AppTenant?.FirstName} {c.AppTenant?.LastName}".Trim(),
            c.StartDate, c.EndDate, c.MonthlyRent, c.Currency,
            c.AdjustmentType, c.AdjustmentFrequency, c.AdjustmentPercent, c.DayOfMonth,
            c.DepositAmount, c.Status, c.Notes, c.CreatedAt, c.TerminatedAt);
}
