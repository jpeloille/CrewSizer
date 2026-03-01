// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using System.Diagnostics;
using CrewSizer.Application.Sizing;
using CrewSizer.Domain.Enums;
using Google.OrTools.Sat;
using Microsoft.Extensions.Logging;

namespace CrewSizer.Infrastructure.Solver;

/// <summary>
/// Module 1 — Dimensionnement d'effectif via Google OR-Tools CP-SAT.
///
/// Vérifie jour par jour que l'effectif disponible couvre le programme de vols
/// en respectant les contraintes FTL EASA ORO.FTL (EU 965/2012 Annex III Subpart FTL).
///
/// Variables : assign[crew, dayIndex, blockIndex] ∈ {0, 1}
/// Objectif  : Minimiser le nombre total de navigants utilisés (= effectif minimum requis)
///
/// ══════════════════════════════════════════════════════════════
/// Contraintes FTL implémentées :
/// ══════════════════════════════════════════════════════════════
///   C1  — Indisponibilités (congés, formation)
///   C2  — Max 1 bloc / navigant / jour
///   C3  — Linkage isUsed ↔ works
///   C4  — Couverture par grade (CDB, OPL, CC)
///   C5  — Duty 7j glissants ≤ 60h         (ORO.FTL.210)
///   C6  — Duty 14j glissants ≤ 110h        (ORO.FTL.210)
///   C7  — Duty 28j glissants ≤ 190h        (ORO.FTL.210)
///   C8  — HDV 28j glissants ≤ 100h         (ORO.FTL.210)
///   C8b — HDV 90j glissants ≤ 280h         (ORO.FTL.210, si horizon ≥ 90j)
///   C9  — 8 jours OFF / 28j                (ORO.FTL.235, si horizon ≥ 28j)
///   C10 — Repos min = max(duty, 12h)       (ORO.FTL.235(a)(1))
///   C11a— Max 6 jours ON consécutifs        (ORO.FTL.235)
///   C11b— Pas de OFF isolé (ON-OFF-ON)      (ORO.FTL.235)
///   C12 — Repos hebdo 36h + 2 nuitées / semaine civile (ORO.FTL.235(d))
///   C13 — Repos mensuel week-end 3j (Sam+Dim)  (si horizon ≥ 28j)
///   C15 — Repos étendu récupération 36h / 168h glissant (ORO.FTL.235(d))
///   PRE — Validation FDP max Table 2        (ORO.FTL.205)
///
/// ══════════════════════════════════════════════════════════════
/// Provisions ORO.FTL hors périmètre (et justification) :
/// ══════════════════════════════════════════════════════════════
///   - Extensions FDP (+1h, split, in-flight rest) : ORO.FTL.205(d), 220
///     → Blocs fixes en dimensionnement, pas de concept d'extension planifiée.
///   - Night duty max 4 secteurs : CS FTL.1.205(a)(1)
///     → Air Calédonie : pas de vols de nuit (domestique NC).
///   - Repos augmenté 14h post-FDP augmenté : ORO.FTL.235(a)
///     → ATR 72-600 = 2 pilotes minimum, pas d'équipage augmenté.
///   - Acclimatisation (Table 1) : ORO.FTL.105
///     → Domestique NC, fuseau unique (UTC+11), toujours état B/D.
///   - Positionnement comme FDP : ORO.FTL.215
///     → Pas de positionnement inter-base pour Air Calédonie.
///   - HDV 900h/an civile, 1000h/12 mois glissants : ORO.FTL.210
///     → Nécessite compteurs entrants historiques. Non mordant sur horizon court.
///   - Commander's discretion +2h/+3h : ORO.FTL.205(f)
///     → Événement imprévisible, non modélisable en planification.
///   - Standby / Reserve : ORO.FTL.225/230
///     → Module 1 = dimensionnement pur, pas de gestion standby.
///   - Reduced rest 10h avec FRM : ORO.FTL.235(c)
///     → Simplifié : repos toujours au home base, pas de repos réduit.
///   - Disruptive schedules : CS FTL.1.235(b)
///     → Réseau domestique court-courrier, pas de schedules disruptifs.
/// </summary>
public sealed class OrToolsSizingSolver : ISizingSolver
{
    private readonly ILogger<OrToolsSizingSolver> _logger;

    public OrToolsSizingSolver(ILogger<OrToolsSizingSolver> logger)
    {
        _logger = logger;
    }

