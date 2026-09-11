namespace GestionAlquileres.Application.Features.Transactions.DTOs;

/// <summary>
/// A page of transactions plus the net cash balance over the whole filtered set (audit M10). The
/// balance can't be derived from a single page, so it's computed server-side and returned alongside.
/// </summary>
public record TransactionsPageDto(
    IReadOnlyList<TransactionDto> Items, int Total, int Page, int PageSize, decimal NetBalance);
