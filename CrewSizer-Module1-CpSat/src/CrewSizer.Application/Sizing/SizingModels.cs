// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using CrewSizer.Domain.Enums;

namespace CrewSizer.Application.Sizing;

// ──────────────────────────────────────────────────────────
// Entrées du solver
// ──────────────────────────────────────────────────────────

/// <summary>
/// Requête de dimensionnement : programme de vols résolu + équipage disponible + règles FTL.
/// </summary>
public sealed record SizingRequest
{
    /// <summary>Horizon de planification (ex: 1er mars → 31 mars 2026).</summary>
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>Programme résolu : pour chaque jour, la liste des blocs à couvrir.</summary>
    public required IReadOnlyList<DailyProgram> DailyPrograms { get; init; }

    /// <summary>Navigants disponibles (actifs, non en congé imposé sur la période).</summary>
    public required IReadOnlyList<CrewMemberInfo> AvailableCrew { get; init; }

    /// <summary>Règles FTL applicables.</summary>
    public required FtlRules FtlRules { get; init; }

    /// <summary>Catégorie à dimensionner (PNT ou PNC). Les calculs sont séparés.</summary>
    public required CrewCategory Category { get; init; }

    /// <summary>Timeout du solver en secondes (défaut: 30s).</summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>Nombre de workers parallèles (défaut: nombre de CPUs).</summary>
    public int NumWorkers { get; init; } = Environment.ProcessorCount;
}

/// <summary>Programme d'un jour : date + blocs à couvrir.</summary>
public sealed class DailyProgram
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<BlockInfo> Blocks { get; init; }
}

/// <summary>Informations d'un bloc de vols pour le solver.</summary>
public sealed class BlockInfo
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }

    /// <summary>Nombre de secteurs (étapes) dans le bloc.</summary>
    public required int SectorCount { get; init; }

    /// <summary>Heures de vol bloc (en minutes).</summary>
    public required int BlockTimeMinutes { get; init; }

    /// <summary>Début/fin du FDP (en minutes depuis 00:00 UTC).</summary>
    public required int FdpStartMinutes { get; init; }
    public required int FdpEndMinutes { get; init; }

    /// <summary>Début/fin du DP — duty period (en minutes depuis 00:00 UTC).</summary>
    public required int DpStartMinutes { get; init; }
    public required int DpEndMinutes { get; init; }

    /// <summary>Durée FDP en minutes (calculée).</summary>
    public int FdpMinutes => FdpEndMinutes - FdpStartMinutes;

    /// <summary>Durée DP (duty) en minutes (calculée).</summary>
    public int DutyMinutes => DpEndMinutes - DpStartMinutes;

    /// <summary>Nombre de CDB requis (typiquement 1).</summary>
    public int RequiredCdb { get; init; } = 1;

    /// <summary>Nombre d'OPL requis (typiquement 1).</summary>
    public int RequiredOpl { get; init; } = 1;

    /// <summary>Nombre de CC requis (pour PNC).</summary>
    public int RequiredCc { get; init; } = 1;

    /// <summary>Nombre de PNC requis (pour PNC).</summary>
    public int RequiredPnc { get; init; } = 0;
}

/// <summary>Informations navigant pour le solver (projection légère de CrewMember).</summary>
public sealed class CrewMemberInfo
{
    public required Guid Id { get; init; }
    public required string Trigram { get; init; }
    public required CrewCategory Category { get; init; }
    public required CrewRank Rank { get; init; }
    public required bool IsExaminer { get; init; }

    /// <summary>Jours d'indisponibilité sur la période (congés imposés, formation, etc.).</summary>
    public IReadOnlySet<DateOnly> UnavailableDates { get; init; } = new HashSet<DateOnly>();
}

/// <summary>Règles FTL pour le solver (projection de FtlRuleSet).</summary>
public sealed class FtlRules
{
    /// <summary>Temps de service max en minutes par fenêtre glissante de 7 jours.</summary>
    public int MaxDuty7dMinutes { get; init; } = 60 * 60;  // 60h

    /// <summary>Temps de service max en minutes par fenêtre glissante de 14 jours.</summary>
    public int MaxDuty14dMinutes { get; init; } = 110 * 60; // 110h

    /// <summary>Temps de service max en minutes par fenêtre glissante de 28 jours.</summary>
    public int MaxDuty28dMinutes { get; init; } = 190 * 60; // 190h

