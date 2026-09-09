using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.API.Contracts;

public record CreateLeadRequest(string Name, string? Email, string? Phone, string Message, Guid? ListingId);

public record UpdateLeadRequest(string Name, string? Email, string? Phone, string Message);

public record ChangeLeadStatusRequest(LeadStatus Status, string? LostReason);

public record AddLeadNoteRequest(string Text);
