using GestionAlquileres.Application.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/dashboard")]
public class DashboardController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetDashboardQuery(), ct));
}
