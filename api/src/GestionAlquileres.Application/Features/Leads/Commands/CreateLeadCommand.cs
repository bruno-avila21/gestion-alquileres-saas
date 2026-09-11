using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Leads.Commands;

/// <summary>Carga manual desde el panel (Source = Manual). El público usa CreatePublicLeadCommand.</summary>
public record CreateLeadCommand(string Name, string? Email, string? Phone, string Message, Guid? ListingId)
    : IRequest<LeadDto>;

public class CreateLeadCommandHandler : IRequestHandler<CreateLeadCommand, LeadDto>
{
    private readonly ILeadRepository _leads;
    private readonly IListingRepository _listings;
    private readonly ICurrentTenant _currentTenant;

    public CreateLeadCommandHandler(ILeadRepository leads, IListingRepository listings, ICurrentTenant currentTenant)
    {
        _leads = leads;
        _listings = listings;
        _currentTenant = currentTenant;
    }

    public async Task<LeadDto> Handle(CreateLeadCommand request, CancellationToken ct)
    {
        Listing? listing = null;
        if (request.ListingId is { } listingId)
        {
            // Tenant-filtered lookup: a listing of another organization reads as not found.
            listing = await _listings.GetByIdAsync(listingId, ct)
                ?? throw new BusinessException("La publicación indicada no existe.");
        }

        var now = DateTimeOffset.UtcNow;
        var lead = new Lead
        {
            OrganizationId = _currentTenant.OrganizationId,
            ListingId = listing?.Id,
            PropertyId = listing?.PropertyId,
            Name = request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Message = request.Message.Trim(),
            Source = LeadSource.Manual,
            Status = LeadStatus.New,
            CreatedAt = now,
            UpdatedAt = now,
            Listing = listing,
        };

        await _leads.AddAsync(lead, ct);
        await _leads.SaveChangesAsync(ct);

        return LeadDto.From(lead);
    }
}
