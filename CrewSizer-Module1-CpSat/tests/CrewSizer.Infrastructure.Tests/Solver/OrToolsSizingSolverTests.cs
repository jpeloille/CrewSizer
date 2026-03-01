// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using CrewSizer.Application.Sizing;
using CrewSizer.Domain.Enums;
using CrewSizer.Infrastructure.Solver;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CrewSizer.Infrastructure.Tests.Solver;

/// <summary>
/// Tests du Module 1 — Dimensionnement CP-SAT.
/// Contexte : Air Calédonie, ATR 72-600, réseau domestique NC.
/// Effectif réel : 10 CDB + 9 OPL (PNT), 14 CC + 3 PNC + 1 RPN (PNC).
/// </summary>
public class OrToolsSizingSolverTests
{
    private readonly OrToolsSizingSolver _solver;

    public OrToolsSizingSolverTests()
    {
        _solver = new OrToolsSizingSolver(NullLogger<OrToolsSizingSolver>.Instance);
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 1 : Faisabilité de base
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_LowSeason_OneWeek_ShouldBeFeasible()
    {
        // Arrange — Basse saison : 2 blocs/jour sauf dimanche
        // Besoin : 2 CDB + 2 OPL par jour → effectif 10+9 largement suffisant
        var start = new DateOnly(2026, 3, 2); // Lundi
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        // Act
        var result = await _solver.SolveAsync(request);

        // Assert
        result.IsFeasible.Should().BeTrue();
        result.Status.Should().BeOneOf(SolverStatus.Optimal, SolverStatus.Feasible);
        result.MinimumCrewByRank.Should().ContainKey(CrewRank.CDB);
        result.MinimumCrewByRank.Should().ContainKey(CrewRank.OPL);
    }

    [Fact]
    public async Task Solve_LowSeason_FullMonth_ShouldBeFeasible()
    {
        // Arrange — Un mois complet basse saison (28 jours)
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(27),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 28),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 30,
        };

        // Act
        var result = await _solver.SolveAsync(request);

