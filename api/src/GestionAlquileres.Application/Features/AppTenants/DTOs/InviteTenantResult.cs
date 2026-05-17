namespace GestionAlquileres.Application.Features.AppTenants.DTOs;

public record InviteTenantResult(
    AppTenantDto Tenant,
    string TempPassword);
