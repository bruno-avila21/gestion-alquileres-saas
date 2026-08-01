namespace GestionAlquileres.Domain.Enums;

/// <summary>
/// Cómo se calcula la actualización del alquiler.
///
/// Se persiste como entero: los miembros nuevos van SIEMPRE al final.
/// </summary>
public enum AdjustmentType
{
    /// <summary>Índice para Contratos de Locación (BCRA). Relación interanual.</summary>
    ICL = 0,

    /// <summary>Índice de Precios al Consumidor (INDEC). Acumula la variación del período.</summary>
    IPC = 1,

    /// <summary>Sin fórmula: el importe lo fija el operador y exige nota explicativa.</summary>
    Manual = 2,

    /// <summary>
    /// Porcentaje fijo pactado, aplicado en cada período (por ejemplo 8% trimestral).
    /// Tras el DNU 70/2023 es de los esquemas más usados en contratos nuevos, y no depende de
    /// ningún índice externo: se calcula con el porcentaje guardado en el contrato.
    /// </summary>
    FixedPercent = 3,
}
