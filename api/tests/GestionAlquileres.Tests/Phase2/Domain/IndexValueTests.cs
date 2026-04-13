using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Tests.Phase2.Domain;

public class IndexValueTests
{
    [Fact]
    [Trait("Phase", "Phase2")]
    public void IndexValue_Default_sets_Id_to_new_Guid()
    {
        var indexValue = new IndexValue();
        Assert.NotEqual(Guid.Empty, indexValue.Id);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public void IndexValue_Default_sets_FetchedAt_to_utc_now()
    {
        var before = DateTimeOffset.UtcNow;
        var indexValue = new IndexValue();
        var after = DateTimeOffset.UtcNow;

        Assert.True(indexValue.FetchedAt >= before && indexValue.FetchedAt <= after);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public void IndexValue_has_no_OrganizationId_property()
    {
        var property = typeof(IndexValue).GetProperty("OrganizationId");
        Assert.Null(property);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public void IndexValue_Period_is_DateOnly()
    {
        var property = typeof(IndexValue).GetProperty("Period");
        Assert.NotNull(property);
        Assert.Equal(typeof(DateOnly), property!.PropertyType);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public void IndexValue_VariationPct_is_nullable_decimal()
    {
        var property = typeof(IndexValue).GetProperty("VariationPct");
        Assert.NotNull(property);
        Assert.Equal(typeof(decimal?), property!.PropertyType);
    }
}
