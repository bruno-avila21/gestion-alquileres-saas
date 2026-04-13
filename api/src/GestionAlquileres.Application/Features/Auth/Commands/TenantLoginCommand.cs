using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public record TenantLoginCommand(string Email, string Password, string OrganizationSlug)
    : IRequest<AuthResponseDto>;
