using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Tests.Phase5;

/// <summary>Audit M-4: charges are Pending with a due date; payments/credits are Paid; mark-paid settles a charge.</summary>
[Trait("Phase", "Phase5")]
public class TransactionStatusTests : IClassFixture<Phase5ApiFactory>
{
    private readonly Phase5ApiFactory _factory;

    public TransactionStatusTests(Phase5ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    [Fact]
    public async Task Payment_is_created_paid()
    {
        var c = await _factory.AuthedClientAsync("tx-pay");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 200_000m, period = "2026-02-01", notes = (string?)null,
        });
        var dto = await r.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        Assert.Equal(TransactionStatus.Paid, dto!.Status);
        Assert.NotNull(dto.PaidAt);
        Assert.Null(dto.DueDate);
    }

    [Fact]
    public async Task Manual_debit_with_future_due_date_is_pending()
    {
        var c = await _factory.AuthedClientAsync("tx-debit");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        // Future period → due date hasn't passed → stays Pending (not derived Overdue).
        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualDebit", amount = 15_000m, period = "2027-12-01", notes = "Multa",
        });
        var dto = await r.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        Assert.Equal(TransactionStatus.Pending, dto!.Status);
        Assert.Equal(new DateOnly(2027, 12, 1), dto.DueDate); // contract DayOfMonth = 1
        Assert.Null(dto.PaidAt);
    }

    [Fact]
    public async Task Manual_debit_past_due_is_reported_overdue()
    {
        var c = await _factory.AuthedClientAsync("tx-overdue");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        // Past period → due date already passed → derived Overdue on read.
        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualDebit", amount = 15_000m, period = "2026-02-01", notes = "Multa vieja",
        });
        var dto = await r.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        Assert.Equal(TransactionStatus.Overdue, dto!.Status);
        Assert.Equal(new DateOnly(2026, 2, 1), dto.DueDate);
        Assert.Null(dto.PaidAt);
    }

    [Fact]
    public async Task Manual_credit_is_created_paid()
    {
        var c = await _factory.AuthedClientAsync("tx-credit");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualCredit", amount = 5_000m, period = "2026-02-01", notes = "Descuento",
        });
        var dto = await r.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        Assert.Equal(TransactionStatus.Paid, dto!.Status);
        Assert.NotNull(dto.PaidAt);
    }

    [Fact]
    public async Task Mark_paid_settles_a_pending_charge()
    {
        var c = await _factory.AuthedClientAsync("tx-markpaid");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var chargeR = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualDebit", amount = 10_000m, period = "2026-02-01", notes = "Cargo",
        });
        var charge = await chargeR.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        var r = await c.PostAsync($"/api/v1/contracts/{contractId}/transactions/{charge!.Id}/mark-paid", null);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var paid = await r.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        Assert.Equal(TransactionStatus.Paid, paid!.Status);
        Assert.NotNull(paid.PaidAt);
    }

    [Fact]
    public async Task Mark_paid_on_a_payment_returns_409()
    {
        var c = await _factory.AuthedClientAsync("tx-markpay-invalid");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var payR = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 200_000m, period = "2026-02-01", notes = (string?)null,
        });
        var pay = await payR.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        var r = await c.PostAsync($"/api/v1/contracts/{contractId}/transactions/{pay!.Id}/mark-paid", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Mark_paid_twice_returns_409()
    {
        var c = await _factory.AuthedClientAsync("tx-markpaid-twice");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var chargeR = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualDebit", amount = 10_000m, period = "2026-02-01", notes = "Cargo",
        });
        var charge = await chargeR.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);

        (await c.PostAsync($"/api/v1/contracts/{contractId}/transactions/{charge!.Id}/mark-paid", null))
            .EnsureSuccessStatusCode();

        var r2 = await c.PostAsync($"/api/v1/contracts/{contractId}/transactions/{charge.Id}/mark-paid", null);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }
}
