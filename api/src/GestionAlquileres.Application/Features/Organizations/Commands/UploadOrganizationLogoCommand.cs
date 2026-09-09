using GestionAlquileres.Application.Features.Organizations.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Commands;

public record UploadOrganizationLogoCommand(
    string FileName,
    string MimeType,
    long SizeBytes,
    Stream Content) : IRequest<OrganizationDto>;
