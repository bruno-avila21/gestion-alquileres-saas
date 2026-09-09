using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

/// <summary>
/// Cambio de contraseña del propio usuario autenticado. El <paramref name="UserId"/> lo pone el
/// controller desde el claim del JWT, nunca el cliente.
/// </summary>
public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<AuthResponseDto>;
