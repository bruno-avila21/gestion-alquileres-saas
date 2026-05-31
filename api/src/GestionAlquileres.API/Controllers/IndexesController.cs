using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.Indexes.Commands;
using GestionAlquileres.Application.Features.Indexes.DTOs;
using GestionAlquileres.Application.Features.Indexes.Queries;
using GestionAlquileres.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/indexes")]
public class IndexesController : AdminControllerBase
{
    /// <summary>Historical index values by type and date range. IDX-06.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IndexValueDto>>> Get(
        [FromQuery] IndexType type,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new GetIndexByPeriodQuery(type, from, to), ct);
        return Ok(result);
    }

    /// <summary>Trigger external sync for one (type, period). IDX-05.</summary>
    [HttpPost("sync")]
    public async Task<ActionResult<SyncIndexResult>> Sync(
        [FromBody] SyncIndexRequest request,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new SyncIndexCommand(request.IndexType, request.Period), ct);
        return Ok(result);
    }
}
