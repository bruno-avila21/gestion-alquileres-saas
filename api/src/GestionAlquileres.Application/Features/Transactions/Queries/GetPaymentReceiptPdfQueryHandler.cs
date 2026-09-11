using System.Globalization;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Common.Reports;
using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Domain.Reports;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Queries;

public class GetPaymentReceiptPdfQueryHandler : IRequestHandler<GetPaymentReceiptPdfQuery, PdfFileDto?>
{
    private readonly ITransactionRepository _transactions;
    private readonly IContractRepository _contracts;
    private readonly IOrganizationRepository _organizations;
    private readonly IStorageService _storage;
    private readonly IPdfReportGenerator _pdf;
    private readonly ICurrentTenant _currentTenant;

    public GetPaymentReceiptPdfQueryHandler(
        ITransactionRepository transactions,
        IContractRepository contracts,
        IOrganizationRepository organizations,
        IStorageService storage,
        IPdfReportGenerator pdf,
        ICurrentTenant currentTenant)
    {
        _transactions = transactions;
        _contracts = contracts;
        _organizations = organizations;
        _storage = storage;
        _pdf = pdf;
        _currentTenant = currentTenant;
    }

    public async Task<PdfFileDto?> Handle(GetPaymentReceiptPdfQuery request, CancellationToken ct)
    {
        // El filtro global de tenant ya deja esto en null cuando la transacción es de otra
        // organización — el aislamiento multi-tenant sale gratis de GetByIdAsync, no hay que
        // comparar OrganizationId a mano.
        var transaction = await _transactions.GetByIdAsync(request.TransactionId, ct);
        if (transaction is null) return null;

        // Un recibo acredita dinero recibido; un cargo (RentCharge) todavía no se cobró.
        if (transaction.Type != TransactionType.Payment)
            throw new BusinessException("Sólo se emite recibo de las transacciones de tipo pago.");

        if (transaction.ReceiptNumber is null)
        {
            var sequence = await _organizations.IncrementReceiptSequenceAsync(_currentTenant.OrganizationId, ct);
            transaction.ReceiptNumber = $"REC-{sequence:00000000}";
            await _transactions.SaveChangesAsync(ct);
        }

        var contract = await _contracts.GetByIdAsync(transaction.ContractId, ct)
            ?? throw new InvalidOperationException($"Contract {transaction.ContractId} not found for transaction {transaction.Id}.");

        var org = await _organizations.GetByIdAsync(_currentTenant.OrganizationId, ct)
            ?? throw new InvalidOperationException("La organización del token no existe.");

        var agency = await AgencyBrandFactory.BuildAsync(org, _storage, ct);

        var issuedOn = ArgentinaTime.ToLocalDate(transaction.PaidAt ?? transaction.CreatedAt);
        var currencyName = transaction.Currency == Currency.ARS ? "Pesos" : "Dólares estadounidenses";
        var amountInWords = $"{currencyName} {AmountInWords.Convert(transaction.Amount)}";
        var propertyAddress = string.Join(", ", new[]
        {
            contract.Property.Address, contract.Property.Neighborhood, contract.Property.City,
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var report = new ReceiptReport(
            Number: transaction.ReceiptNumber!,
            IssuedOn: issuedOn,
            Agency: agency,
            PayerName: $"{contract.AppTenant.FirstName} {contract.AppTenant.LastName}".Trim(),
            PayerDocument: contract.AppTenant.Dni,
            PropertyAddress: propertyAddress,
            Concept: $"Alquiler período {transaction.Period.ToString("MM/yyyy", CultureInfo.InvariantCulture)}",
            Amount: transaction.Amount,
            CurrencyCode: transaction.Currency.ToString(),
            AmountInWords: amountInWords,
            Notes: transaction.Notes);

        var bytes = _pdf.RenderReceipt(report);
        return new PdfFileDto(bytes, $"recibo-{report.Number}.pdf");
    }
}
