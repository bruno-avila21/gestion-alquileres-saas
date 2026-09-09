using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Listings.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Listings.Commands;

public class CreateListingCommandHandler : IRequestHandler<CreateListingCommand, ListingDto>
{
    private readonly IListingRepository _listings;
    private readonly IPropertyRepository _properties;
    private readonly ICurrentTenant _currentTenant;

    public CreateListingCommandHandler(
        IListingRepository listings, IPropertyRepository properties, ICurrentTenant currentTenant)
    {
        _listings = listings;
        _properties = properties;
        _currentTenant = currentTenant;
    }

    public async Task<ListingDto> Handle(CreateListingCommand request, CancellationToken ct)
    {
        // Tenant-filtered lookup: a property of another organization reads as not found.
        var property = await _properties.GetByIdAsync(request.PropertyId, ct)
            ?? throw new BusinessException("La propiedad indicada no existe.");

        await EnsureSinglePublishedPerOperation(property.Id, request.OperationType, request.Status, excludeId: null, ct);

        var now = DateTimeOffset.UtcNow;
        var listing = new Listing
        {
            OrganizationId = _currentTenant.OrganizationId,
            PropertyId = property.Id,
            OperationType = request.OperationType,
            Price = request.Price,
            Currency = request.Currency,
            Expenses = request.Expenses,
            Title = request.Title.Trim(),
            IsFeatured = request.IsFeatured,
            Status = request.Status,
            PublishedAt = request.Status == ListingStatus.Published ? now : null,
            CreatedAt = now,
            UpdatedAt = now,
            Property = property,
        };

        await _listings.AddAsync(listing, ct);
        await _listings.SaveChangesAsync(ct);

        return ListingDto.From(listing);
    }

    /// <summary>
    /// The public site shows one price per operation: two "Published" listings of the same property
    /// for sale would be a data-entry error, not a feature.
    /// </summary>
    internal async Task EnsureSinglePublishedPerOperation(
        Guid propertyId, OperationType operation, ListingStatus status, Guid? excludeId, CancellationToken ct)
    {
        if (status != ListingStatus.Published) return;

        var existing = await _listings.GetAllAsync(propertyId, ct);
        if (existing.Any(l => l.Id != excludeId && l.OperationType == operation && l.Status == ListingStatus.Published))
            throw new BusinessException("La propiedad ya tiene una publicación activa para esa operación.");
    }
}