    /// <summary>HDV max en minutes sur 28 jours.</summary>
    public int MaxHdv28dMinutes { get; init; } = 100 * 60;  // 100h

    /// <summary>HDV max en minutes sur 90 jours glissants (ORO.FTL.210).</summary>
    public int MaxHdv90dMinutes { get; init; } = 280 * 60;  // 280h

    /// <summary>Jours OFF minimum par mois (28 jours).</summary>
    public int MinDaysOffPerMonth { get; init; } = 8;

    /// <summary>Repos minimum en minutes entre deux services.</summary>
    public int MinRestMinutes { get; init; } = 12 * 60; // 12h

    /// <summary>Repos étendu en minutes (toutes les 168h).</summary>
    public int ExtendedRestMinutes { get; init; } = 36 * 60; // 36h

    /// <summary>Période max entre deux repos étendus (en minutes).</summary>
    public int ExtendedRestPeriodMinutes { get; init; } = 168 * 60; // 168h = 7 jours

    // ── Repos spécifiques Air Calédonie / EASA ORO.FTL ──

    /// <summary>
    /// Nombre maximum de jours ON consécutifs avant un repos obligatoire.
    /// Défaut : 6 → le PN peut travailler 1, 2, 3, 4, 5 ou 6 jours d'affilée
    /// mais jamais plus de 6.
    /// </summary>
    public int MaxConsecutiveWorkDays { get; init; } = 6;

    /// <summary>
    /// Durée minimale du repos périodique (en jours consécutifs OFF).
    /// Défaut : 2 jours locaux.
    /// Un jour OFF isolé ne compte pas comme repos valide
    /// et ne remet pas le compteur de jours ON à zéro.
    /// Concrètement : le pattern ON-OFF-ON est interdit.
    /// </summary>
    public int MinRestDaysPerPeriod { get; init; } = 2;

    /// <summary>
    /// Repos hebdomadaire (36h + 2 nuitées locales) par semaine civile (lundi→dimanche).
    /// Au moins 2 jours consécutifs OFF dans chaque semaine civile.
    /// </summary>
    public int WeeklyRestMinutes { get; init; } = 36 * 60; // 36h

    /// <summary>
    /// Nombre de nuitées locales requises pour le repos hebdomadaire.
    /// 2 nuitées = repos couvrant 2 nuits complètes consécutives.
    /// En modélisation journalière : 2 jours consécutifs OFF dans la semaine civile.
    /// </summary>
    public int WeeklyRestNights { get; init; } = 2;

    // ── Repos mensuel week-end ──

    /// <summary>
    /// Repos mensuel incluant un week-end complet (samedi + dimanche).
    /// Chaque PN doit avoir au moins 1 fois par mois calendaire un repos
    /// de 3 jours consécutifs incluant obligatoirement un samedi ET un dimanche.
    /// Les seules combinaisons possibles sont : Ven+Sam+Dim ou Sam+Dim+Lun.
    /// </summary>
    public bool MonthlyWeekendRestRequired { get; init; } = true;

    /// <summary>
    /// Nombre de jours du repos mensuel week-end (défaut : 3).
    /// </summary>
    public int MonthlyWeekendRestDays { get; init; } = 3;

    /// <summary>
    /// Table FDP max EASA ORO.FTL.205 : (heure_début_report, nb_secteurs) → FDP_max_minutes.
    /// Clé = (heure report en minutes depuis 00:00, nombre de secteurs).
    /// </summary>
    public IReadOnlyDictionary<(int ReportHourMinutes, int Sectors), int> FdpLimitTable { get; init; }
        = new Dictionary<(int, int), int>();
}

// ──────────────────────────────────────────────────────────
// Table 2 EASA ORO.FTL.205 — FDP maximale équipage acclimaté
// ──────────────────────────────────────────────────────────

