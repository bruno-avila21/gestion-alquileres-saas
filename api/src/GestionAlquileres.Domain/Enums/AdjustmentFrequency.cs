namespace GestionAlquileres.Domain.Enums;

/// <summary>
/// Cada cuánto se actualiza el alquiler.
///
/// Se persiste como entero, así que los miembros nuevos van SIEMPRE al final: cambiar el orden
/// reinterpretaría los contratos ya cargados.
///
/// Cuatrimestral y semestral se agregaron para el mercado post-DNU 70/2023, donde el índice y la
/// periodicidad se pactan libremente: IPC cuatrimestral y Casa Propia semestral son de los casos
/// más frecuentes, y antes no se podían representar.
/// </summary>
public enum AdjustmentFrequency
{
    Monthly = 0,
    Quarterly = 1,
    Annual = 2,
    FourMonthly = 3,
    SemiAnnual = 4,
}
