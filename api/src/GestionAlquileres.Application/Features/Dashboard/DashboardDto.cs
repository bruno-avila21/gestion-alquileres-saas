using GestionAlquileres.Application.Features.Transactions.DTOs;

namespace GestionAlquileres.Application.Features.Dashboard;

public record DashboardDto(
    int ActiveContractsCount,
    decimal MonthlyRevenue,
    int ExpiringIn30DaysCount,
    IReadOnlyList<TransactionDto> RecentTransactions
);
