using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Leads.Commands;

public record AddLeadNoteCommand(Guid LeadId, string Text, Guid CreatedByUserId) : IRequest<LeadNoteDto>;

public class AddLeadNoteCommandHandler : IRequestHandler<AddLeadNoteCommand, LeadNoteDto>
{
    private readonly ILeadRepository _leads;
    private readonly IUserRepository _users;
    private readonly ICurrentTenant _currentTenant;

    public AddLeadNoteCommandHandler(ILeadRepository leads, IUserRepository users, ICurrentTenant currentTenant)
    {
        _leads = leads;
        _users = users;
        _currentTenant = currentTenant;
    }

    public async Task<LeadNoteDto> Handle(AddLeadNoteCommand request, CancellationToken ct)
    {
        var lead = await _leads.GetForEditAsync(request.LeadId, ct)
            ?? throw new BusinessException($"Lead {request.LeadId} not found.");

        var user = await _users.GetByIdAsync(request.CreatedByUserId, ct);
        var createdByName = user is null ? "Usuario" : $"{user.FirstName} {user.LastName}".Trim();

        var now = DateTimeOffset.UtcNow;
        var note = new LeadNote
        {
            OrganizationId = _currentTenant.OrganizationId,
            LeadId = lead.Id,
            Text = request.Text.Trim(),
            CreatedByUserId = request.CreatedByUserId,
            CreatedByName = createdByName,
            CreatedAt = now,
        };

        await _leads.AddNoteAsync(note, ct);

        lead.LastContactAt = now;
        lead.UpdatedAt = now;

        await _leads.SaveChangesAsync(ct);
        return LeadNoteDto.From(note);
    }
}
