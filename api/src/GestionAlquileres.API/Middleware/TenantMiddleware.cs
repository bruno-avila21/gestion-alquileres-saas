namespace GestionAlquileres.API.Middleware;

/// <summary>
/// Placeholder middleware. In current design the org_id claim is extracted on
/// demand by CurrentTenantService via IHttpContextAccessor — no imperative
/// enrichment needed. This middleware exists as an explicit extension point for
/// future tenant-resolution strategies (subdomain routing, header-based, etc.).
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    public TenantMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context) => _next(context);
}
