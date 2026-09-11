using FluentValidation;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Application.Features.Public.Commands;

/// <summary>
/// Consulta enviada desde el formulario público (ficha de una publicación o "Contacto" del home).
/// El honeypot ("website") se descarta antes de llegar acá: el controller responde 204 sin invocar
/// este command, así ni la validación ni el repositorio ven tráfico de bots.
/// </summary>
public record CreatePublicLeadCommand(
    string Slug, string Name, string? Email, string? Phone, string Message, Guid? ListingId)
    : IRequest<Guid?>;

public class CreatePublicLeadCommandValidator : AbstractValidator<CreatePublicLeadCommand>
{
    public CreatePublicLeadCommandValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Debe indicar email o teléfono.")
            .WithName("Email");
    }
}

public class CreatePublicLeadCommandHandler : IRequestHandler<CreatePublicLeadCommand, Guid?>
{
    private readonly IOrganizationRepository _orgs;
    private readonly IListingRepository _listings;
    private readonly ILeadRepository _leads;
    private readonly IUserRepository _users;
    private readonly IEmailService _emailService;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<CreatePublicLeadCommandHandler> _logger;

    public CreatePublicLeadCommandHandler(
        IOrganizationRepository orgs,
        IListingRepository listings,
        ILeadRepository leads,
        IUserRepository users,
        IEmailService emailService,
        ICurrentTenant currentTenant,
        ILogger<CreatePublicLeadCommandHandler> logger)
    {
        _orgs = orgs;
        _listings = listings;
        _leads = leads;
        _users = users;
        _emailService = emailService;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<Guid?> Handle(CreatePublicLeadCommand request, CancellationToken ct)
    {
        var org = await _orgs.GetBySlugAsync(request.Slug.ToLowerInvariant(), ct);
        if (org is null || !org.IsActive) return null;

        Guid? propertyId = null;
        string? propertyTitle = null;
        string? propertyAddress = null;
        if (request.ListingId is { } listingId)
        {
            // Tenant-scoped by the global filter (TenantMiddleware resolved the slug from the URL),
            // so this can only ever find a listing that belongs to `org`.
            var listing = await _listings.GetByIdAsync(listingId, ct);
            if (listing is null || listing.Status != ListingStatus.Published)
                throw new BusinessException("La publicación indicada no existe.");
            propertyId = listing.PropertyId;
            propertyTitle = listing.Title;
            propertyAddress = listing.Property?.Address;
        }

        var now = DateTimeOffset.UtcNow;
        var lead = new Lead
        {
            OrganizationId = _currentTenant.OrganizationId,
            ListingId = request.ListingId,
            PropertyId = propertyId,
            Name = request.Name.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Message = request.Message.Trim(),
            Source = LeadSource.Website,
            Status = LeadStatus.New,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _leads.AddAsync(lead, ct);
        await _leads.SaveChangesAsync(ct);

        try
        {
            var admins = await _users.GetActiveByRoleAsync(org.Id, UserRole.Admin, ct);
            foreach (var admin in admins)
            {
                await _emailService.SendNewLeadNotificationAsync(
                    admin.Email, org.Name, lead.Name, lead.Email, lead.Phone, lead.Message,
                    propertyTitle, propertyAddress, lead.Id, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el aviso de nueva consulta para el lead {LeadId} de la organización {OrganizationId}", lead.Id, org.Id);
        }

        return lead.Id;
    }
}
