using GestionAlquileres.Application.Features.Indexes.DTOs;
using GestionAlquileres.Domain.Enums;
using MediatR;

namespace GestionAlquileres.Application.Features.Indexes.Commands;

public record SyncIndexCommand(IndexType IndexType, DateOnly Period)
    : IRequest<SyncIndexResult>;
