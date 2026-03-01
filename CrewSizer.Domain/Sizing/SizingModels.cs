// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Sizing;

// ──────────────────────────────────────────────────────────
// Entrées du solver
// ──────────────────────────────────────────────────────────

/// <summary>
/// Requête de dimensionnement : programme de vols résolu + équipage disponible + règles FTL.
/// </summary>
public sealed class SizingRequest
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

    /// <summary>
    /// Mode déterministe : force num_workers=1 et random_seed=0 pour garantir
    /// des résultats identiques entre exécutions. Plus lent mais reproductible.
    /// </summary>
    public bool Deterministic { get; init; }

    /// <summary>
    /// Identifiant de suivi de progression. Si renseigné, le solver envoie
    /// des mises à jour via SolveProgressTracker à chaque solution intermédiaire.
    /// </summary>
    public string? SolveId { get; init; }
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

    /// <summary>Navigant affecté au rôle RDOV (bureau 2,5j/sem, repos weekend).</summary>
    public bool IsRdov { get; init; }

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

    /// <summary>
    /// Contraintes actives ayant participé à la construction de ces règles.
    /// Renseigné uniquement si construit via <see cref="FromRegistry"/>.
    /// </summary>
    public IReadOnlyList<ConstraintDefinition>? ActiveConstraints { get; init; }

    /// <summary>Construit FtlRules depuis le registre de contraintes (fusion multi-sources).</summary>
    public static FtlRules FromRegistry(IEnumerable<ConstraintDefinition>? overrides = null)
        => ConstraintRegistry.MergeToFtlRules(overrides);
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

    /// <summary>Code de la contrainte mordante (ex : "C5").</summary>
    public string? BindingConstraintCode { get; init; }

    /// <summary>Source réglementaire de la contrainte mordante.</summary>
    public ConstraintSource? BindingConstraintSource { get; init; }
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

/// <summary>Résultat combiné PNT + PNC.</summary>
public sealed record CombinedSizingResult
{
    public required SizingResult PntResult { get; init; }
    public required SizingResult PncResult { get; init; }
    public bool IsBothFeasible => PntResult.IsFeasible && PncResult.IsFeasible;
    public long TotalSolveTimeMs => PntResult.SolveTimeMs + PncResult.SolveTimeMs;
}
