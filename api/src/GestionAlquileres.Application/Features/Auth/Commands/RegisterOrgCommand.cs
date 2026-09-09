using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public record RegisterOrgCommand(
    string OrganizationName,
    string Slug,
    string AdminEmail,
    string AdminPassword,
    string AdminFirstName,
    string AdminLastName,
    /// <summary>Requerido cuando Registration:Mode es InviteCode.</summary>
    string? InviteCode = null) : IRequest<AuthResponseDto>;
