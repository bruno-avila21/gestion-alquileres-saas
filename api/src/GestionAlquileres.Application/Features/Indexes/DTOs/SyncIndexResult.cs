namespace GestionAlquileres.Application.Features.Indexes.DTOs;

public record SyncIndexResult(
    bool Success,
    bool WasFallback,
    bool AlreadyExisted,
    string? Message,
    IndexValueDto IndexValue);

public static class SyncIndexResults
{
    public static SyncIndexResult NewlySynced(IndexValueDto v) =>
        new(true, false, false, "Synced from external API.", v);

    public static SyncIndexResult Fallback(IndexValueDto v) =>
        new(true, true, false, "External API unavailable; using last known value.", v);

    public static SyncIndexResult AlreadyExisted(IndexValueDto v) =>
        new(true, false, true, "Period already synced; no external call made.", v);
}
