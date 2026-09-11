using GestionAlquileres.Domain.Billing;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Formatting;
using GestionAlquileres.Domain.Interfaces.Repositories;

namespace GestionAlquileres.Application.Common.Billing;

public class LateFeeAccrualService : ILateFeeAccrualService
{
    private readonly ITransactionRepository _txRepo;

    public LateFeeAccrualService(ITransactionRepository txRepo) => _txRepo = txRepo;

    public async Task<Transaction?> AccrueAsync(
        Transaction charge, decimal dailyRate, int graceDays, DateOnly asOf, CancellationToken ct)
    {
        // Un punitorio no devenga punitorio (anatocismo), y un cargo ya saldado o anulado dejó de
        // devengar el día en que se saldó — no vuelve a crecer aunque el job pase de nuevo.
        if (charge.Type == TransactionType.LateFee) return null;
        if (charge.Status != TransactionStatus.Pending) return null;
        if (charge.DueDate is not { } dueDate) return null;

        var days = LateFeeCalculator.DaysInArrears(dueDate, graceDays, asOf);
        if (days <= 0) return null;

        var amount = LateFeeCalculator.Accrue(charge.Amount, dailyRate, days);
        if (amount <= 0m) return null;

        var existing = await _txRepo.GetLateFeeForChargeRawAsync(charge.Id, charge.OrganizationId, ct);

        if (existing is null)
        {
            var lateFee = new Transaction
            {
                OrganizationId = charge.OrganizationId,
                ContractId = charge.ContractId,
                Type = TransactionType.LateFee,
                Amount = amount,
                Currency = charge.Currency,
                // Mismo período que el cargo: el punitorio pertenece al mes que quedó impago, no al
                // mes en que se lo devengó. Así la liquidación al propietario lo imputa donde debe.
                Period = charge.Period,
                Status = TransactionStatus.Pending,
                // Exigible desde ya, pero no vencido: un punitorio que naciera vencido se mostraría
                // en rojo desde el primer día y encima entraría en el aging como deuda antigua.
                DueDate = asOf,
                RelatedTransactionId = charge.Id,
                AccruedThroughDate = asOf,
                Notes = Describe(days, dailyRate),
            };
            await _txRepo.AddAsync(lateFee, ct);
            return lateFee;
        }

        // Ya devengado hasta esta fecha o más adelante: nada que hacer. Es lo que hace idempotente
        // a la corrida diaria y lo que evita que un reintento del job infle el importe.
        if (existing.AccruedThroughDate is { } through && through >= asOf) return existing;

        existing.Amount = amount;
        existing.AccruedThroughDate = asOf;
        existing.Notes = Describe(days, dailyRate);
        return existing;
    }

    // Cultura explícita, no la del proceso: en el contenedor .NET cae en la cultura invariante y
    // una tasa de 0,1 se escribiría "0.1" sólo en producción (ver ArgentineCulture).
    private static string Describe(int days, decimal dailyRate) =>
        $"Punitorio por mora: {days} día(s) al {ArgentineCulture.Percent(dailyRate)}% diario sobre el capital impago.";
}