        // Assert
        result.IsFeasible.Should().BeTrue();
        result.Assignments.Should().HaveCount(28);
        result.BindingConstraint.Should().NotBeNullOrEmpty();
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 2 : Effectif minimum requis
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_LowSeason_ShouldRequire_AtLeast2Cdb_And2Opl()
    {
        // 2 blocs/jour → besoin simultané de 2 CDB + 2 OPL
        // Mais avec jours OFF obligatoires (8/28), il en faut plus
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeTrue();
        result.MinimumCrewByRank[CrewRank.CDB].Should().BeGreaterThanOrEqualTo(2);
        result.MinimumCrewByRank[CrewRank.OPL].Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Solve_HighSeason_ShouldRequireMoreCrew()
    {
        // Haute saison : 3 blocs/jour → besoin 3 CDB + 3 OPL simultanément
        var start = new DateOnly(2026, 3, 2);
        var lowRequest = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var highRequest = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateHighSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var lowResult = await _solver.SolveAsync(lowRequest);
        var highResult = await _solver.SolveAsync(highRequest);

        lowResult.IsFeasible.Should().BeTrue();
        highResult.IsFeasible.Should().BeTrue();

        // Haute saison nécessite au moins autant de CDB que basse saison
        var highCdb = highResult.MinimumCrewByRank[CrewRank.CDB];
        var lowCdb = lowResult.MinimumCrewByRank[CrewRank.CDB];
        highCdb.Should().BeGreaterThanOrEqualTo(lowCdb);
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 3 : Effectif insuffisant → Infeasible
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_InsufficientCrew_ShouldBeInfeasible()
    {
        // Seulement 1 CDB + 1 OPL pour un programme à 2 blocs/jour
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 1, numOpl: 1),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeFalse();
        result.Status.Should().Be(SolverStatus.Infeasible);
    }

    [Fact]
    public async Task Solve_ZeroOpl_ShouldBeInfeasible()
    {
        // 10 CDB mais 0 OPL → impossible de couvrir les blocs
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 10, numOpl: 0),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 4 : Indisponibilités (congés)
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_WithUnavailableCrew_ShouldStillBeFeasible()
    {
        // 3 CDB en congé sur la période → reste 7 CDB → devrait suffire
        var start = new DateOnly(2026, 3, 2);
        var unavailableDates = Enumerable.Range(0, 7)
            .Select(i => start.AddDays(i))
            .ToHashSet();

        var crew = TestDataBuilder.CreatePntCrew();
        // Mettre 3 CDB en congé
        var cdbOnLeave = crew.Where(c => c.Rank == CrewRank.CDB).Take(3).ToList();
        var updatedCrew = crew.Select(c =>
            cdbOnLeave.Contains(c)
                ? new CrewMemberInfo
                {
                    Id = c.Id, Trigram = c.Trigram, Category = c.Category,
                    Rank = c.Rank, IsExaminer = c.IsExaminer,
                    UnavailableDates = unavailableDates,
                }
                : c
        ).ToList();

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = updatedCrew,
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeTrue();
    }

    [Fact]
    public async Task Solve_AllCdbOnLeave_ExceptOne_ShouldBeInfeasible()
    {
        // 9 CDB en congé, 1 seul restant → ne peut pas couvrir 2 blocs/jour
        var start = new DateOnly(2026, 3, 2);
        var unavailableDates = Enumerable.Range(0, 7)
            .Select(i => start.AddDays(i))
            .ToHashSet();

        var crew = TestDataBuilder.CreatePntCrew();
        var cdbOnLeave = crew.Where(c => c.Rank == CrewRank.CDB).Skip(1).ToList(); // tous sauf 1
        var updatedCrew = crew.Select(c =>
            cdbOnLeave.Contains(c)
                ? new CrewMemberInfo
                {
                    Id = c.Id, Trigram = c.Trigram, Category = c.Category,
                    Rank = c.Rank, IsExaminer = c.IsExaminer,
                    UnavailableDates = unavailableDates,
                }
                : c
        ).ToList();

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = updatedCrew,
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 5 : Jours critiques
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_TightCrew_ShouldIdentifyCriticalDays()
    {
        // Effectif juste suffisant : 3 CDB + 3 OPL pour 2 blocs/jour
        // → jours critiques attendus
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 3, numOpl: 3),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);

        // Avec seulement 3 CDB pour 2 blocs/jour (6 jours sur 7),
        // les jours de vol devraient être critiques
        if (result.IsFeasible)
        {
            result.CriticalDays.Should().NotBeEmpty(
                "avec un effectif serré, des jours critiques sont attendus");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 6 : Jour sans vol
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_NoFlightDay_ShouldHaveNoAssignments()
    {
        var start = new DateOnly(2026, 3, 1); // Dimanche
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start,
            Category = CrewCategory.PNT,
            DailyPrograms = new[] { TestDataBuilder.CreateNoFlightDay(start) },
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 5,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeTrue();
        result.Assignments.Should().HaveCount(1);
        result.Assignments[0].BlockAssignments.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 7 : Contraintes FTL
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_StrictFtl_ShouldRequireMoreCrew()
    {
        // Règles FTL très restrictives → plus de navigants nécessaires
        var start = new DateOnly(2026, 3, 2);
        var strictRules = new FtlRules
        {
            MaxDuty7dMinutes = 40 * 60,    // 40h au lieu de 60h
            MaxDuty14dMinutes = 70 * 60,   // 70h au lieu de 110h
            MaxDuty28dMinutes = 120 * 60,  // 120h au lieu de 190h
            MaxHdv28dMinutes = 60 * 60,    // 60h au lieu de 100h
            MinDaysOffPerMonth = 12,       // 12 jours OFF au lieu de 8
            MinRestMinutes = 14 * 60,      // 14h au lieu de 12h
        };

        var normalRequest = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(13),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 14),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 15,
        };

        var strictRequest = normalRequest with { FtlRules = strictRules };

        var normalResult = await _solver.SolveAsync(normalRequest);
        var strictResult = await _solver.SolveAsync(strictRequest);

        normalResult.IsFeasible.Should().BeTrue();
        strictResult.IsFeasible.Should().BeTrue();

        // Avec des règles plus strictes, on a besoin d'autant ou plus de navigants
        var strictUsed = strictResult.MinimumCrewByRank.Values.Sum();
        var normalUsed = normalResult.MinimumCrewByRank.Values.Sum();
        strictUsed.Should().BeGreaterThanOrEqualTo(normalUsed);
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 8 : Repos minimum inter-bloc
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_BlocksWithInsufficientRest_ShouldNotAssignSameCrewConsecutively()
    {
        // Bloc soir finit à 22:00, bloc matin commence à 05:00 = 7h de repos
        // → repos minimum 12h non respecté → même navigant interdit sur les deux
        var start = new DateOnly(2026, 3, 2);
        var lateBlock = new BlockInfo
        {
            Id = Guid.NewGuid(), Code = "BS1", SectorCount = 3,
            BlockTimeMinutes = 105,
            FdpStartMinutes = 17 * 60, FdpEndMinutes = 22 * 60,
            DpStartMinutes = 16 * 60 + 30, DpEndMinutes = 22 * 60 + 30,
        };
        var earlyBlock = TestDataBuilder.CreateMorningBlock();

        var program = new List<DailyProgram>
        {
            new() { Date = start, Blocks = new[] { lateBlock } },
            new() { Date = start.AddDays(1), Blocks = new[] { earlyBlock } },
        };

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(1),
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 2, numOpl: 2),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeTrue();

        // Vérifier qu'aucun navigant n'est sur les deux blocs
        if (result.Assignments.Count == 2)
        {
            var crewDay1 = result.Assignments[0].BlockAssignments
                .SelectMany(b => b.AssignedCrew).ToHashSet();
            var crewDay2 = result.Assignments[1].BlockAssignments
                .SelectMany(b => b.AssignedCrew).ToHashSet();

            crewDay1.Intersect(crewDay2).Should().BeEmpty(
                "le repos entre bloc soir (22h30) et bloc matin (05h00) = 6h30 < 12h minimum");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 8b : Repos 2 jours locaux / max 6 jours ON
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_ShouldNeverExceed6ConsecutiveWorkDays()
    {
        // 14 jours de programme avec vols tous les jours
        // → chaque navigant doit avoir max 6 jours ON d'affilée
        var start = new DateOnly(2026, 3, 2); // Lundi
        var program = new List<DailyProgram>();
        for (int i = 0; i < 14; i++)
            program.Add(TestDataBuilder.CreateLowSeasonDay(start.AddDays(i)));

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(13),
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 15,
        };

        var result = await _solver.SolveAsync(request);
        result.IsFeasible.Should().BeTrue();

        // Vérifier : max 6 jours consécutifs travaillés par navigant
        var crewTrigrams = request.AvailableCrew.Select(c => c.Trigram).ToHashSet();
        foreach (var trigram in crewTrigrams)
        {
            int maxConsecutive = 0;
            int current = 0;

            for (int d = 0; d < result.Assignments.Count; d++)
            {
                bool worksToday = result.Assignments[d].BlockAssignments
                    .Any(b => b.AssignedCrew.Contains(trigram));

                if (worksToday)
                {
                    current++;
                    maxConsecutive = Math.Max(maxConsecutive, current);
                }
                else
                {
                    current = 0;
                }
            }

            maxConsecutive.Should().BeLessThanOrEqualTo(6,
                $"le navigant {trigram} ne peut pas travailler plus de 6 jours " +
                "consécutifs (repos 2 jours requis après max 6 ON)");
        }
    }

    [Fact]
    public async Task Solve_ShouldNotHaveSingleDayOffBetweenWorkDays()
    {
        // Vérifier qu'il n'y a jamais un pattern ON-OFF-ON
        // (un jour OFF isolé ne compte pas comme repos valide)
        var start = new DateOnly(2026, 3, 2);
        var program = new List<DailyProgram>();
        for (int i = 0; i < 14; i++)
            program.Add(TestDataBuilder.CreateLowSeasonDay(start.AddDays(i)));

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(13),
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 15,
        };

        var result = await _solver.SolveAsync(request);
        result.IsFeasible.Should().BeTrue();

        var crewTrigrams = request.AvailableCrew.Select(c => c.Trigram).ToHashSet();
        foreach (var trigram in crewTrigrams)
        {
            for (int d = 1; d < result.Assignments.Count - 1; d++)
            {
                bool prevOn = result.Assignments[d - 1].BlockAssignments
                    .Any(b => b.AssignedCrew.Contains(trigram));
                bool todayOff = !result.Assignments[d].BlockAssignments
                    .Any(b => b.AssignedCrew.Contains(trigram));
                bool nextOn = result.Assignments[d + 1].BlockAssignments
                    .Any(b => b.AssignedCrew.Contains(trigram));

                bool singleDayOff = prevOn && todayOff && nextOn;

                singleDayOff.Should().BeFalse(
                    $"le navigant {trigram} a un jour OFF isolé le {result.Assignments[d].Date} " +
                    "(ON-OFF-ON interdit — le repos doit être de 2 jours minimum)");
            }
        }
    }

    [Fact]
    public async Task Solve_InsufficientCrewFor6DayRule_ShouldBeInfeasible()
    {
        // 2 CDB + 2 OPL, programme 14 jours, vols chaque jour
        // → avec max 6j ON + 2j OFF obligatoires + pas de jour OFF isolé,
        //   les 2 CDB ne peuvent pas se relayer suffisamment
        var start = new DateOnly(2026, 3, 2);
        var program = new List<DailyProgram>();
        for (int i = 0; i < 14; i++)
            program.Add(TestDataBuilder.CreateLowSeasonDay(start.AddDays(i)));

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(13),
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 2, numOpl: 2),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);