    public Task<SizingResult> SolveAsync(SizingRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var result = Solve(request);
            sw.Stop();
            return Task.FromResult(result with { SolveTimeMs = sw.ElapsedMilliseconds });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Erreur solver dimensionnement");
            return Task.FromResult(new SizingResult
            {
                Status = SolverStatus.Error,
                Message = ex.Message,
                SolveTimeMs = sw.ElapsedMilliseconds
            });
        }
    }

    private SizingResult Solve(SizingRequest request)
    {
        // ──────────────────────────────────────────────────
        // Pré-contrôle FDP (ORO.FTL.205, Table 2)
        // ──────────────────────────────────────────────────

        var fdpWarnings = ValidateBlocksFdp(request);

        var model = new CpModel();

        // ──────────────────────────────────────────────────
        // Indexation
        // ──────────────────────────────────────────────────

        var days = request.DailyPrograms;
        var crew = request.AvailableCrew;
        var rules = request.FtlRules;
        var numDays = days.Count;
        var numCrew = crew.Count;

        _logger.LogDebug(
            "Modèle CP-SAT : {NumCrew} navigants × {NumDays} jours, catégorie {Cat}",
            numCrew, numDays, request.Category);

        // Index → objet pour accès rapide
        var crewByIndex = crew.ToList();
        var dayByIndex = days.ToList();

        // Séparer par rang selon la catégorie
        var crewByRank = request.Category == CrewCategory.PNT
            ? new Dictionary<CrewRank, List<int>>
            {
                [CrewRank.CDB] = Enumerable.Range(0, numCrew)
                    .Where(i => crewByIndex[i].Rank == CrewRank.CDB).ToList(),
                [CrewRank.OPL] = Enumerable.Range(0, numCrew)
                    .Where(i => crewByIndex[i].Rank == CrewRank.OPL).ToList(),
            }
            : new Dictionary<CrewRank, List<int>>
            {
                [CrewRank.CC] = Enumerable.Range(0, numCrew)
                    .Where(i => crewByIndex[i].Rank == CrewRank.CC).ToList(),
                [CrewRank.PNC] = Enumerable.Range(0, numCrew)
                    .Where(i => crewByIndex[i].Rank == CrewRank.PNC).ToList(),
            };

        // ──────────────────────────────────────────────────
        // Variables de décision
        // ──────────────────────────────────────────────────

        // assign[c, d, b] = 1 si le navigant c est affecté au bloc b le jour d
        var assign = new BoolVar[numCrew][][];
        for (int c = 0; c < numCrew; c++)
        {
            assign[c] = new BoolVar[numDays][];
            for (int d = 0; d < numDays; d++)
            {
                var numBlocks = dayByIndex[d].Blocks.Count;
                assign[c][d] = new BoolVar[numBlocks];
                for (int b = 0; b < numBlocks; b++)
                {
                    assign[c][d][b] = model.NewBoolVar($"a_{crewByIndex[c].Trigram}_{d}_{b}");
                }
            }
        }

        // isUsed[c] = 1 si le navigant c est utilisé au moins une fois sur la période
        var isUsed = new BoolVar[numCrew];
        for (int c = 0; c < numCrew; c++)
        {
            isUsed[c] = model.NewBoolVar($"used_{crewByIndex[c].Trigram}");
        }

        // works[c, d] = 1 si le navigant c travaille le jour d (affecté à au moins un bloc)
        var works = new BoolVar[numCrew][];
        for (int c = 0; c < numCrew; c++)
        {
            works[c] = new BoolVar[numDays];
            for (int d = 0; d < numDays; d++)
            {
                works[c][d] = model.NewBoolVar($"w_{crewByIndex[c].Trigram}_{d}");
            }
        }

        // ──────────────────────────────────────────────────
        // Contraintes
        // ──────────────────────────────────────────────────

        // --- C1 : Indisponibilités (congés, formation) ---
        for (int c = 0; c < numCrew; c++)
        {
            for (int d = 0; d < numDays; d++)
            {
                var date = dayByIndex[d].Date;
                if (crewByIndex[c].UnavailableDates.Contains(date))
                {
                    // Navigant indisponible ce jour → aucune affectation
                    for (int b = 0; b < dayByIndex[d].Blocks.Count; b++)
                    {
                        model.Add(assign[c][d][b] == 0);
                    }
                }
            }
        }

        // --- C2 : Max 1 bloc par navigant par jour ---
        for (int c = 0; c < numCrew; c++)
        {
            for (int d = 0; d < numDays; d++)
            {
                var numBlocks = dayByIndex[d].Blocks.Count;
                if (numBlocks == 0)
                {
                    model.Add(works[c][d] == 0);
                    continue;
                }

                // works[c,d] = 1 ssi au moins un assign[c,d,b] = 1
                var blockVars = new List<BoolVar>();
                for (int b = 0; b < numBlocks; b++)
                    blockVars.Add(assign[c][d][b]);

                // sum(assign[c,d,*]) <= 1 (un seul bloc par jour)
                model.Add(LinearExpr.Sum(blockVars) <= 1);

                // Lier works au fait d'être affecté
                model.Add(LinearExpr.Sum(blockVars) >= 1).OnlyEnforceIf(works[c][d]);
                model.Add(LinearExpr.Sum(blockVars) == 0).OnlyEnforceIf(works[c][d].Not());
            }
        }

        // --- C3 : Lien isUsed ↔ works ---
        for (int c = 0; c < numCrew; c++)
        {
            // isUsed[c] = 1 ssi works[c,d] = 1 pour au moins un jour
            model.AddMaxEquality(isUsed[c], works[c]);
        }

        // --- C4 : Couverture des blocs par rang ---
        for (int d = 0; d < numDays; d++)
        {
            var blocks = dayByIndex[d].Blocks;
            for (int b = 0; b < blocks.Count; b++)
            {
                var block = blocks[b];

                if (request.Category == CrewCategory.PNT)
                {
                    // Exactement RequiredCdb CDB affectés à ce bloc
                    var cdbIndices = crewByRank.GetValueOrDefault(CrewRank.CDB, new List<int>());
                    if (cdbIndices.Count > 0)
                    {
                        var cdbVars = cdbIndices.Select(c => assign[c][d][b]).ToArray();
                        model.Add(LinearExpr.Sum(cdbVars) == block.RequiredCdb);
                    }
                    else if (block.RequiredCdb > 0)
                    {
                        // Aucun CDB disponible mais requis → infaisable
                        model.Add(LinearExpr.Constant(0) >= 1);
                    }

                    // Exactement RequiredOpl OPL affectés à ce bloc
                    var oplIndices = crewByRank.GetValueOrDefault(CrewRank.OPL, new List<int>());
                    if (oplIndices.Count > 0)
                    {
                        var oplVars = oplIndices.Select(c => assign[c][d][b]).ToArray();
                        model.Add(LinearExpr.Sum(oplVars) == block.RequiredOpl);
                    }
                    else if (block.RequiredOpl > 0)
                    {
                        model.Add(LinearExpr.Constant(0) >= 1);
                    }
                }
                else // PNC
                {
                    // CC + PNC : couverture cabine
                    if (crewByRank.ContainsKey(CrewRank.CC))
                    {
                        var ccVars = crewByRank[CrewRank.CC]
                            .Select(c => assign[c][d][b]).ToArray();
                        if (ccVars.Length > 0)
                            model.Add(LinearExpr.Sum(ccVars) >= block.RequiredCc);
                    }
                }
            }
        }

        // --- C5 : Temps de service cumulatif — fenêtre glissante 7 jours ---
        AddSlidingWindowDutyConstraint(model, assign, works, dayByIndex, crewByIndex, 7, rules.MaxDuty7dMinutes);

        // --- C6 : Temps de service cumulatif — fenêtre glissante 14 jours ---
        AddSlidingWindowDutyConstraint(model, assign, works, dayByIndex, crewByIndex, 14, rules.MaxDuty14dMinutes);

        // --- C7 : Temps de service cumulatif — fenêtre glissante 28 jours ---
        if (numDays >= 28)
            AddSlidingWindowDutyConstraint(model, assign, works, dayByIndex, crewByIndex, 28, rules.MaxDuty28dMinutes);

        // --- C8 : HDV cumulative 28 jours ---
        if (numDays >= 28)
            AddSlidingWindowHdvConstraint(model, assign, dayByIndex, crewByIndex, 28, rules.MaxHdv28dMinutes);

        // --- C8b : HDV cumulative 90 jours (ORO.FTL.210) ---
        if (numDays >= 90)
            AddSlidingWindowHdvConstraint(model, assign, dayByIndex, crewByIndex, 90, rules.MaxHdv90dMinutes);

        // --- C9 : Jours OFF minimum par 28 jours ---
        // Garde : 8 jours OFF sur 28 jours n'est applicable que si la période >= 28 jours.
        // Pour des périodes plus courtes, C11a (max 6j ON consécutifs) et C12 (repos hebdo)
        // garantissent un repos suffisant.
        if (numDays >= 28)
            AddMinDaysOffConstraint(model, works, numDays, numCrew, rules.MinDaysOffPerMonth);

        // --- C10 : Repos minimum entre deux services ---
        // Simplifié : si un navigant travaille le jour d avec un bloc qui finit tard,
        // et qu'un bloc le jour d+1 commence tôt, vérifier le repos minimum.
        AddMinRestConstraint(model, assign, works, dayByIndex, crewByIndex, rules.MinRestMinutes);

        // --- C11 : Repos 2 jours locaux tous les 6 jours ON maximum ---
        // Deux sous-contraintes :
        //   C11a : Max 6 jours consécutifs ON (jamais 7+)
        //   C11b : Un jour OFF isolé ne compte pas (pattern ON-OFF-ON interdit)
        // Combinées : après 1 à 6 jours ON, repos de 2+ jours consécutifs OFF.
        AddMaxConsecutiveWorkDaysConstraint(model, works, numDays, numCrew, rules.MaxConsecutiveWorkDays);
        AddMinRestDaysConstraint(model, works, numDays, numCrew, rules.MinRestDaysPerPeriod);

        // --- C12 : Repos 36h + 2 nuitées locales par semaine civile ---
        // Dans chaque semaine civile (lundi→dimanche),
        // au moins 2 jours consécutifs OFF (= repos ≥ 36h couvrant 2 nuits).
        AddWeeklyRestConstraint(model, works, dayByIndex, numCrew, rules.WeeklyRestNights);

        // --- C13 : Repos mensuel week-end (3 jours incluant samedi + dimanche) ---
        // 1 fois par mois calendaire, chaque PN doit avoir un repos de 3 jours
        // consécutifs incluant obligatoirement un samedi ET un dimanche.
        // Combinaisons possibles : Ven+Sam+Dim ou Sam+Dim+Lun.
        // Garde : ne pas appliquer sur un horizon < 28 jours (contrainte mensuelle,
        // sur un horizon court le PN obtiendra son repos week-end sur les semaines
        // non couvertes par le solver).
        if (rules.MonthlyWeekendRestRequired && numDays >= 28)
        {
            AddMonthlyWeekendRestConstraint(model, works, dayByIndex, numCrew, rules.MonthlyWeekendRestDays);
        }

        // --- C15 : Repos étendu de récupération — fenêtre glissante 7 jours (ORO.FTL.235(d)) ---
        // Dans toute fenêtre glissante de 168h (7 jours), au moins 2 jours consécutifs OFF.
        // Complète C12 (repos hebdo par semaine civile) en couvrant les cas aux frontières
        // de semaines ISO.
        {
            int extWindowDays = rules.ExtendedRestPeriodMinutes / (24 * 60); // 168h = 7j
            if (extWindowDays > 0 && numDays >= extWindowDays)
                AddExtendedRecoveryRestConstraint(model, works, numDays, numCrew, extWindowDays);
        }

        // ──────────────────────────────────────────────────
        // Objectif : minimiser le nombre de navigants utilisés
        // ──────────────────────────────────────────────────

        model.Minimize(LinearExpr.Sum(isUsed));

        // ──────────────────────────────────────────────────
        // Résolution
        // ──────────────────────────────────────────────────

        var solver = new CpSolver();
        solver.StringParameters = $"max_time_in_seconds:{request.TimeoutSeconds} num_workers:{request.NumWorkers}";

        _logger.LogDebug(
            "Lancement CP-SAT : timeout={Timeout}s, workers={Workers}",
            request.TimeoutSeconds, request.NumWorkers);

        var status = solver.Solve(model);

        return status switch
        {
            CpSolverStatus.Optimal or CpSolverStatus.Feasible =>
                BuildResult(solver, assign, works, isUsed, request, status, fdpWarnings),
            CpSolverStatus.Infeasible =>
                new SizingResult
                {
                    Status = SolverStatus.Infeasible,
                    Message = "Programme non couvrable avec l'effectif disponible. " +
                              "Augmenter l'effectif ou réduire le programme.",
                    FdpWarnings = fdpWarnings,
                },
            _ => new SizingResult
            {
                Status = SolverStatus.Timeout,
                Message = $"Solver arrêté après {request.TimeoutSeconds}s sans solution optimale.",
                FdpWarnings = fdpWarnings,
            }
        };
    }

    // ──────────────────────────────────────────────────────
    // Construction du résultat
    // ──────────────────────────────────────────────────────

    private SizingResult BuildResult(
        CpSolver solver,
        BoolVar[][][] assign,
        BoolVar[][] works,
        BoolVar[] isUsed,
        SizingRequest request,
        CpSolverStatus cpStatus,
        List<string> fdpWarnings)
    {
        var crew = request.AvailableCrew;
        var days = request.DailyPrograms;

        // Compter les navigants utilisés par rang
        var usedByRank = new Dictionary<CrewRank, int>();
        var totalByRank = new Dictionary<CrewRank, int>();

        foreach (var rank in crew.Select(c => c.Rank).Distinct())
        {
            var indicesOfRank = Enumerable.Range(0, crew.Count)
                .Where(i => crew[i].Rank == rank).ToList();

            usedByRank[rank] = indicesOfRank.Count(i => solver.Value(isUsed[i]) == 1);
            totalByRank[rank] = indicesOfRank.Count;
        }

        // Calculer les marges
        var marginByRank = totalByRank.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value - usedByRank.GetValueOrDefault(kvp.Key, 0));

        // Identifier les jours critiques (marge ≤ 1 pour un rang)
        var criticalDays = new List<CriticalDay>();
        for (int d = 0; d < days.Count; d++)
        {
            if (days[d].Blocks.Count == 0) continue;

            foreach (var rank in usedByRank.Keys)
            {
                var indicesOfRank = Enumerable.Range(0, crew.Count)
                    .Where(i => crew[i].Rank == rank).ToList();

                int workingToday = indicesOfRank.Count(i => solver.Value(works[i][d]) == 1);
                int availableToday = indicesOfRank.Count(i =>
                    !crew[i].UnavailableDates.Contains(days[d].Date));
                int margin = availableToday - workingToday;

                if (margin <= 1)
                {
                    criticalDays.Add(new CriticalDay
                    {
                        Date = days[d].Date,
                        Rank = rank,
                        Available = availableToday,
                        Required = workingToday,
                        Reason = margin == 0
                            ? "Aucune marge — tous les navigants disponibles sont affectés"
                            : "Marge minimale — un seul navigant de réserve"
                    });
                }
            }
        }

        // Construire les affectations jour par jour
        var assignments = new List<DailyAssignment>();
        for (int d = 0; d < days.Count; d++)
        {
            var blockAssignments = new List<BlockAssignment>();
            for (int b = 0; b < days[d].Blocks.Count; b++)
            {
                var assignedCrew = new List<string>();
                for (int c = 0; c < crew.Count; c++)
                {
                    if (solver.Value(assign[c][d][b]) == 1)
                        assignedCrew.Add(crew[c].Trigram);
                }
                blockAssignments.Add(new BlockAssignment
                {
                    BlockCode = days[d].Blocks[b].Code,
                    AssignedCrew = assignedCrew
                });
            }

            var dayOff = new List<string>();
            for (int c = 0; c < crew.Count; c++)
            {
                if (solver.Value(works[c][d]) == 0 &&
                    !crew[c].UnavailableDates.Contains(days[d].Date))
                {
                    dayOff.Add(crew[c].Trigram);
                }
            }

            assignments.Add(new DailyAssignment
            {
                Date = days[d].Date,
                BlockAssignments = blockAssignments,
                CrewOnDayOff = dayOff
            });
        }

        // Déterminer la contrainte mordante
        var bindingConstraint = DetermineBindingConstraint(
            solver, crew, days, works, assign, request.FtlRules);

        return new SizingResult
        {
            Status = cpStatus == CpSolverStatus.Optimal
                ? SolverStatus.Optimal
                : SolverStatus.Feasible,
            Message = cpStatus == CpSolverStatus.Optimal
                ? "Solution optimale trouvée."
                : "Solution réalisable (non prouvée optimale dans le temps imparti).",
            MinimumCrewByRank = usedByRank,
            MarginByRank = marginByRank,
            CriticalDays = criticalDays,
            Assignments = assignments,
            BindingConstraint = bindingConstraint,
            FdpWarnings = fdpWarnings,
        };
    }

    // ──────────────────────────────────────────────────────
    // Pré-contrôle FDP (ORO.FTL.205)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie que chaque bloc respecte la Table 2 EASA ORO.FTL.205.
    /// Si un bloc dépasse le FDP max autorisé, un warning est émis.
    /// </summary>
    private List<string> ValidateBlocksFdp(SizingRequest request)
    {
        var warnings = new List<string>();
        var table = request.FtlRules.FdpLimitTable;

        if (table.Count == 0)
            return warnings; // Pas de table configurée → bypass silencieux

        foreach (var day in request.DailyPrograms)
        {
            foreach (var block in day.Blocks)
            {
                var maxFdp = FdpLimitTableFactory.LookupMaxFdp(
                    table, block.FdpStartMinutes, block.SectorCount);

                if (maxFdp.HasValue && block.FdpMinutes > maxFdp.Value)
                {
                    int h = block.FdpStartMinutes / 60;
                    int m = block.FdpStartMinutes % 60;
                    warnings.Add(
                        $"Bloc {block.Code} le {day.Date:yyyy-MM-dd} : " +
                        $"FDP {block.FdpMinutes / 60}h{block.FdpMinutes % 60:D2} dépasse le max " +
                        $"Table 2 ({maxFdp.Value / 60}h{maxFdp.Value % 60:D2}) pour " +
                        $"report {h:D2}:{m:D2} / {block.SectorCount} secteur(s).");

                    _logger.LogWarning(
                        "ORO.FTL.205 : Bloc {Code} le {Date} — FDP {Fdp}min > max {MaxFdp}min " +
                        "(report {Report}, {Sectors} secteurs)",
                        block.Code, day.Date, block.FdpMinutes, maxFdp.Value,
                        $"{h:D2}:{m:D2}", block.SectorCount);
                }
            }
        }

        return warnings;
    }

    // ──────────────────────────────────────────────────────
    // Contraintes FTL
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Temps de service cumulatif sur fenêtre glissante de N jours ≤ maxMinutes.
    /// Pour chaque navigant, pour chaque fenêtre [d, d+windowDays) :
    ///   sum(assign[c,d',b] × block.DutyMinutes) ≤ maxMinutes
    /// </summary>
    private static void AddSlidingWindowDutyConstraint(
        CpModel model,
        BoolVar[][][] assign,
        BoolVar[][] works,
        List<DailyProgram> days,
        List<CrewMemberInfo> crew,
        int windowDays,
        int maxMinutes)
    {
        int numDays = days.Count;
        int numCrew = crew.Count;

        for (int c = 0; c < numCrew; c++)
        {
            for (int startDay = 0; startDay <= numDays - windowDays; startDay++)
            {
                var dutyTerms = new List<LinearExpr>();

                for (int d = startDay; d < startDay + windowDays; d++)
                {
                    for (int b = 0; b < days[d].Blocks.Count; b++)
                    {
                        int dutyMinutes = days[d].Blocks[b].DutyMinutes;
                        dutyTerms.Add(assign[c][d][b] * dutyMinutes);
                    }
                }

                if (dutyTerms.Count > 0)
                {
                    model.Add(LinearExpr.Sum(dutyTerms) <= maxMinutes);
                }
            }
        }
    }

    /// <summary>
    /// HDV cumulative sur fenêtre glissante de N jours ≤ maxMinutes.
    /// </summary>
    private static void AddSlidingWindowHdvConstraint(
        CpModel model,
        BoolVar[][][] assign,
        List<DailyProgram> days,
        List<CrewMemberInfo> crew,
        int windowDays,
        int maxMinutes)
    {
        int numDays = days.Count;
        int numCrew = crew.Count;

        for (int c = 0; c < numCrew; c++)
        {
            for (int startDay = 0; startDay <= numDays - windowDays; startDay++)
            {
                var hdvTerms = new List<LinearExpr>();

                for (int d = startDay; d < startDay + windowDays; d++)
                {
                    for (int b = 0; b < days[d].Blocks.Count; b++)
                    {
                        int blockTimeMinutes = days[d].Blocks[b].BlockTimeMinutes;
                        hdvTerms.Add(assign[c][d][b] * blockTimeMinutes);
                    }
                }

                if (hdvTerms.Count > 0)
                {
                    model.Add(LinearExpr.Sum(hdvTerms) <= maxMinutes);
                }
            }
        }
    }

    /// <summary>
    /// Jours OFF minimum par fenêtre de 28 jours.
    /// Un jour OFF = works[c,d] == 0 ET pas en indisponibilité.
    /// </summary>
    private static void AddMinDaysOffConstraint(
        CpModel model,
        BoolVar[][] works,
        int numDays,
        int numCrew,
        int minDaysOff)
    {
        for (int c = 0; c < numCrew; c++)
        {
            // Par fenêtre glissante de 28 jours
            int windowSize = Math.Min(28, numDays);
            for (int startDay = 0; startDay <= numDays - windowSize; startDay++)
            {
                var workDays = new List<BoolVar>();
                for (int d = startDay; d < startDay + windowSize; d++)
                {
                    workDays.Add(works[c][d]);
                }

                // sum(works) ≤ windowSize - minDaysOff
                // ↔ au moins minDaysOff jours OFF
                model.Add(LinearExpr.Sum(workDays) <= windowSize - minDaysOff);
            }
        }
    }

    /// <summary>
    /// Repos minimum entre deux services consécutifs.
    /// Simplifié au niveau journalier : si le bloc du jour d finit à H_fin
    /// et le bloc du jour d+1 commence à H_début, on vérifie :
    ///   (24h - H_fin + H_début) × 60 ≥ minRestMinutes
    /// Si la condition n'est pas satisfaite, le navigant ne peut pas
    /// travailler les deux jours consécutivement.
    /// </summary>
    private static void AddMinRestConstraint(
        CpModel model,
        BoolVar[][][] assign,
        BoolVar[][] works,
        List<DailyProgram> days,
        List<CrewMemberInfo> crew,
        int minRestMinutes)
    {
        int numDays = days.Count;
        int numCrew = crew.Count;

        for (int c = 0; c < numCrew; c++)
        {
            for (int d = 0; d < numDays - 1; d++)
            {
                var blocksToday = days[d].Blocks;
                var blocksTomorrow = days[d + 1].Blocks;

                for (int b1 = 0; b1 < blocksToday.Count; b1++)
                {
                    for (int b2 = 0; b2 < blocksTomorrow.Count; b2++)
                    {
                        int endToday = blocksToday[b1].DpEndMinutes;
                        int startTomorrow = blocksTomorrow[b2].DpStartMinutes;

                        // Repos = (24*60 - endToday) + startTomorrow
                        int restMinutes = (24 * 60 - endToday) + startTomorrow;

                        // ORO.FTL.235(a)(1) : repos au home base = max(durée duty précédent, 12h)
                        int requiredRest = Math.Max(blocksToday[b1].DutyMinutes, minRestMinutes);
                        if (restMinutes < requiredRest)
                        {
                            // Interdit : ne peut pas travailler ces deux blocs consécutivement
                            model.Add(assign[c][d][b1] + assign[c][d + 1][b2] <= 1);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// C11a — Max N jours consécutifs ON.
    /// 
    /// Règle Air Calédonie : jamais plus de 6 jours ON d'affilée.
    /// 
    /// Modélisation CP-SAT :
    /// Pour chaque navigant c, pour chaque fenêtre de (maxDays + 1) jours consécutifs :
    ///   sum(works[c, d..d+maxDays]) ≤ maxDays
    /// → dans toute fenêtre de 7 jours, au moins 1 jour OFF.
    /// </summary>
    private static void AddMaxConsecutiveWorkDaysConstraint(
        CpModel model,
        BoolVar[][] works,
        int numDays,
        int numCrew,
        int maxConsecutiveWorkDays)
    {
        int windowSize = maxConsecutiveWorkDays + 1; // 6 + 1 = 7

        for (int c = 0; c < numCrew; c++)
        {
            for (int startDay = 0; startDay <= numDays - windowSize; startDay++)
            {
                var workVars = new BoolVar[windowSize];
                for (int i = 0; i < windowSize; i++)
                    workVars[i] = works[c][startDay + i];

                // Au plus maxConsecutiveWorkDays jours travaillés dans la fenêtre
                // → au moins 1 jour OFF dans toute fenêtre de 7 jours
                model.Add(LinearExpr.Sum(workVars) <= maxConsecutiveWorkDays);
            }
        }
    }

    /// <summary>
    /// C11b — Un jour OFF isolé ne compte pas comme repos valide.
    /// Le pattern ON-OFF-ON est interdit.
    /// 
    /// Quand un navigant s'arrête, il doit s'arrêter au moins 2 jours consécutifs.
    /// Un seul jour OFF entre deux jours ON ne remet pas le compteur à zéro.
    /// 
    /// Modélisation CP-SAT :
    /// Pour chaque navigant c, pour chaque triplet (d-1, d, d+1) :
    ///   works[c, d-1] - works[c, d] + works[c, d+1] ≤ 1
    /// 
    /// Vérification :
    ///   ON-OFF-ON  → 1 - 0 + 1 = 2 > 1 ❌ (interdit)
    ///   ON-OFF-OFF → 1 - 0 + 0 = 1 ≤ 1 ✓
    ///   ON-ON-ON   → 1 - 1 + 1 = 1 ≤ 1 ✓
    ///   OFF-OFF-ON → 0 - 0 + 1 = 1 ≤ 1 ✓
    ///   OFF-ON-OFF → 0 - 1 + 0 = -1 ≤ 1 ✓ (jour ON isolé autorisé)
    /// </summary>
    private static void AddMinRestDaysConstraint(
        CpModel model,
        BoolVar[][] works,
        int numDays,
        int numCrew,
        int minRestDays)
    {
        if (minRestDays < 2) return; // Pas de contrainte si repos 1 jour suffit

        for (int c = 0; c < numCrew; c++)
        {
            // Contrainte de base : pas de jour OFF isolé (ON-OFF-ON interdit)
            for (int d = 1; d < numDays - 1; d++)
            {
                // works[d-1] - works[d] + works[d+1] ≤ 1
                // Interdit ON-OFF-ON
                model.Add(
                    works[c][d - 1] - works[c][d] + works[c][d + 1] <= 1);
            }

            // Si minRestDays > 2, étendre la contrainte pour des repos de 3+ jours
            // Exemple minRestDays=3 : interdire aussi ON-OFF-OFF-ON
            for (int restLen = 2; restLen < minRestDays; restLen++)
            {
                for (int d = 1; d < numDays - restLen; d++)
                {
                    // Vérifier que si works[d-1]=ON et works[d..d+restLen-1] sont tous OFF,
                    // alors works[d+restLen] doit aussi être OFF.
                    // 
                    // Linéarisation : works[d-1] + (restLen - sum(works[d..d+restLen-1])) + works[d+restLen]
                    // ne peut pas valoir restLen + 2 (= tous OFF au milieu + ON des deux côtés)
                    //
                    // Plus simplement avec une variable auxiliaire :
                    var allOffInMiddle = model.NewBoolVar($"midoff_{c}_{d}_{restLen}");

                    // allOffInMiddle = 1 ssi tous les jours d..d+restLen-1 sont OFF
                    var middleWork = new BoolVar[restLen];
                    for (int k = 0; k < restLen; k++)
                        middleWork[k] = works[c][d + k];

                    model.Add(LinearExpr.Sum(middleWork) == 0).OnlyEnforceIf(allOffInMiddle);
                    model.Add(LinearExpr.Sum(middleWork) >= 1).OnlyEnforceIf(allOffInMiddle.Not());

                    // Si le jour avant est ON et le milieu est tout OFF,
                    // alors le jour après doit être OFF
                    // works[d-1]=1 AND allOffInMiddle=1 → works[d+restLen]=0
                    var bothConditions = model.NewBoolVar($"both_{c}_{d}_{restLen}");
                    model.Add(works[c][d - 1] == 1).OnlyEnforceIf(bothConditions);
                    model.Add(allOffInMiddle == 1).OnlyEnforceIf(bothConditions);
                    model.AddBoolOr(new[] { works[c][d - 1].Not(), allOffInMiddle.Not(), bothConditions });

                    model.Add(works[c][d + restLen] == 0).OnlyEnforceIf(bothConditions);
                }
            }
        }
    }

    /// <summary>
    /// Repos hebdomadaire : 36h + 2 nuitées locales par semaine civile (lundi→dimanche).
    /// 
    /// Règle Air Calédonie : chaque semaine civile, le navigant doit bénéficier
    /// d'un repos de 36h minimum incluant 2 nuitées locales consécutives.
    /// 
    /// En modélisation journalière : au moins 2 jours consécutifs OFF
    /// dans chaque semaine civile (lundi=1 → dimanche=7).
    /// 
    /// Note : cette contrainte est distincte du repos 48h/6j car elle s'applique
    /// à la semaine civile fixe, pas à une fenêtre glissante.
    /// </summary>
    private static void AddWeeklyRestConstraint(
        CpModel model,
        BoolVar[][] works,
        List<DailyProgram> days,
        int numCrew,
        int requiredConsecutiveNights)
    {
        int numDays = days.Count;

        // Identifier les semaines civiles couvertes par la période
        // Regrouper les jours par semaine civile (ISO : lundi = début)
        var weekGroups = new Dictionary<(int Year, int Week), List<int>>();

        for (int d = 0; d < numDays; d++)
        {
            var date = days[d].Date;
            var dt = date.ToDateTime(TimeOnly.MinValue);
            int isoYear = System.Globalization.ISOWeek.GetYear(dt);
            int isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(dt);

            var key = (isoYear, isoWeek);
            if (!weekGroups.ContainsKey(key))
                weekGroups[key] = new List<int>();
            weekGroups[key].Add(d);
        }

        foreach (var (weekKey, dayIndices) in weekGroups)
        {
            // Ne contraindre que les semaines complètes ou quasi-complètes
            // (au moins requiredConsecutiveNights + 1 jours dans la semaine)
            if (dayIndices.Count < requiredConsecutiveNights + 1)
                continue;

            dayIndices.Sort();

            for (int c = 0; c < numCrew; c++)
            {
                // Pour chaque paire de jours consécutifs dans cette semaine civile,
                // créer une variable "les N jours consécutifs sont OFF"
                var consecutiveOffVars = new List<BoolVar>();

                for (int i = 0; i < dayIndices.Count - (requiredConsecutiveNights - 1); i++)
                {
                    // Vérifier que les jours sont réellement consécutifs dans le calendrier
                    bool areConsecutive = true;
                    for (int n = 0; n < requiredConsecutiveNights; n++)
                    {
                        if (i + n >= dayIndices.Count) { areConsecutive = false; break; }
                        if (n > 0 && dayIndices[i + n] != dayIndices[i + n - 1] + 1)
                        { areConsecutive = false; break; }
                    }
                    if (!areConsecutive) continue;

                    // Créer variable : les N jours consécutifs sont tous OFF
                    var allOff = model.NewBoolVar(
                        $"weeklyrest_{c}_w{weekKey.Week}_{i}");

                    for (int n = 0; n < requiredConsecutiveNights; n++)
                    {
                        model.Add(works[c][dayIndices[i + n]] == 0)
                            .OnlyEnforceIf(allOff);
                    }

                    // Implication inverse : si au moins un jour est travaillé → allOff = 0
                    var workVars = new BoolVar[requiredConsecutiveNights];
                    for (int n = 0; n < requiredConsecutiveNights; n++)
                        workVars[n] = works[c][dayIndices[i + n]];

                    model.Add(LinearExpr.Sum(workVars) >= 1)
                        .OnlyEnforceIf(allOff.Not());

                    consecutiveOffVars.Add(allOff);
                }

                if (consecutiveOffVars.Count > 0)
                {
                    // Au moins une séquence de N jours consécutifs OFF dans la semaine civile
                    model.Add(LinearExpr.Sum(consecutiveOffVars) >= 1);
                }
            }
        }
    }

    /// <summary>
    /// Repos mensuel week-end : 1 fois par mois calendaire, chaque PN doit avoir
    /// un repos de 3 jours consécutifs incluant obligatoirement un samedi ET un dimanche.
    /// 
    /// Les seules séquences de 3 jours consécutifs contenant Samedi + Dimanche sont :
    ///   - Vendredi + Samedi + Dimanche
    ///   - Samedi + Dimanche + Lundi
    /// 
    /// Modélisation CP-SAT :
    /// Pour chaque mois calendaire couvert par la période :
    ///   Pour chaque navigant c :
    ///     Identifier tous les week-ends (Sam+Dim) du mois.
    ///     Pour chaque week-end, créer 2 variables :
    ///       weekendOff_vsd = 1 ssi works[Ven]=0 AND works[Sam]=0 AND works[Dim]=0
    ///       weekendOff_sdl = 1 ssi works[Sam]=0 AND works[Dim]=0 AND works[Lun]=0
    ///     Imposer : sum(weekendOff_*) ≥ 1 sur le mois
    ///     (au moins un week-end du mois a un repos étendu Ven-Sam-Dim ou Sam-Dim-Lun)
    /// </summary>
    private static void AddMonthlyWeekendRestConstraint(
        CpModel model,
        BoolVar[][] works,
        List<DailyProgram> days,
        int numCrew,
        int restDays)
    {
        int numDays = days.Count;

        // Construire un index date → dayIndex pour accès rapide
        var dateToIndex = new Dictionary<DateOnly, int>();
        for (int d = 0; d < numDays; d++)
            dateToIndex[days[d].Date] = d;

        // Identifier les mois calendaires couverts
        var monthGroups = new Dictionary<(int Year, int Month), List<DateOnly>>();
        for (int d = 0; d < numDays; d++)
        {
            var date = days[d].Date;
            var key = (date.Year, date.Month);
            if (!monthGroups.ContainsKey(key))
                monthGroups[key] = new List<DateOnly>();
            monthGroups[key].Add(date);
        }

        foreach (var (monthKey, datesInMonth) in monthGroups)
        {
            // Identifier tous les samedis du mois qui sont dans la période
            var saturdays = datesInMonth
                .Where(d => d.DayOfWeek == DayOfWeek.Saturday)
                .ToList();

            if (saturdays.Count == 0)
                continue; // Mois partiel sans samedi → pas de contrainte

            for (int c = 0; c < numCrew; c++)
            {
                var weekendRestVars = new List<BoolVar>();

                foreach (var saturday in saturdays)
                {
                    var sunday = saturday.AddDays(1);
                    var friday = saturday.AddDays(-1);
                    var monday = sunday.AddDays(1);

                    // Les deux jours du week-end doivent être dans la période
                    if (!dateToIndex.ContainsKey(saturday) || !dateToIndex.ContainsKey(sunday))
                        continue;

                    int satIdx = dateToIndex[saturday];
                    int sunIdx = dateToIndex[sunday];

                    // Option 1 : Vendredi + Samedi + Dimanche
                    if (dateToIndex.TryGetValue(friday, out int friIdx))
                    {
                        var vsd = model.NewBoolVar(
                            $"mwr_vsd_{c}_{saturday:yyyyMMdd}");

                        // vsd = 1 → les 3 jours sont OFF
                        model.Add(works[c][friIdx] == 0).OnlyEnforceIf(vsd);
                        model.Add(works[c][satIdx] == 0).OnlyEnforceIf(vsd);
                        model.Add(works[c][sunIdx] == 0).OnlyEnforceIf(vsd);

                        // vsd = 0 → au moins un des 3 jours est travaillé
                        model.Add(works[c][friIdx] + works[c][satIdx] + works[c][sunIdx] >= 1)
                            .OnlyEnforceIf(vsd.Not());

                        weekendRestVars.Add(vsd);
                    }

                    // Option 2 : Samedi + Dimanche + Lundi
                    if (dateToIndex.TryGetValue(monday, out int monIdx))
                    {
                        var sdl = model.NewBoolVar(
                            $"mwr_sdl_{c}_{saturday:yyyyMMdd}");

                        // sdl = 1 → les 3 jours sont OFF
                        model.Add(works[c][satIdx] == 0).OnlyEnforceIf(sdl);
                        model.Add(works[c][sunIdx] == 0).OnlyEnforceIf(sdl);
                        model.Add(works[c][monIdx] == 0).OnlyEnforceIf(sdl);

                        // sdl = 0 → au moins un des 3 jours est travaillé
                        model.Add(works[c][satIdx] + works[c][sunIdx] + works[c][monIdx] >= 1)
                            .OnlyEnforceIf(sdl.Not());

                        weekendRestVars.Add(sdl);
                    }
                }

                if (weekendRestVars.Count > 0)
                {
                    // Au moins un repos week-end de 3 jours dans ce mois calendaire
                    model.Add(LinearExpr.Sum(weekendRestVars) >= 1);
                }
            }
        }
    }

    /// <summary>
    /// C15 — Repos étendu de récupération : dans toute fenêtre glissante de N jours,
    /// au moins 2 jours consécutifs OFF (approximation du repos 36h + 2 nuitées locales).
    /// ORO.FTL.235(d) — complète C12 (repos hebdo par semaine civile).
    /// </summary>
    private static void AddExtendedRecoveryRestConstraint(
        CpModel model,
        BoolVar[][] works,
        int numDays,
        int numCrew,
        int windowDays)
    {
        for (int c = 0; c < numCrew; c++)
        {
            for (int startDay = 0; startDay <= numDays - windowDays; startDay++)
            {
                // Au moins une paire de jours consécutifs OFF dans [startDay, startDay+windowDays-1]
                var consecutiveOffVars = new List<BoolVar>();

                for (int d = startDay; d < startDay + windowDays - 1; d++)
                {
                    var bothOff = model.NewBoolVar($"extrest_{c}_{startDay}_{d}");
                    model.Add(works[c][d] == 0).OnlyEnforceIf(bothOff);
                    model.Add(works[c][d + 1] == 0).OnlyEnforceIf(bothOff);
                    model.Add(works[c][d] + works[c][d + 1] >= 1).OnlyEnforceIf(bothOff.Not());
                    consecutiveOffVars.Add(bothOff);
                }

                if (consecutiveOffVars.Count > 0)
                    model.Add(LinearExpr.Sum(consecutiveOffVars) >= 1);
            }
        }
    }

    /// <summary>
    /// Analyse la solution pour identifier la contrainte la plus serrée.
    /// </summary>
    private static string? DetermineBindingConstraint(
        CpSolver solver,
        IReadOnlyList<CrewMemberInfo> crew,
        IReadOnlyList<DailyProgram> days,
        BoolVar[][] works,
        BoolVar[][][] assign,
        FtlRules rules)
    {
        double maxDutyRatio7d = 0;
        double maxDutyRatio14d = 0;
        double maxHdvRatio28d = 0;
        double maxHdvRatio90d = 0;
        double maxWorkRatio28d = 0;
        double maxConsecutiveWorkRatio = 0;
        double weeklyRestTightness = 0;
        double monthlyWeekendTightness = 0;

        for (int c = 0; c < crew.Count; c++)
        {
            // Temps de service sur 7 jours glissants
            for (int start = 0; start <= days.Count - 7; start++)
            {
                int totalDuty = 0;
                for (int d = start; d < start + 7; d++)
                    for (int b = 0; b < days[d].Blocks.Count; b++)
                        if (solver.Value(assign[c][d][b]) == 1)
                            totalDuty += days[d].Blocks[b].DutyMinutes;

                double ratio = rules.MaxDuty7dMinutes > 0
                    ? (double)totalDuty / rules.MaxDuty7dMinutes
                    : 0;
                maxDutyRatio7d = Math.Max(maxDutyRatio7d, ratio);
            }

            // Idem 14 jours
            for (int start = 0; start <= days.Count - 14; start++)
            {
                int totalDuty = 0;
                for (int d = start; d < start + 14; d++)
                    for (int b = 0; b < days[d].Blocks.Count; b++)
                        if (solver.Value(assign[c][d][b]) == 1)
                            totalDuty += days[d].Blocks[b].DutyMinutes;

                double ratio = rules.MaxDuty14dMinutes > 0
                    ? (double)totalDuty / rules.MaxDuty14dMinutes
                    : 0;
                maxDutyRatio14d = Math.Max(maxDutyRatio14d, ratio);
            }

            // HDV sur 28 jours
            int windowSize = Math.Min(28, days.Count);
            for (int start = 0; start <= days.Count - windowSize; start++)
            {
                int totalHdv = 0;
                for (int d = start; d < start + windowSize; d++)
                    for (int b = 0; b < days[d].Blocks.Count; b++)
                        if (solver.Value(assign[c][d][b]) == 1)
                            totalHdv += days[d].Blocks[b].BlockTimeMinutes;

                double ratio = rules.MaxHdv28dMinutes > 0
                    ? (double)totalHdv / rules.MaxHdv28dMinutes
                    : 0;
                maxHdvRatio28d = Math.Max(maxHdvRatio28d, ratio);
            }

            // HDV sur 90 jours
            if (days.Count >= 90)
            {
                for (int start = 0; start <= days.Count - 90; start++)
                {
                    int totalHdv90 = 0;
                    for (int d = start; d < start + 90; d++)
                        for (int b = 0; b < days[d].Blocks.Count; b++)
                            if (solver.Value(assign[c][d][b]) == 1)
                                totalHdv90 += days[d].Blocks[b].BlockTimeMinutes;

                    double ratio90 = rules.MaxHdv90dMinutes > 0
                        ? (double)totalHdv90 / rules.MaxHdv90dMinutes
                        : 0;
                    maxHdvRatio90d = Math.Max(maxHdvRatio90d, ratio90);
                }
            }

            // Jours travaillés sur 28 jours
            for (int start = 0; start <= days.Count - windowSize; start++)
            {
                int workedDays = 0;
                for (int d = start; d < start + windowSize; d++)
                    if (solver.Value(works[c][d]) == 1)
                        workedDays++;

                int maxWorkDays = windowSize - rules.MinDaysOffPerMonth;
                double ratio = maxWorkDays > 0
                    ? (double)workedDays / maxWorkDays
                    : 0;
                maxWorkRatio28d = Math.Max(maxWorkRatio28d, ratio);
            }

            // Jours consécutifs travaillés (repos 2j / max 6j ON)
            int consecutive = 0;
            for (int d = 0; d < days.Count; d++)
            {
                if (solver.Value(works[c][d]) == 1)
                    consecutive++;
                else
                    consecutive = 0;

                double ratio2 = rules.MaxConsecutiveWorkDays > 0
                    ? (double)consecutive / rules.MaxConsecutiveWorkDays
                    : 0;
                maxConsecutiveWorkRatio = Math.Max(maxConsecutiveWorkRatio, ratio2);
            }

            // Repos hebdomadaire par semaine civile
            // Vérifier le nombre max de jours travaillés dans chaque semaine civile
            // (max théorique = 5 si on exige 2 jours consécutifs OFF)
            for (int d = 0; d < days.Count; d++)
            {
                var dt = days[d].Date.ToDateTime(TimeOnly.MinValue);
                int dow = (int)days[d].Date.DayOfWeek;
                if (dow == 0) dow = 7; // Dimanche = 7
                int mondayOffset = dow - 1;
                int mondayIndex = d - mondayOffset;
                int sundayIndex = mondayIndex + 6;

                // Ne traiter que le lundi de chaque semaine
                if (d != Math.Max(0, mondayIndex)) continue;

                int weekStart = Math.Max(0, mondayIndex);
                int weekEnd = Math.Min(days.Count - 1, sundayIndex);
                int daysInWeek = weekEnd - weekStart + 1;
                if (daysInWeek < 3) continue;

                int workedInWeek = 0;
                for (int wd = weekStart; wd <= weekEnd; wd++)
                    if (solver.Value(works[c][wd]) == 1)
                        workedInWeek++;

                // Max jours travaillables = jours dans semaine - 2 (repos obligatoire)
                int maxWorkable = Math.Max(1, daysInWeek - rules.WeeklyRestNights);
                double ratio3 = (double)workedInWeek / maxWorkable;
                weeklyRestTightness = Math.Max(weeklyRestTightness, ratio3);
            }

            // Repos mensuel week-end : vérifier si des week-ends complets sont travaillés
            // Plus il y a de week-ends travaillés, plus la contrainte est serrée
            if (rules.MonthlyWeekendRestRequired)
            {
                var monthlyDates = new Dictionary<(int Y, int M), List<int>>();
                for (int d2 = 0; d2 < days.Count; d2++)
                {
                    var key2 = (days[d2].Date.Year, days[d2].Date.Month);
                    if (!monthlyDates.ContainsKey(key2))
                        monthlyDates[key2] = new List<int>();
                    monthlyDates[key2].Add(d2);
                }

                foreach (var (mKey, mDays) in monthlyDates)
                {
                    // Compter les week-ends où le navigant travaille le samedi ou dimanche
                    int weekendsWorked = 0;
                    int totalWeekends = 0;
                    foreach (int d2 in mDays)
                    {
                        if (days[d2].Date.DayOfWeek == DayOfWeek.Saturday)
                        {
                            totalWeekends++;
                            int sunD = d2 + 1;
                            if (sunD < days.Count &&
                                days[sunD].Date.DayOfWeek == DayOfWeek.Sunday)
                            {
                                bool satWorked = solver.Value(works[c][d2]) == 1;
                                bool sunWorked = solver.Value(works[c][sunD]) == 1;
                                if (satWorked || sunWorked)
                                    weekendsWorked++;
                            }
                        }
                    }

                    if (totalWeekends > 0)
                    {
                        // Ratio : si tous les week-ends sauf 1 sont travaillés → serré
                        int maxWorkableWeekends = Math.Max(0, totalWeekends - 1);
                        double ratio4 = maxWorkableWeekends > 0
                            ? (double)weekendsWorked / maxWorkableWeekends
                            : 0;
                        monthlyWeekendTightness = Math.Max(monthlyWeekendTightness, ratio4);
                    }
                }
            }
        }

        // Retourner la contrainte la plus serrée
        var constraints = new (string Name, double Ratio)[]
        {
            ("Temps de service 7j (60h max)", maxDutyRatio7d),
            ("Temps de service 14j (110h max)", maxDutyRatio14d),
            ("HDV 28j (100h max)", maxHdvRatio28d),
            ("HDV 90j (280h max)", maxHdvRatio90d),
            ("Jours travaillés 28j (max 20/28)", maxWorkRatio28d),
            ("Repos 2j local / max 6j ON consécutifs", maxConsecutiveWorkRatio),
            ("Repos 36h + 2 nuitées / semaine civile", weeklyRestTightness),
            ("Repos mensuel 3j + week-end", monthlyWeekendTightness),
        };

        var binding = constraints.MaxBy(x => x.Ratio);
        return binding.Ratio > 0
            ? $"{binding.Name} — utilisation {binding.Ratio:P0}"
            : null;
    }
}
