using GestionAlquileres.Application.Features.Owners.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Commands;

public record UpdateOwnerCommand(
    Guid Id,
    string Name,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Cbu,
    string? Notes,
    bool IsActive)
    : IRequest<OwnerDto>;
