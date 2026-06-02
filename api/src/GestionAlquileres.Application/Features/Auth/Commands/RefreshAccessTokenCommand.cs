using GestionAlquileres.Application.Features.Auth.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

/// <summary>Exchanges a valid (non-expired, non-revoked) refresh token for a fresh access token, rotating it.</summary>
public record RefreshAccessTokenCommand(string RawToken) : IRequest<RefreshAccessTokenResult>;
