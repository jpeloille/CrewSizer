// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using CrewSizer.Application.Sizing;
using CrewSizer.Domain.Enums;

namespace CrewSizer.Infrastructure.Tests.Solver;

/// <summary>
/// Générateur de données de test réalistes basées sur le contexte Air Calédonie.
/// ATR 72-600, réseau domestique Nouvelle-Calédonie.
/// Étapes courtes 25-45 min, 2 blocs/jour typiques (matin + après-midi).
/// </summary>
internal static class TestDataBuilder
{
    // ─────────────────────────────────────────────
    // Navigants PNT — basé sur l'effectif réel
    // ─────────────────────────────────────────────

    /// <summary>
    /// Crée l'effectif PNT standard : 10 CDB + 9 OPL.
    /// Trigrammes réalistes issus de la liste Air Calédonie.
    /// </summary>
    public static List<CrewMemberInfo> CreatePntCrew(
        int numCdb = 10,
        int numOpl = 9,
        IReadOnlySet<DateOnly>? unavailableDates = null,
        bool firstCdbIsRdov = false)
    {
        var cdbTrigrams = new[] { "LRS", "SRV", "WAY", "GAL", "FRD", "BLT", "MAR", "DUP", "VER", "JOL" };
        var oplTrigrams = new[] { "ADE", "BRT", "CJO", "KLM", "PLL", "RTH", "STN", "TRV", "WGN" };
        var result = new List<CrewMemberInfo>();
        var dates = unavailableDates ?? new HashSet<DateOnly>();

        for (int i = 0; i < numCdb; i++)
        {
            result.Add(new CrewMemberInfo
            {
                Id = Guid.NewGuid(),
                Trigram = i < cdbTrigrams.Length ? cdbTrigrams[i] : $"C{i:D2}",
                Category = CrewCategory.PNT,
                Rank = CrewRank.CDB,
                IsExaminer = i < 2, // LRS et SRV sont TRE
                UnavailableDates = dates,
                OfficeDaysPerWeek = firstCdbIsRdov && i == 0 ? 3 : 0,
                WeekendOffFixed = firstCdbIsRdov && i == 0,
            });
        }

        for (int i = 0; i < numOpl; i++)
        {
            result.Add(new CrewMemberInfo
            {
                Id = Guid.NewGuid(),
                Trigram = i < oplTrigrams.Length ? oplTrigrams[i] : $"O{i:D2}",
                Category = CrewCategory.PNT,
                Rank = CrewRank.OPL,
                IsExaminer = false,
                UnavailableDates = dates,
            });
        }

        return result;
    }

    // ─────────────────────────────────────────────
    // Blocs de vols — programme type Air Calédonie
    // ─────────────────────────────────────────────

    /// <summary>
    /// Crée un bloc matin typique Air Calédonie.
    /// Exemple : BM1 — 4 secteurs NOU→LIF→NOU→TOU→NOU
    /// Report 05:30, fin 12:30 — 7h de DP, ~3h30 HDV, 4 secteurs.
    /// </summary>
    public static BlockInfo CreateMorningBlock(string code = "BM1", int sectorCount = 4)
    {
        return new BlockInfo
        {
            Id = Guid.NewGuid(),
            Code = code,
            SectorCount = sectorCount,
            BlockTimeMinutes = sectorCount * 35, // ~35 min/secteur moyen
            FdpStartMinutes = 5 * 60 + 30,       // 05:30
            FdpEndMinutes = 12 * 60 + 30,         // 12:30
            DpStartMinutes = 5 * 60,              // 05:00 (report 30 min avant)
            DpEndMinutes = 13 * 60,               // 13:00 (post-vol 30 min)
        };
    }

    /// <summary>
    /// Crée un bloc après-midi typique.
    /// Exemple : BA1 — 3 secteurs, 14:00→19:00.
    /// </summary>
    public static BlockInfo CreateAfternoonBlock(string code = "BA1", int sectorCount = 3)
    {
        return new BlockInfo
        {
            Id = Guid.NewGuid(),
            Code = code,
            SectorCount = sectorCount,
            BlockTimeMinutes = sectorCount * 35,
            FdpStartMinutes = 14 * 60,           // 14:00
            FdpEndMinutes = 19 * 60,             // 19:00
            DpStartMinutes = 13 * 60 + 30,       // 13:30
            DpEndMinutes = 19 * 60 + 30,         // 19:30
        };
    }