/// <summary>
/// Factory pour la Table 2 EASA ORO.FTL.205(b) — FDP maximale (équipage acclimaté).
/// 12 tranches horaires × 10 colonnes (1-2 secteurs à 10 secteurs).
/// </summary>
public static class FdpLimitTableFactory
{
    /// <summary>
    /// Tranches horaires de la Table 2 : (debut_minutes, fin_minutes_exclusive).
    /// </summary>
    private static readonly (int Start, int End)[] TimeSlots =
    {
        (6 * 60,       13 * 60 + 30), // 06:00–13:29
        (13 * 60 + 30, 14 * 60),      // 13:30–13:59
        (14 * 60,      14 * 60 + 30), // 14:00–14:29
        (14 * 60 + 30, 15 * 60),      // 14:30–14:59
        (15 * 60,      15 * 60 + 30), // 15:00–15:29
        (15 * 60 + 30, 16 * 60),      // 15:30–15:59
        (16 * 60,      16 * 60 + 30), // 16:00–16:29
        (16 * 60 + 30, 17 * 60),      // 16:30–16:59
        (17 * 60,      5 * 60),       // 17:00–04:59 (wrap midnight)
        (5 * 60,       5 * 60 + 15),  // 05:00–05:14
        (5 * 60 + 15,  5 * 60 + 30),  // 05:15–05:29
        (5 * 60 + 30,  5 * 60 + 45),  // 05:30–05:44
        (5 * 60 + 45,  6 * 60),       // 05:45–05:59
    };

    /// <summary>
    /// Valeurs FDP max en minutes pour chaque tranche × 10 colonnes de secteurs.
    /// Index [tranche][secteurs-1..9] → colonnes = 1-2sec, 3, 4, 5, 6, 7, 8, 9, 10.
    /// Secteurs au-delà de 10 utilisent la valeur de 10 secteurs (plancher 09:00).
    /// </summary>
    private static readonly int[][] FdpMaxMinutes =
    {
        // 06:00–13:29 : 13:00, 12:30, 12:00, 11:30, 11:00, 10:30, 10:00, 09:30, 09:00
        new[] { 780, 750, 720, 690, 660, 630, 600, 570, 540 },
        // 13:30–13:59
        new[] { 765, 735, 705, 675, 645, 615, 585, 555, 540 },
        // 14:00–14:29
        new[] { 750, 720, 690, 660, 630, 600, 570, 540, 540 },
        // 14:30–14:59
        new[] { 735, 705, 675, 645, 615, 585, 555, 540, 540 },
        // 15:00–15:29
        new[] { 720, 690, 660, 630, 600, 570, 540, 540, 540 },
        // 15:30–15:59
        new[] { 705, 675, 645, 615, 585, 555, 540, 540, 540 },
        // 16:00–16:29
        new[] { 690, 660, 630, 600, 570, 540, 540, 540, 540 },
        // 16:30–16:59
        new[] { 675, 645, 615, 585, 555, 540, 540, 540, 540 },
        // 17:00–04:59
        new[] { 660, 630, 600, 570, 540, 540, 540, 540, 540 },
        // 05:00–05:14
        new[] { 720, 690, 660, 630, 600, 570, 540, 540, 540 },
        // 05:15–05:29
        new[] { 735, 705, 675, 645, 615, 585, 555, 540, 540 },
        // 05:30–05:44
        new[] { 750, 720, 690, 660, 630, 600, 570, 540, 540 },
        // 05:45–05:59
        new[] { 765, 735, 705, 675, 645, 615, 585, 555, 540 },
    };

    /// <summary>
    /// Crée la Table 2 EASA ORO.FTL.205 complète.
    /// </summary>
    public static Dictionary<(int ReportHourMinutes, int Sectors), int> CreateEasaTable2()
    {
        var table = new Dictionary<(int, int), int>();

        for (int slot = 0; slot < TimeSlots.Length; slot++)
        {
            int startMinutes = TimeSlots[slot].Start;
            for (int sectorCol = 0; sectorCol < 9; sectorCol++)
            {
                // sectorCol 0 = 1-2 secteurs, sectorCol 1 = 3 secteurs, ...
                int sectorCount = sectorCol == 0 ? 1 : sectorCol + 2;
                table[(startMinutes, sectorCount)] = FdpMaxMinutes[slot][sectorCol];

                // sectorCol 0 couvre 1 ET 2 secteurs
                if (sectorCol == 0)
                    table[(startMinutes, 2)] = FdpMaxMinutes[slot][sectorCol];
            }
        }

        return table;
    }

