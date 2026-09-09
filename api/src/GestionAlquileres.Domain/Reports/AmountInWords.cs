namespace GestionAlquileres.Domain.Reports;

/// <summary>
/// Convierte un importe a su representación en letras, en español rioplatense, para la línea
/// "SON: ..." de los recibos y liquidaciones. Función pura, sin dependencias de EF/HTTP/otras capas.
///
/// Reglas de apocope de "uno" (para que la cifra module correctamente al sustantivo implícito del
/// monto, "pesos"/"dólares"): se usa la forma corta "un"/"veintiún"/"...y un" salvo cuando el "uno"
/// va pegado directamente a "ciento(s)" sin decena de por medio (ahí se deja "ciento uno", no
/// "ciento un") — es la convención que pide el contrato de recibos, no necesariamente la única
/// forma correcta en español.
/// </summary>
public static class AmountInWords
{
    private const decimal MinAmount = 0m;
    private const decimal MaxAmount = 999_999_999.99m;

    private static readonly string[] Units =
    {
        "", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
    };

    private static readonly string[] Teens =
    {
        "diez", "once", "doce", "trece", "catorce", "quince",
        "dieciséis", "diecisiete", "dieciocho", "diecinueve",
    };

    private static readonly string[] Twenties =
    {
        "veinte", "veintiún", "veintidós", "veintitrés", "veinticuatro",
        "veinticinco", "veintiséis", "veintisiete", "veintiocho", "veintinueve",
    };

    private static readonly string[] Tens =
    {
        "", "", "", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa",
    };

    private static readonly string[] Hundreds =
    {
        "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos",
        "seiscientos", "setecientos", "ochocientos", "novecientos",
    };

    /// <summary>
    /// Importe en letras con los centavos como fracción ("con 00/100"). Rango soportado: 0 a
    /// 999.999.999,99. Fuera de rango lanza <see cref="ArgumentOutOfRangeException"/>.
    /// No incluye el nombre de la moneda: quien llama antepone "Pesos"/"Dólares estadounidenses".
    /// </summary>
    public static string Convert(decimal amount)
    {
        if (amount < MinAmount || amount > MaxAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount), amount, $"El monto debe estar entre {MinAmount} y {MaxAmount}.");
        }

        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        var integerPart = (long)Math.Truncate(rounded);
        var cents = (int)Math.Round((rounded - integerPart) * 100m, 0, MidpointRounding.AwayFromZero);

        // Borde de redondeo: 999999999.995 podría redondear los centavos a 100.
        if (cents >= 100)
        {
            integerPart += 1;
            cents = 0;
        }

        var words = IntegerToWords(integerPart);
        return $"{words} con {cents:00}/100";
    }

    private static string IntegerToWords(long value)
    {
        if (value == 0) return "cero";

        var millions = value / 1_000_000;
        var thousands = value / 1000 % 1000;
        var units = value % 1000;

        var parts = new List<string>();

        if (millions > 0)
        {
            var word = ChunkToWords((int)millions);
            parts.Add(millions == 1 ? $"{word} millón" : $"{word} millones");
        }

        if (thousands > 0)
        {
            // "mil" nunca lleva "un" adelante (a diferencia de "millón", que sí lo necesita).
            parts.Add(thousands == 1 ? "mil" : $"{ChunkToWords((int)thousands)} mil");
        }

        if (units > 0)
        {
            parts.Add(ChunkToWords((int)units));
        }

        return string.Join(" ", parts);
    }

    /// <summary>Convierte un valor de 1 a 999 a palabras.</summary>
    private static string ChunkToWords(int n)
    {
        if (n == 100) return "cien";

        var h = n / 100;
        var rem = n % 100;
        var t = rem / 10;
        var u = rem % 10;

        var parts = new List<string>();

        if (h == 1) parts.Add("ciento");
        else if (h > 0) parts.Add(Hundreds[h]);

        if (rem == 0)
        {
            // sólo la centena, ya agregada arriba (o nada si h==0, lo que no debería pasar acá).
        }
        else if (rem < 10)
        {
            // Unidad suelta. Apocopa "uno" -> "un" sólo cuando no está pegada a "ciento(s)"
            // (h == 0): es la excepción que pide el contrato para 101 -> "ciento uno".
            parts.Add(u == 1 ? (h == 0 ? "un" : "uno") : Units[u]);
        }
        else if (rem <= 19)
        {
            parts.Add(Teens[rem - 10]);
        }
        else if (rem <= 29)
        {
            parts.Add(Twenties[rem - 20]);
        }
        else
        {
            var tensWord = Tens[t];
            parts.Add(u switch
            {
                0 => tensWord,
                1 => $"{tensWord} y un",
                _ => $"{tensWord} y {Units[u]}",
            });
        }

        return string.Join(" ", parts);
    }
}
