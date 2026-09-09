namespace GestionAlquileres.Application.Common;

/// <summary>
/// Rutas públicas (anónimas) del sitio de cada inmobiliaria. Las fotos de las fichas se sirven por
/// acá y nunca por URL directa al storage, igual que los documentos privados pero sin token: son
/// contenido público por definición.
/// </summary>
public static class PublicUrls
{
    public static string Photo(string orgSlug, Guid photoId) => $"/api/v1/public/{orgSlug}/photos/{photoId}";
}
