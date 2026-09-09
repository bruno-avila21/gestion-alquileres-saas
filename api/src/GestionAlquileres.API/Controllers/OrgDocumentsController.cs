using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Documents;
using GestionAlquileres.Application.Features.Documents.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/documents")]
public class OrgDocumentsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<DocumentDto>>> ListAll(
        CancellationToken ct, int page = 1, int pageSize = 20, string? search = null) =>
        Ok(await Mediator.Send(new GetDocumentsPageQuery(page, pageSize, search), ct));
}
