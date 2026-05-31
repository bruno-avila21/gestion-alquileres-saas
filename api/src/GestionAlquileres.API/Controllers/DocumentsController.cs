using GestionAlquileres.Application.Features.Documents;
using GestionAlquileres.Application.Features.Documents.Commands;
using GestionAlquileres.Application.Features.Documents.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/contracts/{contractId:guid}/documents")]
public class DocumentsController : BaseController
{
    // Management of contract documents is staff-only. The download-url endpoint stays
    // accessible to any authenticated user (incl. Tenant) because the tenant portal
    // downloads its own documents through it — ownership is enforced in the handler.
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List(Guid contractId, CancellationToken ct) =>
        Ok(await Mediator.Send(new ListContractDocumentsQuery(contractId), ct));

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<ActionResult<DocumentDto>> Upload(
        Guid contractId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file provided.");

        await using var stream = file.OpenReadStream();
        var cmd = new UploadDocumentCommand(
            ContractId: contractId,
            FileName: Path.GetFileName(file.FileName),
            MimeType: file.ContentType,
            SizeBytes: file.Length,
            Content: stream,
            UploadedByUserId: CurrentUserId);

        var dto = await Mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetDownloadUrl), new { contractId, docId = dto.Id }, dto);
    }

    [HttpGet("{docId:guid}/download-url")]
    public async Task<ActionResult<DocumentDownloadUrlDto>> GetDownloadUrl(
        Guid contractId, Guid docId, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new GetDocumentDownloadUrlQuery(docId, contractId, CurrentUserId, IsStaff), ct);
        if (result is null) return NotFound();

        // Build absolute URL from relative path returned by handler
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{result.Url}";
        return Ok(result with { Url = absoluteUrl });
    }

    [HttpDelete("{docId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Delete(Guid contractId, Guid docId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteDocumentCommand(docId), ct);
        return NoContent();
    }
}
