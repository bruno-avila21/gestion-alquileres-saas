namespace GestionAlquileres.Domain.Enums;

/// <summary>
/// Ojo con el orden: los valores se persisten como enteros. Un tipo nuevo se agrega SIEMPRE al
/// final — insertarlo en el medio reinterpretaría toda la tabla `transactions` ya escrita.
/// </summary>
public enum TransactionType { RentCharge, Payment, ManualDebit, ManualCredit, LateFee }
