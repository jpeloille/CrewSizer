using System.Globalization;
using CrewSizer.Models;

namespace CrewSizer.Services;

public static class CalendrierHelper
{
    /// <summary>Convertit un nom de mois français en numéro 1-12 (0 si inconnu)</summary>
    public static int MoisVersNumero(string mois) => mois.ToLowerInvariant() switch
    {
        "janvier" => 1,
        "février" or "fevrier" => 2,
        "mars" => 3,
        "avril" => 4,
        "mai" => 5,
        "juin" => 6,
        "juillet" => 7,
        "août" or "aout" => 8,
        "septembre" => 9,
        "octobre" => 10,
        "novembre" => 11,
        "décembre" or "decembre" => 12,
        _ => 0
    };

    /// <summary>Retourne les semaines ISO dont le lundi tombe dans le mois donné</summary>
    public static List<(int semaine, int annee)> GetSemainesDuMois(string mois, int annee)
    {
        int moisNum = MoisVersNumero(mois);
        if (moisNum == 0) return [];

        var result = new List<(int, int)>();
        int nbJours = DateTime.DaysInMonth(annee, moisNum);

        for (int jour = 1; jour <= nbJours; jour++)
        {
            var date = new DateTime(annee, moisNum, jour);
            if (date.DayOfWeek == DayOfWeek.Monday)
            {
                int semIso = ISOWeek.GetWeekOfYear(date);
                int anneeIso = ISOWeek.GetYear(date);
                result.Add((semIso, anneeIso));
            }
        }

        return result;
    }

    /// <summary>Résout le programme pour la période : collecte les blocs des semaines assignées</summary>
    public static List<BlocVol> ResoudreProgramme(
        Configuration config,
        out int nbSemaines,
        out double semainesMois)
    {
        var semainesDuMois = GetSemainesDuMois(config.Periode.Mois, config.Periode.Annee);
        nbSemaines = semainesDuMois.Count;
        semainesMois = nbSemaines;

        var blocs = new List<BlocVol>();
        var semainesTypesDict = config.SemainesTypes.ToDictionary(st => st.Reference);

        foreach (var (semaine, annee) in semainesDuMois)
        {
            var affectation = config.Calendrier
                .FirstOrDefault(a => a.Semaine == semaine && a.Annee == annee);

            if (affectation == null) continue;

            if (semainesTypesDict.TryGetValue(affectation.SemaineTypeRef, out var st))
                blocs.AddRange(st.Blocs);
        }

        return blocs;
    }

    /// <summary>Retourne les blocs uniques (1 exemplaire par semaine type) pour vérifications TSV</summary>
    public static List<BlocVol> BlocsUniques(Configuration config)
    {
        return config.SemainesTypes.SelectMany(st => st.Blocs).ToList();
    }
}
