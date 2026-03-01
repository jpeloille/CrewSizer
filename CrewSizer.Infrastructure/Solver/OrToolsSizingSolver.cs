// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using System.Diagnostics;
using CrewSizer.Domain.Sizing;
using CrewSizer.Domain.Enums;
using Google.OrTools.Sat;
using Microsoft.Extensions.Logging;

namespace CrewSizer.Infrastructure.Solver;

/// <summary>
/// Module 1 — Dimensionnement d'effectif via Google OR-Tools CP-SAT.
/// 
/// Vérifie jour par jour que l'effectif disponible couvre le programme de vols
/// en respectant toutes les contraintes FTL EASA ORO.FTL.
/// 
/// Variables : assign[crew, dayIndex, blockIndex] ∈ {0, 1}
/// Objectif  : Minimiser le nombre total de navigants utilisés (= effectif minimum requis)
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
                    if (crewByRank.ContainsKey(CrewRank.CDB))
                    {
                        var cdbVars = crewByRank[CrewRank.CDB]
                            .Select(c => assign[c][d][b]).ToArray();
                        if (cdbVars.Length > 0)
                            model.Add(LinearExpr.Sum(cdbVars) == block.RequiredCdb);
                    }

                    // Exactement RequiredOpl OPL affectés à ce bloc
                    if (crewByRank.ContainsKey(CrewRank.OPL))
                    {
                        var oplVars = crewByRank[CrewRank.OPL]
                            .Select(c => assign[c][d][b]).ToArray();
                        if (oplVars.Length > 0)
                            model.Add(LinearExpr.Sum(oplVars) == block.RequiredOpl);
                    }
                }
                else // PNC
                {
                    // C4a : Au moins RequiredCc CC (le CC ne peut pas être remplacé par un PNC)
                    BoolVar[] ccVars = [];
                    if (crewByRank.ContainsKey(CrewRank.CC))
                    {
                        ccVars = crewByRank[CrewRank.CC]
                            .Select(c => assign[c][d][b]).ToArray();
                        if (ccVars.Length > 0)
                            model.Add(LinearExpr.Sum(ccVars) >= block.RequiredCc);
                    }

                    // C4b : Total cabine (CC + PNC) >= RequiredCc + RequiredPnc
                    // Un CC peut occuper un poste PNC (polyvalence CC → PNC)
                    BoolVar[] pncVars = [];
                    if (crewByRank.ContainsKey(CrewRank.PNC))
                    {
                        pncVars = crewByRank[CrewRank.PNC]
                            .Select(c => assign[c][d][b]).ToArray();
                    }

                    var totalCabinRequired = block.RequiredCc + block.RequiredPnc;
                    var allCabinVars = ccVars.Concat(pncVars).ToArray();
                    if (allCabinVars.Length > 0 && totalCabinRequired > 0)
                        model.Add(LinearExpr.Sum(allCabinVars) >= totalCabinRequired);
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

        // --- C9 : Jours OFF minimum par 28 jours ---
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
        if (rules.MonthlyWeekendRestRequired)
        {
            AddMonthlyWeekendRestConstraint(model, works, dayByIndex, numCrew, rules.MonthlyWeekendRestDays);
        }

        // --- C14 : RDOV — repos weekend + max 2,5j vol/semaine ---
        // Le navigant RDOV effectue 2,5 jours de bureau par semaine civile.
        // Il ne vole jamais le samedi ni le dimanche.
        if (crew.Any(c => c.IsRdov))
        {
            AddRdovConstraint(model, works, dayByIndex, crewByIndex);
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
                BuildResult(solver, assign, works, isUsed, request, status),
            CpSolverStatus.Infeasible =>
                new SizingResult
                {
                    Status = SolverStatus.Infeasible,
                    Message = "Programme non couvrable avec l'effectif disponible. " +
                              "Augmenter l'effectif ou réduire le programme.",
                },
            _ => new SizingResult
            {
                Status = SolverStatus.Timeout,
                Message = $"Solver arrêté après {request.TimeoutSeconds}s sans solution optimale.",
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
        CpSolverStatus cpStatus)
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
        var (bindingLabel, bindingCode, bindingSource) = DetermineBindingConstraint(
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
            BindingConstraint = bindingLabel,
            BindingConstraintCode = bindingCode,
            BindingConstraintSource = bindingSource,
        };
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

                        if (restMinutes < minRestMinutes)
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
    /// Analyse la solution pour identifier la contrainte la plus serrée.
    /// Retourne (label, code, source) de la contrainte mordante.
    /// </summary>
    private static (string? Label, string? Code, ConstraintSource? Source) DetermineBindingConstraint(
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
        double maxWorkRatio28d = 0;
        double maxConsecutiveWorkRatio = 0;
        double weeklyRestTightness = 0;
        double monthlyWeekendTightness = 0;
        double rdovTightness = 0;

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

        // RDOV : ratio jours volés / 2,5 par semaine civile (sur les navigants RDOV)
        for (int c = 0; c < crew.Count; c++)
        {
            if (!crew[c].IsRdov) continue;

            // Grouper les jours ouvrables par semaine civile
            var weekWork = new Dictionary<(int Y, int W), int>();
            for (int d = 0; d < days.Count; d++)
            {
                if (days[d].Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;
                if (solver.Value(works[c][d]) != 1) continue;

                var dt = days[d].Date.ToDateTime(TimeOnly.MinValue);
                var key = (System.Globalization.ISOWeek.GetYear(dt),
                           System.Globalization.ISOWeek.GetWeekOfYear(dt));
                weekWork[key] = weekWork.GetValueOrDefault(key) + 1;
            }

            foreach (var (_, flightDays) in weekWork)
            {
                double ratio = flightDays / 2.5;
                rdovTightness = Math.Max(rdovTightness, ratio);
            }
        }

        // Retourner la contrainte la plus serrée (code + label + source)
        var constraints = new (string Code, string Name, double Ratio)[]
        {
            ("C5", $"TSV 7j ({rules.MaxDuty7dMinutes / 60}h max)", maxDutyRatio7d),
            ("C6", $"TSV 14j ({rules.MaxDuty14dMinutes / 60}h max)", maxDutyRatio14d),
            ("C8", $"HDV 28j ({rules.MaxHdv28dMinutes / 60}h max)", maxHdvRatio28d),
            ("C9", $"Jours travaillés 28j (max {28 - rules.MinDaysOffPerMonth}/28)", maxWorkRatio28d),
            ("C11a", $"Max {rules.MaxConsecutiveWorkDays}j ON consécutifs", maxConsecutiveWorkRatio),
            ("C12", $"Repos hebdo {rules.WeeklyRestNights} nuitées/sem. civile", weeklyRestTightness),
            ("C13", $"Repos mensuel {rules.MonthlyWeekendRestDays}j + week-end", monthlyWeekendTightness),
            ("C14", "RDOV 2,5j vol/sem.", rdovTightness),
        };

        var binding = constraints.MaxBy(x => x.Ratio);
        if (binding.Ratio <= 0)
            return (null, null, null);

        var source = ConstraintRegistry.All.TryGetValue(binding.Code, out var def)
            ? def.Source
            : (ConstraintSource?)null;

        var label = $"{binding.Name} — utilisation {binding.Ratio:P0}";
        return (label, binding.Code, source);
    }

    // ──────────────────────────────────────────────────────
    // C14 — RDOV : repos weekend + max 2,5 jours de vol / semaine
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// Contrainte RDOV : le navigant affecté au rôle RDOV effectue 2,5 jours
    /// de bureau par semaine civile et ne vole jamais le samedi ni le dimanche.
    /// Modélisation : C14a (weekend OFF) + C14b (≤ 3j vol/semaine) + C14c (≤ 5j vol/2 semaines).
    /// </summary>
    private static void AddRdovConstraint(
        CpModel model,
        BoolVar[][] works,
        List<DailyProgram> days,
        List<CrewMemberInfo> crew)
    {
        int numDays = days.Count;

        for (int c = 0; c < crew.Count; c++)
        {
            if (!crew[c].IsRdov)
                continue;

            // ── C14a : Repos obligatoire samedi + dimanche ──
            for (int d = 0; d < numDays; d++)
            {
                var dow = days[d].Date.DayOfWeek;
                if (dow is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    model.Add(works[c][d] == 0);
            }

            // ── Grouper les jours par semaine civile ISO (lundi→dimanche) ──
            var weekGroups = new Dictionary<(int Year, int Week), List<int>>();
            for (int d = 0; d < numDays; d++)
            {
                var date = days[d].Date;
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue; // Jours ouvrables uniquement (Lun-Ven)

                var dt = date.ToDateTime(TimeOnly.MinValue);
                var isoYear = System.Globalization.ISOWeek.GetYear(dt);
                var isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(dt);
                var key = (isoYear, isoWeek);

                if (!weekGroups.ContainsKey(key))
                    weekGroups[key] = [];
                weekGroups[key].Add(d);
            }

            var sortedWeeks = weekGroups.OrderBy(kv => kv.Key).ToList();

            // ── C14b : Max 3 jours de vol par semaine civile ──
            foreach (var (_, weekDays) in sortedWeeks)
            {
                var weekVars = weekDays.Select(d => works[c][d]).ToArray();
                if (weekVars.Length > 0)
                    model.Add(LinearExpr.Sum(weekVars) <= 3);
            }

            // ── C14c : Max 5 jours de vol par 2 semaines civiles consécutives ──
            for (int w = 0; w < sortedWeeks.Count - 1; w++)
            {
                var twoWeekDays = sortedWeeks[w].Value
                    .Concat(sortedWeeks[w + 1].Value)
                    .Select(d => works[c][d])
                    .ToArray();

                if (twoWeekDays.Length > 0)
                    model.Add(LinearExpr.Sum(twoWeekDays) <= 5);
            }
        }
    }
}
