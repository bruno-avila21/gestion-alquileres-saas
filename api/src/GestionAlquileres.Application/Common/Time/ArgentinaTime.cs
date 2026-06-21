namespace GestionAlquileres.Application.Common.Time;

/// <summary>
/// Business clock for the Argentine market. Rent-adjustment scheduling, index periods and contract
/// expiry windows must be evaluated in Buenos Aires local time, not UTC: AR is UTC-3, so between
/// 21:00 and 24:00 local the UTC date is already "tomorrow", which would fire or skip adjustments a
/// day early/late and select the wrong index period (audit C-10).
/// </summary>
public static class ArgentinaTime
{
    // .NET 6+ resolves IANA ids on Windows too (via ICU). Buenos Aires is a fixed UTC-3 (no DST today),
    // but we go through the tz database so a future DST change is handled automatically.
    private static readonly TimeZoneInfo Tz = ResolveTimeZone();

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Tz);

    public static DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        // Last resort on a host without tz data: fixed UTC-3 (Argentina has no DST).
        return TimeZoneInfo.CreateCustomTimeZone("AR-UTC-3", TimeSpan.FromHours(-3), "Argentina (UTC-3)", "Argentina (UTC-3)");
    }
}
