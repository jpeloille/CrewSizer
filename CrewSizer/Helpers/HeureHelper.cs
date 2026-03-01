using System;
using System.Globalization;

namespace CrewSizer.Helpers;

public static class HeureHelper
{
    public static int JourVersIndex(string jour) => jour.ToLowerInvariant() switch
    {
        "lundi" => 1, "mardi" => 2, "mercredi" => 3, "jeudi" => 4,
        "vendredi" => 5, "samedi" => 6, "dimanche" => 7, _ => 0
    };

    public static string IndexVersJour(int idx) => idx switch
    {
        1 => "Lundi", 2 => "Mardi", 3 => "Mercredi", 4 => "Jeudi",
        5 => "Vendredi", 6 => "Samedi", 7 => "Dimanche", _ => "?"
    };

    public static TimeSpan ParseHeure(string hhmm)
    {
        if (string.IsNullOrWhiteSpace(hhmm)) return TimeSpan.Zero;
        var parts = hhmm.Split(':');
        if (parts.Length != 2) return TimeSpan.Zero;
        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h)) return TimeSpan.Zero;
        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m)) return TimeSpan.Zero;
        return new TimeSpan(h, m, 0);
    }

    public static double CalculerDuree(string debut, string fin)
    {
        var d = ParseHeure(debut);
        var f = ParseHeure(fin);
        var duree = f - d;
        if (duree < TimeSpan.Zero) duree += TimeSpan.FromHours(24);
        return duree.TotalHours;
    }
}
