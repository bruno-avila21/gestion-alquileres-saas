using GestionAlquileres.Application.Features.Documents;
using GestionAlquileres.Application.Features.Documents.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/documents")]
public class OrgDocumentsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> ListAll(CancellationToken ct) =>
        Ok(await Mediator.Send(new ListAllDocumentsQuery(), ct));
}