        // 2 CDB pour 2 blocs/jour pendant 14j avec repos 2j/6j obligatoire
        result.IsFeasible.Should().BeFalse(
            "2 CDB ne suffisent pas pour 2 blocs/jour pendant 14 jours " +
            "avec max 6j ON + repos 2j obligatoire");
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 8c : Repos 36h + 2 nuitées / semaine civile
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_ShouldEnforce2ConsecutiveNightsOffPerCivilWeek()
    {
        // 2 semaines complètes (lundi à dimanche) avec vols chaque jour
        var start = new DateOnly(2026, 3, 2); // Lundi
        var program = new List<DailyProgram>();
        for (int i = 0; i < 14; i++)
            program.Add(TestDataBuilder.CreateLowSeasonDay(start.AddDays(i)));

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(13),
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 15,
        };

        var result = await _solver.SolveAsync(request);
        result.IsFeasible.Should().BeTrue();

        // Pour chaque navigant et chaque semaine civile,
        // vérifier qu'il y a au moins 2 jours consécutifs OFF
        var crewTrigrams = request.AvailableCrew.Select(c => c.Trigram).ToHashSet();

        // Semaine 1 : jours 0-6, Semaine 2 : jours 7-13
        foreach (var trigram in crewTrigrams)
        {
            for (int weekStart = 0; weekStart < 14; weekStart += 7)
            {
                bool hasConsecutiveOff = false;

                for (int d = weekStart; d < weekStart + 6; d++)
                {
                    bool offToday = !result.Assignments[d].BlockAssignments
                        .Any(b => b.AssignedCrew.Contains(trigram));
                    bool offTomorrow = !result.Assignments[d + 1].BlockAssignments
                        .Any(b => b.AssignedCrew.Contains(trigram));

                    if (offToday && offTomorrow)
                    {
                        hasConsecutiveOff = true;
                        break;
                    }
                }

                hasConsecutiveOff.Should().BeTrue(
                    $"le navigant {trigram} doit avoir 2 jours consécutifs OFF " +
                    $"dans la semaine civile commençant jour {weekStart} " +
                    "(repos 36h + 2 nuitées locales)");
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 8d : Repos mensuel 3 jours incluant Sam+Dim
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_ShouldEnforceMonthlyWeekendRest_3DaysWithSatAndSun()
    {
        // Mois complet de mars 2026 avec vols tous les jours
        // → chaque navigant doit avoir au moins un repos de 3 jours
        //   incluant un samedi ET un dimanche (= Ven+Sam+Dim ou Sam+Dim+Lun)
        var start = new DateOnly(2026, 3, 1); // Dimanche
        var end = new DateOnly(2026, 3, 31);
        int numDays = end.DayNumber - start.DayNumber + 1;

        var program = new List<DailyProgram>();
        for (int i = 0; i < numDays; i++)
            program.Add(TestDataBuilder.CreateLowSeasonDay(start.AddDays(i)));

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = end,
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 30,
        };

        var result = await _solver.SolveAsync(request);
        result.IsFeasible.Should().BeTrue();

        // Vérifier que chaque navigant a au moins un repos Ven+Sam+Dim ou Sam+Dim+Lun
        var crewTrigrams = request.AvailableCrew.Select(c => c.Trigram).ToHashSet();

        foreach (var trigram in crewTrigrams)
        {
            bool hasWeekendRest = false;

            for (int d = 0; d < result.Assignments.Count; d++)
            {
                var date = result.Assignments[d].Date;
                if (date.DayOfWeek != DayOfWeek.Saturday) continue;

                // Vérifier si Ven+Sam+Dim sont tous OFF
                int friIdx = d - 1;
                int satIdx = d;
                int sunIdx = d + 1;

                bool satOff = !result.Assignments[satIdx].BlockAssignments
                    .Any(b => b.AssignedCrew.Contains(trigram));

                if (!satOff) continue; // Samedi travaillé → pas ce week-end

                bool sunOff = sunIdx < result.Assignments.Count &&
                    !result.Assignments[sunIdx].BlockAssignments
                        .Any(b => b.AssignedCrew.Contains(trigram));

                if (!sunOff) continue; // Dimanche travaillé → pas ce week-end

                // Option Ven+Sam+Dim
                if (friIdx >= 0)
                {
                    bool friOff = !result.Assignments[friIdx].BlockAssignments
                        .Any(b => b.AssignedCrew.Contains(trigram));
                    if (friOff)
                    {
                        hasWeekendRest = true;
                        break;
                    }
                }

                // Option Sam+Dim+Lun
                int monIdx = sunIdx + 1;
                if (monIdx < result.Assignments.Count)
                {
                    bool monOff = !result.Assignments[monIdx].BlockAssignments
                        .Any(b => b.AssignedCrew.Contains(trigram));
                    if (monOff)
                    {
                        hasWeekendRest = true;
                        break;
                    }
                }
            }

            hasWeekendRest.Should().BeTrue(
                $"le navigant {trigram} doit avoir au moins un repos de 3 jours " +
                "incluant samedi + dimanche dans le mois de mars 2026 " +
                "(Ven+Sam+Dim ou Sam+Dim+Lun)");
        }
    }

    [Fact]
    public async Task Solve_MonthlyWeekendRest_WithMinimalCrew_ShouldStillBeFeasible()
    {
        // 4 CDB + 4 OPL pour 2 blocs/jour sur un mois complet
        // → le repos week-end de 3 jours réduit la capacité
        //   mais 4 CDB devraient suffire
        var start = new DateOnly(2026, 3, 2); // Lundi
        var program = new List<DailyProgram>();
        for (int i = 0; i < 28; i++)
            program.Add(TestDataBuilder.CreateLowSeasonDay(start.AddDays(i)));

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(27),
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 4, numOpl: 4),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 30,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeTrue(
            "4 CDB + 4 OPL devraient suffire pour 2 blocs/jour " +
            "même avec repos mensuel week-end de 3 jours");
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 9 : Marge et contrainte mordante
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_FullMonth_ShouldReportBindingConstraint()
    {
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(27),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 28),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 30,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeTrue();
        result.BindingConstraint.Should().NotBeNullOrEmpty(
            "sur un mois, au moins une contrainte FTL doit être identifiée comme mordante");
    }

