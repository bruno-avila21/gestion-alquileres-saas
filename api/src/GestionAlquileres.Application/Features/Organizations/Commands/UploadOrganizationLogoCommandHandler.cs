using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Organizations.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Commands;

public class UploadOrganizationLogoCommandHandler : IRequestHandler<UploadOrganizationLogoCommand, OrganizationDto>
{
    public const long MaxSizeBytes = 2 * 1024 * 1024; // 2 MB

    // Sólo formatos raster: sirven directo en el encabezado del PDF y en la vista previa del panel.
    public static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp",
    };

    private readonly IOrganizationRepository _repo;
    private readonly IStorageService _storage;
    private readonly ICurrentTenant _currentTenant;

    public UploadOrganizationLogoCommandHandler(
        IOrganizationRepository repo, IStorageService storage, ICurrentTenant currentTenant)
    {
        _repo = repo;
        _storage = storage;
        _currentTenant = currentTenant;
    }

    public async Task<OrganizationDto> Handle(UploadOrganizationLogoCommand request, CancellationToken ct)
    {
        // Regla de negocio (no de forma): el contrato pide BusinessException -> 409 para el logo,
        // no una ValidationException -> 400 como el resto de los campos de la marca.
        if (!AllowedMimeTypes.Contains(request.MimeType))
            throw new BusinessException("Formato de logo no permitido. Aceptados: PNG, JPG y WebP.");

        if (request.SizeBytes <= 0 || request.SizeBytes > MaxSizeBytes)
            throw new BusinessException("El logo puede pesar hasta 2 MB.");

        var org = await _repo.GetByIdAsync(_currentTenant.OrganizationId, ct)
            ?? throw new InvalidOperationException("La organización del token no existe.");

        var previousKey = org.LogoStorageKey;

        org.LogoStorageKey = await _storage.UploadAsync(request.Content, request.FileName, request.MimeType, ct);
        await _repo.SaveChangesAsync(ct);

        // Se borra DESPUÉS de confirmar el nuevo: si el upload falla, el logo anterior sigue servible.
        if (!string.IsNullOrWhiteSpace(previousKey))
            await _storage.DeleteAsync(previousKey, ct);

        return OrganizationDto.From(org);
    }
}
