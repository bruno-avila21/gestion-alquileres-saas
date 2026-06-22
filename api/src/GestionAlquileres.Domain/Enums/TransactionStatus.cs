namespace GestionAlquileres.Domain.Enums;

/// <summary>
/// Settlement state of a transaction. Applies to charges (RentCharge, ManualDebit); payments and
/// credits are created already <see cref="Paid"/>. <see cref="Overdue"/> is a derived view state
/// (a Pending charge past its DueDate) — it is computed on read, not persisted.
/// </summary>
public enum TransactionStatus { Pending, Paid, Overdue, Cancelled }
