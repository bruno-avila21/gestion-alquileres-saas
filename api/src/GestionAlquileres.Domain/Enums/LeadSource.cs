namespace GestionAlquileres.Domain.Enums;

/// <summary>De dónde llegó la consulta.</summary>
public enum LeadSource
{
    /// <summary>Formulario público del sitio (ficha de una publicación o "Contacto" del home).</summary>
    Website,

    /// <summary>Carga manual desde el panel (teléfono, WhatsApp, visita espontánea, etc.).</summary>
    Manual,
}
