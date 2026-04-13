using System.Security.Claims;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace GestionAlquileres.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid OrganizationId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirstValue("org_id");
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
}
