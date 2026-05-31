using MediatR;

namespace GestionAlquileres.Application.Features.Dashboard;

public record GetDashboardQuery : IRequest<DashboardDto>;
