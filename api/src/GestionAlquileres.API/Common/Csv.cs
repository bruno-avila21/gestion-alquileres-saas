using System.Globalization;

namespace GestionAlquileres.API.Common;

/// <summary>Helpers for building safe CSV exports.</summary>
public static class Csv
{
    private static readonly char[] FormulaTriggers = { '=', '+', '-', '@', '\t', '\r' };

    /// <summary>
    /// Quotes a free-text field and neutralizes CSV/formula injection: a leading formula character
    /// (=, +, -, @, tab, CR) is prefixed with a single quote so spreadsheets treat it as text.
    /// </summary>
    public static string Field(string? value)
    {
        var v = value ?? string.Empty;
        if (v.Length > 0 && Array.IndexOf(FormulaTriggers, v[0]) >= 0)
            v = "'" + v;
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>Formats a decimal with an invariant decimal point (never a locale comma).</summary>
    public static string Number(decimal value, string format = "") =>
        string.IsNullOrEmpty(format)
            ? value.ToString(CultureInfo.InvariantCulture)
            : value.ToString(format, CultureInfo.InvariantCulture);
}
