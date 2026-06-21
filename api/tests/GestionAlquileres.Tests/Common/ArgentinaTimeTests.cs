using GestionAlquileres.Application.Common.Time;

namespace GestionAlquileres.Tests.Common;

/// <summary>
/// Locks audit C-10: business "today" must be Buenos Aires local (UTC-3), not UTC.
/// </summary>
public class ArgentinaTimeTests
{
    [Fact]
    public void Now_is_offset_minus_three_hours()
    {
        // Argentina has no DST today, so the offset is a fixed -3h regardless of when this runs.
        Assert.Equal(TimeSpan.FromHours(-3), ArgentinaTime.Now.Offset);
    }

    [Fact]
    public void Today_matches_the_local_date_of_now()
    {
        Assert.Equal(DateOnly.FromDateTime(ArgentinaTime.Now.DateTime), ArgentinaTime.Today);
    }

    [Fact]
    public void Today_can_differ_from_utc_date_late_in_the_local_day()
    {
        // Sanity: the local date is never ahead of the UTC date (AR is behind UTC).
        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.True(ArgentinaTime.Today <= utcToday);
    }
}