    [Fact]
    public async Task Solve_FullMonth_MarginShouldBePositive()
    {
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(27),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 28),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 30,
        };

        var result = await _solver.SolveAsync(request);

        result.IsFeasible.Should().BeTrue();
        // Avec 10 CDB + 9 OPL pour 2 blocs/jour, la marge doit être confortable
        foreach (var (rank, margin) in result.MarginByRank)
        {
            margin.Should().BeGreaterThanOrEqualTo(0,
                $"la marge pour {rank} ne devrait pas être négative");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 10 : Performance
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_FullMonth_ShouldCompleteWithinTimeout()
    {
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(27),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 28),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 30,
        };

        var result = await _solver.SolveAsync(request);

        result.SolveTimeMs.Should().BeLessThan(30_000,
            "le dimensionnement d'un mois devrait se résoudre en moins de 30s");
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 11 : Affectations cohérentes
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_Assignments_EachBlock_ShouldHaveExactlyOneCdbAndOneOpl()
    {
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);
        result.IsFeasible.Should().BeTrue();

        var crewDict = request.AvailableCrew.ToDictionary(c => c.Trigram);

        foreach (var day in result.Assignments)
        {
            foreach (var block in day.BlockAssignments)
            {
                var assignedCdb = block.AssignedCrew
                    .Where(t => crewDict[t].Rank == CrewRank.CDB).Count();
                var assignedOpl = block.AssignedCrew
                    .Where(t => crewDict[t].Rank == CrewRank.OPL).Count();

                assignedCdb.Should().Be(1,
                    $"le bloc {block.BlockCode} le {day.Date} doit avoir exactement 1 CDB");
                assignedOpl.Should().Be(1,
                    $"le bloc {block.BlockCode} le {day.Date} doit avoir exactement 1 OPL");
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 12 : ORO.FTL.205 — Validation FDP Table 2
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_BlockExceedsFdpLimit_ShouldWarn()
    {
        // Bloc avec FDP de 14h (840 min), report 06:00, 4 secteurs
        // Table 2 : 06:00–13:29 / 4 secteurs → FDP max = 12h00 (720 min)
        // → warning attendu
        var start = new DateOnly(2026, 3, 2);
        var longFdpBlock = new BlockInfo
        {
            Id = Guid.NewGuid(), Code = "BX1", SectorCount = 4,
            BlockTimeMinutes = 4 * 60,
            FdpStartMinutes = 6 * 60,          // 06:00
            FdpEndMinutes = 20 * 60,            // 20:00 → FDP = 14h = 840 min
            DpStartMinutes = 5 * 60 + 30,
            DpEndMinutes = 20 * 60 + 30,
        };

        var program = new List<DailyProgram>
        {
            new() { Date = start, Blocks = new[] { longFdpBlock } },
        };

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start,
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 2, numOpl: 2),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 5,
        };

        var result = await _solver.SolveAsync(request);

        result.FdpWarnings.Should().NotBeEmpty(
            "un bloc avec FDP 14h dépasse le max Table 2 (12h pour report 06:00 / 4 secteurs)");
        result.FdpWarnings[0].Should().Contain("BX1");
    }

    [Fact]
    public async Task Solve_BlockWithinFdpLimit_ShouldNotWarn()
    {
        // Bloc matin standard : FDP 7h, report 05:30, 4 secteurs
        // Table 2 : 05:30–05:44 / 4 secteurs → FDP max = 11h30 (690 min)
        // → pas de warning
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start,
            Category = CrewCategory.PNT,
            DailyPrograms = new[] { TestDataBuilder.CreateLowSeasonDay(start) },
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 2, numOpl: 2),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 5,
        };

        var result = await _solver.SolveAsync(request);
        result.FdpWarnings.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 14 : ORO.FTL.235(d) — repos étendu glissant 7j
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_ShouldHave2ConsecutiveOffInEvery7DayWindow()
    {
        // 14 jours de programme avec vols tous les jours
        // → dans toute fenêtre glissante de 7 jours, au moins 2 jours consécutifs OFF
        var start = new DateOnly(2026, 3, 2); // Lundi
        var program = new List<DailyProgram>();
        for (int i = 0; i < 14; i++)
            program.Add(TestDataBuilder.CreateLowSeasonDay(start.AddDays(i)));

        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(13),
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 15,
        };

        var result = await _solver.SolveAsync(request);
        result.IsFeasible.Should().BeTrue();

        // Pour chaque navigant, vérifier la propriété glissante
        var crewTrigrams = request.AvailableCrew.Select(c => c.Trigram).ToHashSet();
        foreach (var trigram in crewTrigrams)
        {
            for (int windowStart = 0; windowStart <= result.Assignments.Count - 7; windowStart++)
            {
                bool hasConsecutiveOff = false;

                for (int d = windowStart; d < windowStart + 6; d++)
                {
                    bool offToday = !result.Assignments[d].BlockAssignments
                        .Any(b => b.AssignedCrew.Contains(trigram));
                    bool offTomorrow = !result.Assignments[d + 1].BlockAssignments
                        .Any(b => b.AssignedCrew.Contains(trigram));

                    if (offToday && offTomorrow)
                    {
                        hasConsecutiveOff = true;
                        break;
                    }
                }

                hasConsecutiveOff.Should().BeTrue(
                    $"le navigant {trigram} doit avoir 2 jours consécutifs OFF " +
                    $"dans la fenêtre glissante jours {windowStart}–{windowStart + 6} " +
                    "(ORO.FTL.235(d) repos étendu 36h + 2 nuitées / 168h)");
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Scénario 15 : ORO.FTL.235(a) — repos = max(duty, 12h)
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task Solve_BlockWith14hDuty_ShouldRequire14hRest()
    {
        // Bloc 1 : duty de 14h30 (05:00 → 19:30), DpEnd = 1170 min
        // Bloc 2 : duty commence à 09:00, DpStart = 540 min
        // Repos disponible = (1440 - 1170) + 540 = 810 min = 13h30
        // Ancien code (12h fixe) : 810 ≥ 720 → autorisé ✓
        // Nouveau code (max(duty,12h)) : requiredRest = max(870,720) = 870, 810 < 870 → interdit ✓
        var start = new DateOnly(2026, 3, 2);
        var longBlock = new BlockInfo
        {
            Id = Guid.NewGuid(), Code = "BL1", SectorCount = 6,
            BlockTimeMinutes = 6 * 35,
            FdpStartMinutes = 5 * 60 + 30,    // 05:30
            FdpEndMinutes = 19 * 60,           // 19:00
            DpStartMinutes = 5 * 60,           // 05:00
            DpEndMinutes = 19 * 60 + 30,       // 19:30 → duty = 14h30 = 870 min
        };
        var nextBlock = new BlockInfo
        {
            Id = Guid.NewGuid(), Code = "BM1", SectorCount = 4,
            BlockTimeMinutes = 4 * 35,
            FdpStartMinutes = 9 * 60 + 30,    // 09:30
            FdpEndMinutes = 16 * 60 + 30,     // 16:30
            DpStartMinutes = 9 * 60,           // 09:00
            DpEndMinutes = 17 * 60,            // 17:00
        };

        var program = new List<DailyProgram>
        {
            new() { Date = start, Blocks = new[] { longBlock } },
            new() { Date = start.AddDays(1), Blocks = new[] { nextBlock } },
        };

        // 2 CDB + 2 OPL : avec repos 12h fixe, 1 suffirait par rang.
        // Avec repos max(duty,12h), le même PN ne peut pas enchaîner → 2 requis.
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(1),
            Category = CrewCategory.PNT,
            DailyPrograms = program,
            AvailableCrew = TestDataBuilder.CreatePntCrew(numCdb: 2, numOpl: 2),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);
        result.IsFeasible.Should().BeTrue();

        // Vérifier qu'aucun navigant ne fait les deux jours consécutivement
        if (result.Assignments.Count == 2)
        {
            var crewDay1 = result.Assignments[0].BlockAssignments
                .SelectMany(b => b.AssignedCrew).ToHashSet();
            var crewDay2 = result.Assignments[1].BlockAssignments
                .SelectMany(b => b.AssignedCrew).ToHashSet();

            crewDay1.Intersect(crewDay2).Should().BeEmpty(
                "repos disponible (13h30) < repos requis max(14h30 duty, 12h) = 14h30 " +
                "→ le même PN ne peut pas enchaîner les deux blocs");
        }
    }

    [Fact]
    public async Task Solve_Assignments_NoCrew_ShouldBeAssignedToTwoBlocks_SameDay()
    {
        var start = new DateOnly(2026, 3, 2);
        var request = new SizingRequest
        {
            StartDate = start,
            EndDate = start.AddDays(6),
            Category = CrewCategory.PNT,
            DailyPrograms = TestDataBuilder.CreateLowSeasonProgram(start, 7),
            AvailableCrew = TestDataBuilder.CreatePntCrew(),
            FtlRules = TestDataBuilder.DefaultFtlRules,
            TimeoutSeconds = 10,
        };

        var result = await _solver.SolveAsync(request);
        result.IsFeasible.Should().BeTrue();

        foreach (var day in result.Assignments)
        {
            var allAssigned = day.BlockAssignments
                .SelectMany(b => b.AssignedCrew)
                .ToList();

            allAssigned.Should().OnlyHaveUniqueItems(
                $"le {day.Date} aucun navigant ne doit être affecté à 2 blocs");
        }
    }
}