    /// <summary>
    /// Crée un bloc lourd (haute saison) : 6 secteurs, longue journée.
    /// </summary>
    public static BlockInfo CreateHeavyBlock(string code = "BH1", int sectorCount = 6)
    {
        return new BlockInfo
        {
            Id = Guid.NewGuid(),
            Code = code,
            SectorCount = sectorCount,
            BlockTimeMinutes = sectorCount * 35,
            FdpStartMinutes = 5 * 60 + 30,
            FdpEndMinutes = 14 * 60,             // FDP = 8h30
            DpStartMinutes = 5 * 60,
            DpEndMinutes = 14 * 60 + 30,         // DP = 9h30
        };
    }

    // ─────────────────────────────────────────────
    // Programmes journaliers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Crée un programme journalier basse saison : 2 blocs (matin + après-midi).
    /// Nécessite 2 CDB + 2 OPL par jour.
    /// </summary>
    public static DailyProgram CreateLowSeasonDay(DateOnly date)
    {
        return new DailyProgram
        {
            Date = date,
            Blocks = new[]
            {
                CreateMorningBlock("BM1"),
                CreateAfternoonBlock("BA1"),
            }
        };
    }

    /// <summary>
    /// Crée un programme haute saison : 3 blocs (2 matin + 1 après-midi).
    /// Nécessite 3 CDB + 3 OPL par jour.
    /// </summary>
    public static DailyProgram CreateHighSeasonDay(DateOnly date)
    {
        return new DailyProgram
        {
            Date = date,
            Blocks = new[]
            {
                CreateMorningBlock("BM1"),
                CreateMorningBlock("BM2", sectorCount: 3),
                CreateAfternoonBlock("BA1"),
            }
        };
    }

    /// <summary>Jour sans vol (dimanche typiquement).</summary>
    public static DailyProgram CreateNoFlightDay(DateOnly date)
    {
        return new DailyProgram
        {
            Date = date,
            Blocks = Array.Empty<BlockInfo>()
        };
    }

    // ─────────────────────────────────────────────
    // Programmes sur une période
    // ─────────────────────────────────────────────

    /// <summary>
    /// Programme basse saison sur N jours : Lun-Sam = 2 blocs, Dim = repos.
    /// </summary>
    public static List<DailyProgram> CreateLowSeasonProgram(DateOnly start, int days)
    {
        var program = new List<DailyProgram>();
        for (int i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            program.Add(date.DayOfWeek == DayOfWeek.Sunday
                ? CreateNoFlightDay(date)
                : CreateLowSeasonDay(date));
        }
        return program;
    }

    /// <summary>
    /// Programme haute saison sur N jours : Lun-Sam = 3 blocs, Dim = 1 bloc.
    /// </summary>
    public static List<DailyProgram> CreateHighSeasonProgram(DateOnly start, int days)
    {
        var program = new List<DailyProgram>();
        for (int i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            program.Add(date.DayOfWeek == DayOfWeek.Sunday
                ? CreateLowSeasonDay(date) // dimanche réduit mais pas OFF
                : CreateHighSeasonDay(date));
        }
        return program;
    }

    // ─────────────────────────────────────────────
    // Règles FTL EASA ORO.FTL
    // ─────────────────────────────────────────────

    /// <summary>Règles FTL standard EASA ORO.FTL + règles Air Calédonie.</summary>
    public static FtlRules DefaultFtlRules => new()
    {
        MaxDuty7dMinutes = 60 * 60,        // 60h
        MaxDuty14dMinutes = 110 * 60,      // 110h
        MaxDuty28dMinutes = 190 * 60,      // 190h
        MaxHdv28dMinutes = 100 * 60,       // 100h
        MinDaysOffPerMonth = 8,            // 8 jours OFF / 28 jours
        MinRestMinutes = 12 * 60,          // 12h repos minimum
        ExtendedRestMinutes = 36 * 60,     // 36h repos étendu
        ExtendedRestPeriodMinutes = 168 * 60, // toutes les 168h
        // Repos spécifiques Air Calédonie
        MaxConsecutiveWorkDays = 6,        // max 6 jours ON consécutifs
        MinRestDaysPerPeriod = 2,          // repos = 2 jours locaux minimum
        WeeklyRestMinutes = 36 * 60,       // 36h repos hebdomadaire
        WeeklyRestNights = 2,              // 2 nuitées locales par semaine civile
        // Repos mensuel week-end
        MonthlyWeekendRestRequired = true, // 1× par mois calendaire
        MonthlyWeekendRestDays = 3,        // 3 jours incluant samedi + dimanche
        // Table 2 EASA ORO.FTL.205 — FDP max
        FdpLimitTable = FdpLimitTableFactory.CreateEasaTable2(),
    };
}
