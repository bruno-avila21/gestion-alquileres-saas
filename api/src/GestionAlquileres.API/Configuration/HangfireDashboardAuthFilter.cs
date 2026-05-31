using Hangfire.Dashboard;

namespace GestionAlquileres.API.Configuration;

/// <summary>
/// Restricts the Hangfire dashboard. In Development it is open for convenience; otherwise it
/// requires an authenticated user with the Admin role (the JWT cookie is sent on same-origin
/// browser requests). Prevents the dashboard from leaking job data publicly.
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    private readonly bool _isDevelopment;
    public HangfireDashboardAuthFilter(bool isDevelopment) => _isDevelopment = isDevelopment;

    public bool Authorize(DashboardContext context)
    {
        if (_isDevelopment) return true;
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole("Admin");
    }
}
