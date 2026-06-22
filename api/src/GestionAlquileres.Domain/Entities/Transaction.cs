using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

public class Transaction : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ContractId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.ARS;
    public DateOnly Period { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Settlement state. Charges start Pending; payments/credits start Paid.</summary>
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    /// <summary>When the charge is due. Null for payments/credits.</summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>When the transaction was settled. Null while Pending.</summary>
    public DateTimeOffset? PaidAt { get; set; }

    /// <summary>A charge owed by the tenant (vs a payment/credit in their favor).</summary>
    public bool IsCharge => Type is TransactionType.RentCharge or TransactionType.ManualDebit;

    /// <summary>Due date for a period: the contract's day-of-month, clamped to the month length.</summary>
    public static DateOnly DueDateFor(DateOnly period, int dayOfMonth)
    {
        var day = Math.Clamp(dayOfMonth, 1, DateTime.DaysInMonth(period.Year, period.Month));
        return new DateOnly(period.Year, period.Month, day);
    }
}
