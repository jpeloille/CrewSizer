// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

using CrewSizer.Domain.Enums;
using CrewSizer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrewSizer.Application.Sizing;

/// <summary>
/// Service applicatif de dimensionnement.
/// Orchestre la collecte des données et l'appel au solver.
/// </summary>
public sealed class SizingService
{
    private readonly ISizingSolver _solver;
    private readonly ICrewRepository _crewRepo;
    private readonly IFlightBlockRepository _blockRepo;
    private readonly ICalendarAssignmentRepository _calendarRepo;
    private readonly IWeekPatternRepository _weekPatternRepo;
    private readonly ILeaveRequestRepository _leaveRepo;
    private readonly IFtlRuleSetRepository _ftlRepo;
    private readonly ILogger<SizingService> _logger;

    public SizingService(
        ISizingSolver solver,
        ICrewRepository crewRepo,
        IFlightBlockRepository blockRepo,
        ICalendarAssignmentRepository calendarRepo,
        IWeekPatternRepository weekPatternRepo,
        ILeaveRequestRepository leaveRepo,
        IFtlRuleSetRepository ftlRepo,
        ILogger<SizingService> logger)
    {
        _solver = solver;
        _crewRepo = crewRepo;
        _blockRepo = blockRepo;
        _calendarRepo = calendarRepo;
        _weekPatternRepo = weekPatternRepo;
        _leaveRepo = leaveRepo;
        _ftlRepo = ftlRepo;
        _logger = logger;
    }

    /// <summary>
    /// Lance le dimensionnement pour une catégorie sur une période donnée.
    /// Résout le programme (dépliage semaine type → calendrier réel) puis appelle le solver.
    /// </summary>
    public async Task<SizingResult> ComputeAsync(
        CrewCategory category,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Dimensionnement {Category} du {Start} au {End}",
            category, startDate, endDate);

        // 1. Charger l'équipage actif de la catégorie
        var crew = await _crewRepo.GetByCategoryAsync(category, ct);
        var activeCrew = crew.Where(c => c.IsActive).ToList();

