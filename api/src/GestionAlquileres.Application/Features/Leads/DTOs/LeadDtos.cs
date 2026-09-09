using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Leads.DTOs;

public record LeadDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Message,
    LeadSource Source,
    LeadStatus Status,
    string? LostReason,
    Guid? ListingId,
    Guid? PropertyId,
    string? PropertyTitle,
    string? PropertyAddress,
    OperationType? ListingOperation,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastContactAt,
    int NotesCount)
{
    public static LeadDto From(Lead l) => new(
        l.Id, l.Name, l.Email, l.Phone, l.Message, l.Source, l.Status, l.LostReason,
        l.ListingId, l.PropertyId,
        l.Listing?.Title, l.Property?.Address, l.Listing?.OperationType,
        l.CreatedAt, l.UpdatedAt, l.LastContactAt, l.Notes.Count);
}

public record LeadNoteDto(Guid Id, string Text, string CreatedByName, DateTimeOffset CreatedAt)
{
    public static LeadNoteDto From(LeadNote n) => new(n.Id, n.Text, n.CreatedByName, n.CreatedAt);
}

public record LeadDetailDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Message,
    LeadSource Source,
    LeadStatus Status,
    string? LostReason,
    Guid? ListingId,
    Guid? PropertyId,
    string? PropertyTitle,
    string? PropertyAddress,
    OperationType? ListingOperation,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastContactAt,
    int NotesCount,
    IReadOnlyList<LeadNoteDto> Notes)
{
    public static LeadDetailDto From(Lead l)
    {
        var notes = l.Notes.OrderByDescending(n => n.CreatedAt).Select(LeadNoteDto.From).ToList();
        return new(
            l.Id, l.Name, l.Email, l.Phone, l.Message, l.Source, l.Status, l.LostReason,
            l.ListingId, l.PropertyId,
            l.Listing?.Title, l.Property?.Address, l.Listing?.OperationType,
            l.CreatedAt, l.UpdatedAt, l.LastContactAt, notes.Count, notes);
    }
}

/// <summary>Conteos para los encabezados de columna del kanban. Incluye siempre las 6 claves del enum.</summary>
public record LeadSummaryDto(int Total, IReadOnlyDictionary<string, int> ByStatus)
{
    public static LeadSummaryDto From(int total, IReadOnlyDictionary<LeadStatus, int> byStatus)
    {
        var dict = Enum.GetValues<LeadStatus>()
            .ToDictionary(s => s.ToString(), s => byStatus.TryGetValue(s, out var c) ? c : 0);
        return new(total, dict);
    }
}
