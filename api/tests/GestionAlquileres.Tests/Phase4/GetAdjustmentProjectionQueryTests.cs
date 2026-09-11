using GestionAlquileres.Application.Features.RentHistory.Queries;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using Xunit;

namespace GestionAlquileres.Tests.Phase4;

public class GetAdjustmentProjectionQueryTests
{
    private sealed class StubContracts : IContractRepository
    {
        public Contract? Contract;
        public Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Contract);
        public Task<IReadOnlyList<Contract>> ListAsync(Guid? a, Guid? p, ContractStatus? s, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> HasActiveOverlapAsync(Guid propertyId, DateOnly s, DateOnly e, Guid? exclude, CancellationToken ct) => throw new NotImplementedException();
        public Task AddAsync(Contract c, CancellationToken ct) => throw new NotImplementedException();
        public Task SaveChangesAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<Contract?> GetByIdRawAsync(Guid id, Guid orgId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Contract>> ListActiveRawAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Contract>> GetExpiringRawAsync(int d, CancellationToken ct) => throw new NotImplementedException();
        public Task<(int ActiveCount, decimal MonthlyRevenue, int ExpiringCount)> GetDashboardStatsAsync(
            DateOnly today, DateOnly until, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class StubHistory : IRentHistoryRepository
    {
        public IReadOnlyList<Domain.Entities.RentHistory> Items = Array.Empty<Domain.Entities.RentHistory>();
        public Task<IReadOnlyList<Domain.Entities.RentHistory>> GetByContractAsync(Guid id, CancellationToken ct) => Task.FromResult(Items);
        public Task<Domain.Entities.RentHistory?> GetLastByContractAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Domain.Entities.RentHistory>> GetAllAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<(IReadOnlyList<Domain.Entities.RentHistory> Items, int Total)> GetPagedAsync(
            Domain.Enums.AdjustmentType? type, string? search, int page, int pageSize, CancellationToken ct) => throw new NotImplementedException();
        public Task AddAsync(Domain.Entities.RentHistory r, CancellationToken ct) => throw new NotImplementedException();
        public Task SaveChangesAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> ExistsForPeriodAsync(Guid id, DateOnly d, CancellationToken ct) => throw new NotImplementedException();
        public Task<Domain.Entities.RentHistory?> GetLastByContractRawAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
        public Task<int> CountByContractRawAsync(Guid id, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class StubCalculator : IIndicesCalculator
    {
        public string? IndexUsed;
        public decimal InitialUsed;
        public int FreqUsed;
        public AdjustmentProjection? Result;
        public bool Called;
        public Task<AdjustmentProjection?> CalculateAsync(string index, decimal initialRent, DateOnly start, int freq, DateOnly? until, CancellationToken ct = default)
        {
            Called = true; IndexUsed = index; InitialUsed = initialRent; FreqUsed = freq;
            return Task.FromResult(Result);
        }
    }

    [Fact]
    public async Task Maps_ICL_quarterly_and_uses_original_rent_from_history()
    {
        var contract = new Contract
        {
            AdjustmentType = AdjustmentType.ICL,
            AdjustmentFrequency = AdjustmentFrequency.Quarterly,
            MonthlyRent = 200_000m, // current (already adjusted)
            StartDate = new DateOnly(2025, 1, 1),
        };
        var contracts = new StubContracts { Contract = contract };
        var history = new StubHistory
        {
            Items = new[] // ordered desc; oldest last → PreviousRent 100000 is the original
            {
                new Domain.Entities.RentHistory { PreviousRent = 150_000m, NewRent = 200_000m, EffectiveDate = new DateOnly(2025, 7, 1) },
                new Domain.Entities.RentHistory { PreviousRent = 100_000m, NewRent = 150_000m, EffectiveDate = new DateOnly(2025, 4, 1) },
            }
        };
        var calc = new StubCalculator { Result = new AdjustmentProjection(150_000m, Array.Empty<AdjustmentProjectionItem>(), null) };

        var result = await new GetAdjustmentProjectionQueryHandler(contracts, history, calc)
            .Handle(new GetAdjustmentProjectionQuery(Guid.NewGuid()), default);

        Assert.NotNull(result);
        Assert.Equal("ICL", calc.IndexUsed);
        Assert.Equal(3, calc.FreqUsed);
        Assert.Equal(100_000m, calc.InitialUsed); // original rent, not current
    }

    [Fact]
    public async Task Manual_contract_returns_null_without_calling_calculator()
    {
        var contracts = new StubContracts { Contract = new Contract { AdjustmentType = AdjustmentType.Manual } };
        var calc = new StubCalculator();

        var result = await new GetAdjustmentProjectionQueryHandler(contracts, new StubHistory(), calc)
            .Handle(new GetAdjustmentProjectionQuery(Guid.NewGuid()), default);

        Assert.Null(result);
        Assert.False(calc.Called);
    }
}
