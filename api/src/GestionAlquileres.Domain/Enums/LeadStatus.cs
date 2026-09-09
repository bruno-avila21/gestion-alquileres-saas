namespace GestionAlquileres.Domain.Enums;

/// <summary>Estado del lead en el embudo de ventas. El orden es el orden de las columnas del kanban.</summary>
public enum LeadStatus
{
    New,
    Contacted,
    Visit,
    Negotiation,
    Won,
    Lost,
}
