namespace GestionAlquileres.API.Configuration;

/// <summary>
/// Shared definition for the HttpOnly authentication cookie.
/// The JWT is delivered to browsers via this cookie (not readable by JS), while
/// non-browser API clients may still use the Authorization: Bearer header.
/// </summary>
public static class AuthCookie
{
    public const string Name = "access_token";
}
