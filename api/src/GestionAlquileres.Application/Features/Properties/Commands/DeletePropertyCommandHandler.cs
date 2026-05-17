using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public class DeletePropertyCommandHandler : IRequestHandler<DeletePropertyCommand>
{
    private readonly IPropertyRepository _repo;

    public DeletePropertyCommandHandler(IPropertyRepository repo) => _repo = repo;

    public async Task Handle(DeletePropertyCommand request, CancellationToken ct)
    {
        var property = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Property {request.Id} not found.");

        property.IsActive = false;
        await _repo.SaveChangesAsync(ct);
    }
}
