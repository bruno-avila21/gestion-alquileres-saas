using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Transactions.Queries;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Tests.Phase13.Application;

/// <summary>
/// Bloque PDF recibos/liquidaciones, parte B. Prueba la regla de negocio y la numeración a nivel
/// handler, con fakes en memoria: la atomicidad real del contador (ExecuteUpdateAsync + transacción
/// de fila) vive en OrganizationRepository.IncrementReceiptSequenceAsync y corre contra Postgres —
/// el proveedor InMemory de la suite no traduce ExecuteUpdateAsync ni soporta transacciones reales
/// (ver decisión documentada en el informe). Lo que este test verifica es la regla que el handler
/// tiene que cumplir: "primera vez asigna, después reusa" y "dos transacciones distintas -> números
/// consecutivos" — exactamente lo que pide el criterio de numeración del contrato.
/// </summary>
[Trait("Phase", "Phase13")]
public class PaymentReceiptPdfHandlerTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private static (
        GetPaymentReceiptPdfQueryHandler Handler,
        FakeTransactionRepository Transactions,
        FakeOrganizationRepository Organizations,
        FakeContractRepository Contracts,
        FakePdfReportGenerator Pdf) Build()
    {
        var org = new Organization { Id = OrgId, Name = "Inmobiliaria Test", Slug = "test", ReceiptSequence = 0 };
        var orgRepo = new FakeOrganizationRepository { Org = org };
        var txRepo = new FakeTransactionRepository();
        var contractRepo = new FakeContractRepository();
        var pdf = new FakePdfReportGenerator();
        var storage = new FakeStorageService();
        var tenant = new FakeCurrentTenant { OrganizationId = OrgId };

        var handler = new GetPaymentReceiptPdfQueryHandler(txRepo, contractRepo, orgRepo, storage, pdf, tenant);
        return (handler, txRepo, orgRepo, contractRepo, pdf);
    }

    private static Contract MakeContract(Guid orgId)
    {
        var property = new Property
        {
            OrganizationId = orgId,
            Address = "Rivadavia 1000",
            City = "CABA",
            Province = "CABA",
            PropertyType = PropertyType.Apartment,
        };
        var appTenant = new AppTenant
        {
            OrganizationId = orgId,
            FirstName = "Ana",
            LastName = "López",
            Dni = "28888111",
        };
        return new Contract
        {
            OrganizationId = orgId,
            Property = property,
            PropertyId = property.Id,
            AppTenant = appTenant,
            AppTenantId = appTenant.Id,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2028, 1, 1),
            MonthlyRent = 250_000m,
        };
    }

    private static Transaction MakePayment(Guid orgId, Guid contractId, decimal amount = 100_000m) => new()
    {
        OrganizationId = orgId,
        ContractId = contractId,
        Type = TransactionType.Payment,
        Amount = amount,
        Currency = Currency.ARS,
        Period = new DateOnly(2026, 3, 1),
        Status = TransactionStatus.Paid,
    };

    [Fact]
    public async Task T1_Transaccion_inexistente_devuelve_null()
    {
        var (handler, _, _, _, _) = Build();
        var result = await handler.Handle(new GetPaymentReceiptPdfQuery(Guid.NewGuid()), default);
        Assert.Null(result);
    }

    [Fact]
    public async Task T2_Transaccion_que_no_es_pago_lanza_BusinessException()
    {
        var (handler, txRepo, _, contractRepo, _) = Build();
        var contract = MakeContract(OrgId);
        contractRepo.All.Add(contract);

        var charge = new Transaction
        {
            OrganizationId = OrgId,
            ContractId = contract.Id,
            Type = TransactionType.RentCharge,
            Amount = 100_000m,
            Period = new DateOnly(2026, 3, 1),
        };
        txRepo.All.Add(charge);

        var ex = await Assert.ThrowsAsync<BusinessException>(
            () => handler.Handle(new GetPaymentReceiptPdfQuery(charge.Id), default));
        Assert.Equal("Sólo se emite recibo de las transacciones de tipo pago.", ex.Message);
    }

    [Fact]
    public async Task T3_Pedir_dos_veces_el_recibo_de_la_misma_transaccion_devuelve_el_mismo_numero()
    {
        var (handler, txRepo, orgRepo, contractRepo, _) = Build();
        var contract = MakeContract(OrgId);
        contractRepo.All.Add(contract);
        var payment = MakePayment(OrgId, contract.Id);
        txRepo.All.Add(payment);

        var first = await handler.Handle(new GetPaymentReceiptPdfQuery(payment.Id), default);
        var second = await handler.Handle(new GetPaymentReceiptPdfQuery(payment.Id), default);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("REC-00000001", payment.ReceiptNumber);
        Assert.Equal(first!.FileName, second!.FileName);
        Assert.Equal(1, orgRepo.IncrementCalls); // el segundo pedido NO vuelve a incrementar
    }

    [Fact]
    public async Task T4_Dos_transacciones_distintas_devuelven_numeros_consecutivos()
    {
        var (handler, txRepo, _, contractRepo, _) = Build();
        var contract = MakeContract(OrgId);
        contractRepo.All.Add(contract);
        var paymentA = MakePayment(OrgId, contract.Id, 100_000m);
        var paymentB = MakePayment(OrgId, contract.Id, 50_000m);
        txRepo.All.Add(paymentA);
        txRepo.All.Add(paymentB);

        await handler.Handle(new GetPaymentReceiptPdfQuery(paymentA.Id), default);
        await handler.Handle(new GetPaymentReceiptPdfQuery(paymentB.Id), default);

        Assert.Equal("REC-00000001", paymentA.ReceiptNumber);
        Assert.Equal("REC-00000002", paymentB.ReceiptNumber);
    }

    [Fact]
    public async Task T5_El_PDF_generado_arranca_con_la_cabecera_PDF()
    {
        var (handler, txRepo, _, contractRepo, pdf) = Build();
        var contract = MakeContract(OrgId);
        contractRepo.All.Add(contract);
        var payment = MakePayment(OrgId, contract.Id);
        txRepo.All.Add(payment);

        var result = await handler.Handle(new GetPaymentReceiptPdfQuery(payment.Id), default);

        Assert.NotNull(result);
        Assert.Equal(0x25, result!.Content[0]); // '%'
        Assert.Single(pdf.Receipts);
        Assert.Equal("Ana López", pdf.Receipts[0].PayerName);
    }
}
