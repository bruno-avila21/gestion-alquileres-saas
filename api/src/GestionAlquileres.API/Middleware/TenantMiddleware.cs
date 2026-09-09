using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Infrastructure.Services;

namespace GestionAlquileres.API.Middleware;

/// <summary>
/// Tenant resolution. Authenticated requests carry the tenant in the JWT (org_id claim) and need no
/// help. The public site (/api/v1/public/{slug}/...) is anonymous, so the organization is resolved
/// from the slug here and handed to <see cref="CurrentTenantService"/> through HttpContext.Items —
/// the global query filters then scope every public query to that organization, with no
/// IgnoreQueryFilters anywhere.
/// </summary>
public class TenantMiddleware
{
    private const string PublicPrefix = "/api/v1/public/";

    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IOrganizationRepository orgs)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith(PublicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var slug = path[PublicPrefix.Length..].Split('/', 2)[0];
            if (slug.Length > 0)
            {
                var org = await orgs.GetBySlugAsync(slug.ToLowerInvariant(), context.RequestAborted);
                if (org is null || !org.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                context.Items[CurrentTenantService.PublicOrgItemKey] = org.Id;
            }
        }

        await _next(context);
    }
}
