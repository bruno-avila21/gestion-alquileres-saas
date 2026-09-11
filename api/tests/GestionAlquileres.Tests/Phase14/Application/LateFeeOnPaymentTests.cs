using GestionAlquileres.Application.Common.Billing;
using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.Transactions.Commands;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Tests.Phase13.Application;

namespace GestionAlquileres.Tests.Phase14.Application;

/// <summary>
/// Bloque punitorios, parte C: el enganche con el cobro. El punitorio tiene que quedar congelado en
/// el día del pago —no en el de la próxima corrida del job— y tiene que poder saldarse con ese
/// mismo pago en vez de quedar colgado como deuda nueva justo después de cobrar.
///
/// Los vencimientos se arman relativos a ArgentinaTime.Today porque los handlers devengan "hasta
/// hoy": fijar una fecha absoluta haría que el test cambiara de resultado con el calendario.
/// </summary>
[Trait("Phase", "Phase14")]
public class LateFeeOnPaymentTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    /// <summary>Contrato al 0,1% diario, sin tolerancia, con un cargo vencido hace 10 días.</summary>
    private static (FakeContractRepository Contracts, FakeTransactionRepository Txs, Contract Contract, Transaction Cargo)
        Escenario(decimal capital = 100_000m, decimal? tasa = 0.1m)
    {
        var contract = new Contract
        {
            OrganizationId = OrgId,
            StartDate = ArgentinaTime.Today.AddMonths(-6),
            EndDate = ArgentinaTime.Today.AddMonths(6),
            MonthlyRent = capital,
            LateFeeDailyRate = tasa,
            LateFeeGraceDays = 0,
        };
        var cargo = new Transaction
        {
            OrganizationId = OrgId,
            ContractId = contract.Id,
            Type = TransactionType.RentCharge,
            Amount = capital,
            Period = new DateOnly(ArgentinaTime.Today.Year, ArgentinaTime.Today.Month, 1),
            DueDate = ArgentinaTime.Today.AddDays(-10),
            Status = TransactionStatus.Pending,
        };

        var contracts = new FakeContractRepository();
        contracts.All.Add(contract);
        var txs = new FakeTransactionRepository();
        txs.All.Add(cargo);
        return (contracts, txs, contract, cargo);
    }

    private static RegisterPaymentCommandHandler Payments(FakeContractRepository c, FakeTransactionRepository t) =>
        new(c, t, new LateFeeAccrualService(t));

    private static Transaction? LateFee(FakeTransactionRepository t) =>
        t.All.FirstOrDefault(x => x.Type == TransactionType.LateFee);

    [Fact]
    public async Task Registrar_un_pago_devenga_el_punitorio_hasta_hoy()
    {
        var (contracts, txs, contract, cargo) = Escenario();

        await Payments(contracts, txs).Handle(
            new RegisterPaymentCommand(contract.Id, 100_000m, cargo.Period, null), default);

        // 100.000 × 0,1% × 10 días = 1.000
        Assert.Equal(1_000m, LateFee(txs)!.Amount);
        Assert.Equal(ArgentinaTime.Today, LateFee(txs)!.AccruedThroughDate);
    }

    [Fact]
    public async Task Un_pago_que_alcanza_para_todo_salda_tambien_el_punitorio()
    {
        var (contracts, txs, contract, cargo) = Escenario();

        // Capital (100.000) + punitorio de 10 días (1.000).
        await Payments(contracts, txs).Handle(
            new RegisterPaymentCommand(contract.Id, 101_000m, cargo.Period, null), default);

        Assert.Equal(TransactionStatus.Paid, cargo.Status);
        Assert.Equal(TransactionStatus.Paid, LateFee(txs)!.Status);
    }

    [Fact]
    public async Task Un_pago_que_solo_cubre_el_capital_deja_el_punitorio_pendiente()
    {
        var (contracts, txs, contract, cargo) = Escenario();

        await Payments(contracts, txs).Handle(
            new RegisterPaymentCommand(contract.Id, 100_000m, cargo.Period, null), default);

        Assert.Equal(TransactionStatus.Paid, cargo.Status);
        Assert.Equal(TransactionStatus.Pending, LateFee(txs)!.Status);
    }

    [Fact]
    public async Task Un_contrato_sin_punitorio_pactado_no_genera_ninguno()
    {
        var (contracts, txs, contract, cargo) = Escenario(tasa: null);

        await Payments(contracts, txs).Handle(
            new RegisterPaymentCommand(contract.Id, 100_000m, cargo.Period, null), default);

        Assert.Null(LateFee(txs));
    }

    [Fact]
    public async Task Saldar_el_cargo_a_mano_congela_el_punitorio_y_lo_deja_exigible()
    {
        var (contracts, txs, contract, cargo) = Escenario();
        var handler = new MarkTransactionPaidCommandHandler(txs, contracts, new LateFeeAccrualService(txs));

        await handler.Handle(new MarkTransactionPaidCommand(contract.Id, cargo.Id), default);

        Assert.Equal(TransactionStatus.Paid, cargo.Status);
        // Saldar el cargo no salda su punitorio: es deuda aparte y sigue debiéndose.
        var punitorio = LateFee(txs)!;
        Assert.Equal(1_000m, punitorio.Amount);
        Assert.Equal(TransactionStatus.Pending, punitorio.Status);
    }
}
