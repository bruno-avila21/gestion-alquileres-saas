using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[ApiController]
[Route("api/v1/files")]
[AllowAnonymous]
public class FilesController : ControllerBase
{
    private readonly IDocumentTokenService _tokenSvc;
    private readonly IDocumentRepository _docs;
    private readonly IStorageService _storage;

    public FilesController(
        IDocumentTokenService tokenSvc,
        IDocumentRepository docs,
        IStorageService storage)
    {
        _tokenSvc = tokenSvc;
        _docs = docs;
        _storage = storage;
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download([FromQuery] string? token, CancellationToken ct)
    {
        var result = _tokenSvc.Validate(token ?? "");
        if (result is null) return Unauthorized();

        var (docId, orgId) = result.Value;
        var doc = await _docs.GetByIdRawAsync(docId, orgId, ct);
        if (doc is null) return NotFound();

        var stream = await _storage.DownloadAsync(doc.StorageKey, ct);
        return File(stream, doc.MimeType, doc.FileName);
    }
}