    /// <summary>
    /// Recherche le FDP max pour un début FDP donné et un nombre de secteurs.
    /// Trouve la tranche horaire correspondante dans la Table 2.
    /// </summary>
    /// <returns>FDP max en minutes, ou null si la table est vide.</returns>
    public static int? LookupMaxFdp(
        IReadOnlyDictionary<(int ReportHourMinutes, int Sectors), int> table,
        int fdpStartMinutes,
        int sectorCount)
    {
        if (table.Count == 0)
            return null;

        // Plafonner les secteurs à 10 (au-delà, plancher 09:00 = 540 min)
        int effectiveSectors = Math.Min(sectorCount, 10);

        // Trouver la tranche horaire
        for (int slot = 0; slot < TimeSlots.Length; slot++)
        {
            var (slotStart, slotEnd) = TimeSlots[slot];
            bool inSlot;

            if (slotStart < slotEnd)
            {
                // Tranche normale (ex: 06:00–13:29)
                inSlot = fdpStartMinutes >= slotStart && fdpStartMinutes < slotEnd;
            }
            else
            {
                // Tranche traversant minuit (17:00–04:59)
                inSlot = fdpStartMinutes >= slotStart || fdpStartMinutes < slotEnd;
            }

            if (inSlot)
            {
                int startKey = TimeSlots[slot].Start;
                // Chercher la clé exacte dans la table
                if (table.TryGetValue((startKey, effectiveSectors), out int maxFdp))
                    return maxFdp;

                // Secteurs > 10 → utiliser le plancher
                if (effectiveSectors > 10 && table.TryGetValue((startKey, 10), out int floorFdp))
                    return floorFdp;
            }
        }

        return null;
    }
}

// ──────────────────────────────────────────────────────────
// Sorties du solver
// ──────────────────────────────────────────────────────────

/// <summary>Résultat complet du dimensionnement.</summary>
public sealed record SizingResult
{
    /// <summary>Statut de la résolution.</summary>
    public required SolverStatus Status { get; init; }

    /// <summary>Message d'information (contrainte mordante, warnings...).</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Alertes FDP : blocs dont la durée FDP dépasse la Table 2 EASA.</summary>
    public IReadOnlyList<string> FdpWarnings { get; init; } = Array.Empty<string>();

    /// <summary>Temps de résolution en millisecondes.</summary>
    public long SolveTimeMs { get; init; }

    /// <summary>Le programme est-il couvrable avec l'effectif donné ?</summary>
    public bool IsFeasible => Status is SolverStatus.Optimal or SolverStatus.Feasible;

    /// <summary>Effectif minimum requis par rang (résultat principal).</summary>
    public IReadOnlyDictionary<CrewRank, int> MinimumCrewByRank { get; init; }
        = new Dictionary<CrewRank, int>();

    /// <summary>Marge par rang : effectif disponible - minimum requis.</summary>
    public IReadOnlyDictionary<CrewRank, int> MarginByRank { get; init; }
        = new Dictionary<CrewRank, int>();

    /// <summary>Jours critiques (marge = 0 ou 1).</summary>
    public IReadOnlyList<CriticalDay> CriticalDays { get; init; }
        = Array.Empty<CriticalDay>();

    /// <summary>Détail des affectations jour par jour (pour visualisation).</summary>
    public IReadOnlyList<DailyAssignment> Assignments { get; init; }
        = Array.Empty<DailyAssignment>();

    /// <summary>Contrainte FTL la plus serrée (la "mordante").</summary>
    public string? BindingConstraint { get; init; }
}

public enum SolverStatus
{
    Optimal,
    Feasible,
    Infeasible,
    Timeout,
    Error
}

/// <summary>Jour critique : date + marge restante + contrainte active.</summary>
public sealed class CriticalDay
{
    public required DateOnly Date { get; init; }
    public required CrewRank Rank { get; init; }
    public required int Available { get; init; }
    public required int Required { get; init; }
    public int Margin => Available - Required;
    public string? Reason { get; init; }
}

/// <summary>Affectations d'une journée.</summary>
public sealed class DailyAssignment
{
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<BlockAssignment> BlockAssignments { get; init; }
    public required IReadOnlyList<string> CrewOnDayOff { get; init; }
}

/// <summary>Affectation d'un bloc à un équipage.</summary>
public sealed class BlockAssignment
{
    public required string BlockCode { get; init; }
    public required IReadOnlyList<string> AssignedCrew { get; init; }
}
