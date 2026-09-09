using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

/// <summary>
/// La liquidación del período para TODOS los propietarios de la organización, de una.
///
/// La pantalla de rendiciones venía resolviendo un propietario por vez, que sirve para
/// imprimir un comprobante pero no para la pregunta con la que arranca el mes: cuánto hay
/// que transferir en total y a quién. Sin esta consulta, esa vista tendría que pedir N
/// liquidaciones desde el navegador.
/// </summary>
public record GetAllOwnerSettlementsQuery(DateOnly PeriodFrom, DateOnly PeriodTo)
    : IRequest<IReadOnlyList<OwnerSettlementDto>>;

public class GetAllOwnerSettlementsQueryHandler
    : IRequestHandler<GetAllOwnerSettlementsQuery, IReadOnlyList<OwnerSettlementDto>>
{
    private readonly IOwnerRepository _owners;
    private readonly ITransactionRepository _txRepo;

    public GetAllOwnerSettlementsQueryHandler(IOwnerRepository owners, ITransactionRepository txRepo)
    {
        _owners = owners;
        _txRepo = txRepo;
    }

    public async Task<IReadOnlyList<OwnerSettlementDto>> Handle(
        GetAllOwnerSettlementsQuery request, CancellationToken ct)
    {
        GetOwnerSettlementQueryHandler.ValidatePeriod(request.PeriodFrom, request.PeriodTo);

        var owners = await _owners.GetAllAsync(ct);
        var result = new List<OwnerSettlementDto>(owners.Count);

        // Una consulta agregada por propietario. Es N+0 y no N+1 —la agregación pesada ya la
        // hace la base en GetCollectedByOwnerAsync— y N acá es la cantidad de propietarios de
        // una inmobiliaria, no de contratos. Si alguna llega a tener cientos, esto pasa a ser
        // una sola consulta agrupada por owner; hoy sería optimizar antes de tener el problema.
        foreach (var owner in owners)
        {
            var rows = await _txRepo.GetCollectedByOwnerAsync(
                owner.Id, request.PeriodFrom, request.PeriodTo, ct);

            var dto = GetOwnerSettlementQueryHandler.BuildDto(
                owner, request.PeriodFrom, request.PeriodTo, rows);

            // Un propietario sin cobranzas en el período no es una fila vacía de la tabla:
            // es alguien que no entra en la liquidación de este mes.
            if (dto.Lines.Count > 0) result.Add(dto);
        }

        return result;
    }
}
