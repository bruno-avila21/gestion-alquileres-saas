using GestionAlquileres.Application.Common.Billing;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Tests.Phase13.Application;

namespace GestionAlquileres.Tests.Phase14.Application;

/// <summary>
/// Bloque punitorios, parte B: el devengamiento. Acá se prueba lo que hace idempotente al job
/// diario y lo que congela el punitorio cuando el cargo se salda.
/// </summary>
[Trait("Phase", "Phase14")]
public class LateFeeAccrualServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid ContractId = Guid.NewGuid();
    private static readonly DateOnly Vencimiento = new(2026, 8, 1);

    private static (LateFeeAccrualService Service, FakeTransactionRepository Repo, Transaction Cargo) Build(
        decimal amount = 420_000m)
    {
        var cargo = new Transaction
        {
            OrganizationId = OrgId,
            ContractId = ContractId,
            Type = TransactionType.RentCharge,
            Amount = amount,
            Period = new DateOnly(2026, 8, 1),
            DueDate = Vencimiento,
            Status = TransactionStatus.Pending,
        };
        var repo = new FakeTransactionRepository();
        repo.All.Add(cargo);
        return (new LateFeeAccrualService(repo), repo, cargo);
    }

    private static Transaction? LateFeeOf(FakeTransactionRepository repo, Guid chargeId) =>
        repo.All.FirstOrDefault(t => t.Type == TransactionType.LateFee && t.RelatedTransactionId == chargeId);

    [Fact]
    public async Task Crea_el_punitorio_vinculado_al_cargo_que_lo_origina()
    {
        var (service, repo, cargo) = Build();

        var lateFee = await service.AccrueAsync(cargo, 0.1m, 0, new DateOnly(2026, 8, 26), default);

        Assert.NotNull(lateFee);
        Assert.Equal(TransactionType.LateFee, lateFee!.Type);
        Assert.Equal(cargo.Id, lateFee.RelatedTransactionId);
        Assert.Equal(10_500m, lateFee.Amount); // 420.000 × 0,1% × 25 días
        Assert.Equal(new DateOnly(2026, 8, 26), lateFee.AccruedThroughDate);
        // Mismo período que el cargo, para que la liquidación al propietario lo impute donde debe.
        Assert.Equal(cargo.Period, lateFee.Period);
        // Exigible pero no vencido: si naciera vencido saldría en rojo desde el primer día.
        Assert.Equal(new DateOnly(2026, 8, 26), lateFee.DueDate);
        Assert.Equal(TransactionStatus.Pending, lateFee.Status);
        Assert.Single(repo.All.Where(t => t.Type == TransactionType.LateFee));
    }

    [Fact]
    public async Task Dos_corridas_del_mismo_dia_no_duplican_ni_inflan_el_punitorio()
    {
        var (service, repo, cargo) = Build();
        var hoy = new DateOnly(2026, 8, 26);

        await service.AccrueAsync(cargo, 0.1m, 0, hoy, default);
        await service.AccrueAsync(cargo, 0.1m, 0, hoy, default);

        Assert.Single(repo.All.Where(t => t.Type == TransactionType.LateFee));
        Assert.Equal(10_500m, LateFeeOf(repo, cargo.Id)!.Amount);
    }

    [Fact]
    public async Task Al_dia_siguiente_actualiza_el_mismo_punitorio_sin_capitalizar()
    {
        var (service, repo, cargo) = Build(100_000m);

        await service.AccrueAsync(cargo, 0.1m, 0, new DateOnly(2026, 8, 11), default); // 10 días
        await service.AccrueAsync(cargo, 0.1m, 0, new DateOnly(2026, 8, 31), default); // 30 días

        var lateFee = LateFeeOf(repo, cargo.Id)!;
        Assert.Single(repo.All.Where(t => t.Type == TransactionType.LateFee));
        // 30 días sobre el capital = 3.000. Si capitalizara sobre los 100 ya devengados, daría más.
        Assert.Equal(3_000m, lateFee.Amount);
        Assert.Equal(new DateOnly(2026, 8, 31), lateFee.AccruedThroughDate);
    }

    [Fact]
    public async Task No_devenga_sobre_un_cargo_ya_saldado()
    {
        var (service, repo, cargo) = Build();
        cargo.Status = TransactionStatus.Paid;

        var lateFee = await service.AccrueAsync(cargo, 0.1m, 0, new DateOnly(2026, 8, 26), default);

        Assert.Null(lateFee);
        Assert.Empty(repo.All.Where(t => t.Type == TransactionType.LateFee));
    }

    [Fact]
    public async Task No_devenga_punitorio_sobre_punitorio()
    {
        var (service, repo, _) = Build();
        var punitorio = new Transaction
        {
            OrganizationId = OrgId,
            ContractId = ContractId,
            Type = TransactionType.LateFee,
            Amount = 10_500m,
            Period = new DateOnly(2026, 8, 1),
            DueDate = Vencimiento,
            Status = TransactionStatus.Pending,
        };
        repo.All.Add(punitorio);

        var resultado = await service.AccrueAsync(punitorio, 0.1m, 0, new DateOnly(2026, 9, 30), default);

        Assert.Null(resultado);
        Assert.Single(repo.All.Where(t => t.Type == TransactionType.LateFee));
    }

    [Fact]
    public async Task Respeta_los_dias_de_gracia_pactados()
    {
        var (service, repo, cargo) = Build();

        // Día 5 con 5 días de tolerancia: todavía sin recargo.
        Assert.Null(await service.AccrueAsync(cargo, 0.1m, 5, new DateOnly(2026, 8, 6), default));
        Assert.Empty(repo.All.Where(t => t.Type == TransactionType.LateFee));

        // Un día después, el primer día de punitorio.
        var lateFee = await service.AccrueAsync(cargo, 0.1m, 5, new DateOnly(2026, 8, 7), default);
        Assert.Equal(420m, lateFee!.Amount);
    }
}
