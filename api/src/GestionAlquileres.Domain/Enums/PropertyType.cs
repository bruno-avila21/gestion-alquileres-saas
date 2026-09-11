namespace GestionAlquileres.Domain.Enums;

public enum PropertyType
{
    House,
    Apartment,
    Commercial,
    Land,
    Other,
    /// <summary>Propiedad horizontal: casa/depto sin expensas ni consorcio. Tipo muy usado en AMBA.</summary>
    PH,
    Office,
}
