namespace CrewSizer.Domain.ValueObjects;

public class Periode
{
    public DateOnly DateDebut { get; set; }
    public DateOnly DateFin { get; set; }

    // Propriétés calculées (non persistées en base)
    public int NbJours => DateFin.DayNumber - DateDebut.DayNumber + 1;

    public string LibellePeriode => FormatLibelle();

    private string FormatLibelle()
    {
        if (DateDebut == default && DateFin == default)
            return "";
        if (DateDebut.Year == DateFin.Year && DateDebut.Month == DateFin.Month)
            return $"{NomMois(DateDebut.Month)} {DateDebut.Year}";
        if (DateDebut.Year == DateFin.Year)
            return $"{NomMois(DateDebut.Month)} — {NomMois(DateFin.Month)} {DateFin.Year}";
        return $"{NomMois(DateDebut.Month)} {DateDebut.Year} — {NomMois(DateFin.Month)} {DateFin.Year}";
    }

    private static string NomMois(int m) => m switch
    {
        1 => "Janvier", 2 => "Février", 3 => "Mars", 4 => "Avril",
        5 => "Mai", 6 => "Juin", 7 => "Juillet", 8 => "Août",
        9 => "Septembre", 10 => "Octobre", 11 => "Novembre", 12 => "Décembre",
        _ => ""
    };

    /// <summary>Retourne la liste des (mois, annee) couverts par la période</summary>
    public List<(int mois, int annee)> MoisCouverts()
    {
        if (DateDebut == default || DateFin == default)
            return [];

        var result = new List<(int, int)>();
        var current = new DateOnly(DateDebut.Year, DateDebut.Month, 1);
        var fin = new DateOnly(DateFin.Year, DateFin.Month, 1);
        while (current <= fin)
        {
            result.Add((current.Month, current.Year));
            current = current.AddMonths(1);
        }
        return result;
    }
}
