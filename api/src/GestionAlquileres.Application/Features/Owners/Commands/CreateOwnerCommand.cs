using GestionAlquileres.Application.Features.Owners.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Commands;

public record CreateOwnerCommand(
    Guid OrganizationId,
    string Name,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Cbu,
    string? Notes)
    : IRequest<OwnerDto>;
