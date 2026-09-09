namespace GestionAlquileres.API.Contracts;

/// <summary>
/// <see cref="Website"/> is a honeypot: a real visitor never fills it (hidden off-screen with CSS,
/// not display:none, so basic bots that skip hidden/display:none fields still fall for it). Any
/// content there means the submission is dropped silently — the client is never told why.
/// </summary>
public record CreatePublicLeadRequest(string Name, string? Email, string? Phone, string Message, Guid? ListingId, string? Website);
