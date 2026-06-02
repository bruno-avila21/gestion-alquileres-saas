using GestionAlquileres.Application.Features.Auth.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

/// <summary>
/// Issues and persists a refresh token for a just-authenticated user. Called by the controller
/// right after login/register/tenant-login (additive — the existing login flow is unchanged).
/// </summary>
public record IssueRefreshTokenCommand(Guid UserId, Guid OrganizationId) : IRequest<RefreshTokenResult>;
