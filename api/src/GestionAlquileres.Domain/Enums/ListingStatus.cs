namespace GestionAlquileres.Domain.Enums;

/// <summary>Ciclo de vida de una publicación. Sólo <see cref="Published"/> es visible en el sitio público.</summary>
public enum ListingStatus
{
    Draft,
    Published,
    Reserved,
    Sold,
    Rented,
    Paused,
}
