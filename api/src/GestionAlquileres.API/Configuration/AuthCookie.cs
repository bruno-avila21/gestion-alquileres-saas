namespace GestionAlquileres.API.Configuration;

/// <summary>
/// Shared definition for the HttpOnly authentication cookie.
/// The JWT is delivered to browsers via this cookie (not readable by JS), while
/// non-browser API clients may still use the Authorization: Bearer header.
/// </summary>
public static class AuthCookie
{
    public const string Name = "access_token";

    /// <summary>
    /// Refresh-token cookie. Scoped to the auth path so it is NOT sent on every API call —
    /// it only travels to /refresh and /logout, shrinking the exposure window.
    /// </summary>
    public const string RefreshName = "refresh_token";
    public const string RefreshPath = "/api/v1/auth";
}
