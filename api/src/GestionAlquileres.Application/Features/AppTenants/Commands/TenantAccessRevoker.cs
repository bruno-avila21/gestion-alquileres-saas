using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

/// <summary>
/// Corta el acceso al portal del inquilino cuando se lo da de baja o se lo desactiva.
///
/// Antes, dar de baja un inquilino sólo ponía <c>AppTenant.IsActive = false</c>. El login del
/// portal valida <c>User.IsActive</c>, no el del inquilino, así que un ex-inquilino seguía
/// entrando indefinidamente y viendo su contrato, sus pagos y sus documentos. Era una revocación
/// que la interfaz prometía y el backend no cumplía.
///
/// Revocar los refresh tokens es lo que realmente termina la sesión: el access token es
/// autocontenido y sigue valiendo hasta que expira (ver JwtSettings.AccessTokenMinutes), pero sin
/// refresh no se puede renovar.
/// </summary>
internal static class TenantAccessRevoker
{
    public static async Task RevokeAsync(
        AppTenant tenant,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        CancellationToken ct)
    {
        if (tenant.UserId is not { } userId) return;

        var user = await users.GetByIdAsync(userId, ct);
        if (user is not null) user.IsActive = false;

        await refreshTokens.RevokeAllForUserAsync(userId, ct);
    }
}