        // 2. Charger les congés imposés sur la période
        var leaves = await _leaveRepo.GetApprovedInRangeAsync(startDate, endDate, ct);
        var leavesByMember = leaves
            .GroupBy(l => l.CrewMemberId)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(l => EnumerateDates(l.StartDate, l.EndDate)).ToHashSet());

        // 3. Résoudre le programme : calendrier → semaine type → blocs
        var dailyPrograms = await ResolveProgramAsync(startDate, endDate, ct);

        // 4. Charger les règles FTL
        var ruleSet = await _ftlRepo.GetDefaultAsync(ct)
            ?? throw new InvalidOperationException("Aucun jeu de règles FTL configuré.");

        // 5. Construire la requête solver
        var request = new SizingRequest
        {
            StartDate = startDate,
            EndDate = endDate,
            Category = category,
            DailyPrograms = dailyPrograms,
            AvailableCrew = activeCrew.Select(c => new CrewMemberInfo
            {
                Id = c.Id,
                Trigram = c.Trigram,
                Category = c.Category,
                Rank = c.Rank,
                IsExaminer = c.IsExaminer,
                UnavailableDates = leavesByMember.GetValueOrDefault(c.Id, new HashSet<DateOnly>()),
                OfficeDaysPerWeek = c.OfficeDaysPerWeek,
                WeekendOffFixed = c.WeekendOffFixed,
            }).ToList(),
            FtlRules = MapFtlRules(ruleSet)
        };

        // 6. Appeler le solver
        var result = await _solver.SolveAsync(request, ct);

        _logger.LogInformation(
            "Dimensionnement terminé : {Status} en {Time}ms — {Constraint}",
            result.Status, result.SolveTimeMs, result.BindingConstraint ?? "aucune");

        return result;
    }

    /// <summary>
    /// Résout le programme de vols : pour chaque jour de la période,
    /// détermine la semaine type affectée et retourne les blocs correspondants.
    /// </summary>
    private async Task<List<DailyProgram>> ResolveProgramAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var result = new List<DailyProgram>();
        var allBlocks = await _blockRepo.GetAllAsync(ct);
        var blockDict = allBlocks.ToDictionary(b => b.Id);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Trouver la semaine ISO
            var isoWeek = System.Globalization.ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));
            var isoYear = System.Globalization.ISOWeek.GetYear(date.ToDateTime(TimeOnly.MinValue));
            var dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek; // Lundi=1..Dimanche=7

            // Chercher l'affectation calendrier pour cette semaine
            var assignment = await _calendarRepo.GetByWeekAsync(isoYear, isoWeek, ct);
            if (assignment == null)
            {
                // Pas de programme ce jour → journée sans vol
                result.Add(new DailyProgram { Date = date, Blocks = Array.Empty<BlockInfo>() });
                continue;
            }

            // Récupérer les blocs de la semaine type pour ce jour de la semaine
            var weekPattern = await _weekPatternRepo.GetByIdWithBlocksAsync(assignment.WeekPatternId, ct);
            var dayBlocks = weekPattern?.Blocks
                .Where(b => b.DayOfWeek == dayOfWeek)
                .OrderBy(b => b.Sequence)
                .Select(wpb =>
                {
                    var block = blockDict.GetValueOrDefault(wpb.FlightBlockId);
                    if (block == null) return null;
                    return new BlockInfo
                    {
                        Id = block.Id,
                        Code = block.Code,
                        SectorCount = block.Flights?.Count ?? 0,
                        BlockTimeMinutes = block.Flights?.Sum(f => f.BlockTimeMinutes) ?? 0,
                        FdpStartMinutes = block.FdpStart.Hour * 60 + block.FdpStart.Minute,
                        FdpEndMinutes = block.FdpEnd.Hour * 60 + block.FdpEnd.Minute,
                        DpStartMinutes = block.DpStart.Hour * 60 + block.DpStart.Minute,
                        DpEndMinutes = block.DpEnd.Hour * 60 + block.DpEnd.Minute,
                    };
                })
                .Where(b => b != null)
                .Cast<BlockInfo>()
                .ToList() ?? new List<BlockInfo>();

            result.Add(new DailyProgram { Date = date, Blocks = dayBlocks });
        }

        return result;
    }

    private static FtlRules MapFtlRules(Domain.Entities.FtlRuleSet ruleSet)
    {
        return new FtlRules
        {
            MaxDuty7dMinutes = (int)(ruleSet.MaxDuty7d * 60),
            MaxDuty14dMinutes = (int)(ruleSet.MaxDuty14d * 60),
            MaxDuty28dMinutes = (int)(ruleSet.MaxDuty28d * 60),
            MaxHdv28dMinutes = (int)(ruleSet.MaxHdv28d * 60),
            MaxHdv90dMinutes = (int)(ruleSet.MaxHdv90d * 60),
            MinDaysOffPerMonth = ruleSet.MinDaysOffMonth,
            MinRestMinutes = (int)(ruleSet.MinRestHours * 60),
            ExtendedRestMinutes = (int)(ruleSet.ExtendedRestHours * 60),
            ExtendedRestPeriodMinutes = (int)(ruleSet.ExtendedRestPeriodHours * 60),
            // Repos spécifiques Air Calédonie
            MaxConsecutiveWorkDays = ruleSet.MaxConsecutiveWorkDays,
            MinRestDaysPerPeriod = ruleSet.MinRestDaysPerPeriod,
            WeeklyRestMinutes = (int)(ruleSet.WeeklyRestHours * 60),
            WeeklyRestNights = ruleSet.WeeklyRestNights,
            // Repos mensuel week-end
            MonthlyWeekendRestRequired = ruleSet.MonthlyWeekendRestRequired,
            MonthlyWeekendRestDays = ruleSet.MonthlyWeekendRestDays,
            // Table 2 EASA ORO.FTL.205 — FDP max (équipage acclimaté)
            FdpLimitTable = FdpLimitTableFactory.CreateEasaTable2(),
        };
    }

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly start, DateOnly end)
    {
        for (var d = start; d <= end; d = d.AddDays(1))
            yield return d;
    }
}
