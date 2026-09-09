namespace GestionAlquileres.Application.Common.DTOs;

/// <summary>
/// A page of results plus the total count, for server-side pagination (audit M10). Replaces the
/// hard Take(500) on org-wide listings so admins can page through the whole dataset and the DB only
/// materializes one page (supported by the (OrganizationId, …) indexes added in the A4 migration).
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
