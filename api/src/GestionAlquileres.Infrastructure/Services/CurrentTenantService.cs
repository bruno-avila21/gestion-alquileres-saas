using System.Security.Claims;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace GestionAlquileres.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenant
{
    /// <summary>
    /// HttpContext.Items key under which TenantMiddleware stores the organization resolved from the
    /// public-site slug (/api/v1/public/{slug}/...). Anonymous requests have no JWT, so this is the
    /// only way the global query filters get a tenant on the public site.
    /// </summary>
    public const string PublicOrgItemKey = "public_org_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid OrganizationId
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            var claim = ctx?.User.FindFirstValue("org_id");
            if (Guid.TryParse(claim, out var id)) return id;

            // The JWT always wins: a logged-in user browsing the public site of another org must not
            // have their queries re-scoped by the URL.
            if (ctx?.Items.TryGetValue(PublicOrgItemKey, out var item) == true && item is Guid publicOrg)
                return publicOrg;

            return Guid.Empty;
        }
    }
}
