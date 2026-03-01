using System.Globalization;
using CrewSizer.Domain.Entities;
using CrewSizer.Domain.Enums;
using CrewSizer.Domain.Services;
using CrewSizer.Domain.Sizing;
using CrewSizer.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CrewSizer.Application.Features.Sizing.Commands;

public record RunSizingCommand(
    Guid ScenarioId,
    string? CalculePar = null,
    string? SolveId = null
) : IRequest<CombinedSizingResult>;

public class RunSizingHandler(
    IDbContextFactory<CrewSizerDbContext> dbFactory,
    ISizingSolver solver) : IRequestHandler<RunSizingCommand, CombinedSizingResult>
{
    public async Task<CombinedSizingResult> Handle(RunSizingCommand request, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // 1. Charger le scénario
        var scenario = await db.Scenarios.FindAsync([request.ScenarioId], cancellationToken)
            ?? throw new KeyNotFoundException($"Scénario '{request.ScenarioId}' introuvable.");

        // 2. Charger tous les membres actifs (PNT + PNC)
        var membres = await db.MembresEquipage
            .AsNoTracking()
            .Where(m => m.Actif)
            .ToListAsync(cancellationToken);

        // 3. Charger les indisponibilités sur la période (UTC pour PostgreSQL timestamptz)
        var debutPeriode = DateTime.SpecifyKind(
            scenario.Periode.DateDebut.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var finPeriode = DateTime.SpecifyKind(
            scenario.Periode.DateFin.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var membreIds = membres.Select(m => m.Id).ToList();
        var indispos = await db.DisponibilitesMembre
            .AsNoTracking()
            .Where(i => membreIds.Contains(i.MembreId))
            .Where(i => i.DateDebut <= finPeriode && (!i.DateFin.HasValue || i.DateFin >= debutPeriode))
            .ToListAsync(cancellationToken);

        var indispoParMembre = indispos
            .GroupBy(i => i.MembreId)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(i => EnumerateDates(
                    DateOnly.FromDateTime(i.DateDebut.Date < debutPeriode ? debutPeriode : i.DateDebut.Date),
                    DateOnly.FromDateTime(i.DateFin.HasValue
                        ? (i.DateFin.Value.Date > finPeriode ? finPeriode : i.DateFin.Value.Date)
                        : finPeriode)
                )).ToHashSet());

        // 4. Résoudre le programme jour par jour
        var blocs = await db.BlocsVol.AsNoTracking()
            .Include(b => b.TypeAvion)
            .ToListAsync(cancellationToken);
        var vols = await db.Vols.AsNoTracking().ToListAsync(cancellationToken);

        // Hydrater Vols à partir des Etapes (JSONB) — même pattern que CatalogueResolver
        var volDict = vols.ToDictionary(v => v.Id);
        foreach (var bloc in blocs)
        {
            foreach (var etape in bloc.Etapes.OrderBy(e => e.Position))
            {
                if (volDict.TryGetValue(etape.VolId, out var vol))
                    bloc.Vols.Add(vol);
            }
        }
        var semaines = await db.SemainesTypes.AsNoTracking().ToListAsync(cancellationToken);
        var blocDict = blocs.ToDictionary(b => b.Id);
        var stDict = semaines.ToDictionary(st => st.Id);

        var dailyPrograms = ResolveDailyPrograms(
            scenario.Periode.DateDebut,
            scenario.Periode.DateFin,
            scenario.Calendrier,
            stDict,
            blocDict);

        // 5. Lire les réglages solver depuis AppSettings
        var solverSettings = await db.AppSettings
            .AsNoTracking()
            .Where(s => s.Key.StartsWith("solver."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        var deterministic = string.Equals(
            solverSettings.GetValueOrDefault("solver.deterministic", "false"),
            "true", StringComparison.OrdinalIgnoreCase);

        var timeoutSeconds = int.TryParse(
            solverSettings.GetValueOrDefault("solver.timeout", "30"), out var t) && t > 0 ? t : 30;

        var numWorkers = int.TryParse(
            solverSettings.GetValueOrDefault("solver.workers", "0"), out var w) && w > 0 ? w : Environment.ProcessorCount;

        // 6. Construire les règles FTL
        var ftlRules = MapFtlRules(scenario);

        // 7. Appeler le solver pour PNT
        var pntResult = await SolveForCategory(
            CrewCategory.PNT, TypeContrat.PNT, membres, indispoParMembre,
            scenario, dailyPrograms, ftlRules, deterministic, timeoutSeconds, numWorkers,
            request.SolveId, cancellationToken);

        // 8. Appeler le solver pour PNC
        var pncResult = await SolveForCategory(
            CrewCategory.PNC, TypeContrat.PNC, membres, indispoParMembre,
            scenario, dailyPrograms, ftlRules, deterministic, timeoutSeconds, numWorkers,
            request.SolveId, cancellationToken);

        return new CombinedSizingResult
        {
            PntResult = pntResult,
            PncResult = pncResult
        };
    }

    private async Task<SizingResult> SolveForCategory(
        CrewCategory category,
        TypeContrat contrat,
        List<MembreEquipage> allMembres,
        Dictionary<Guid, HashSet<DateOnly>> indispoParMembre,
        ConfigurationScenario scenario,
        List<DailyProgram> dailyPrograms,
        FtlRules ftlRules,
        bool deterministic,
        int timeoutSeconds,
        int numWorkers,
        string? solveId,
        CancellationToken cancellationToken)
    {
        var membresCategorie = allMembres.Where(m => m.Contrat == contrat).ToList();

        var crewInfos = membresCategorie.Select(m => new CrewMemberInfo
        {
            Id = m.Id,
            Trigram = m.Code,
            Category = category,
            Rank = MapGradeToRank(m.Grade),
            IsExaminer = false,
            IsRdov = m.Roles.Contains("RDOV"),
            UnavailableDates = indispoParMembre.GetValueOrDefault(m.Id, new HashSet<DateOnly>())
        }).ToList();

        var sizingRequest = new SizingRequest
        {
            StartDate = scenario.Periode.DateDebut,
            EndDate = scenario.Periode.DateFin,
            Category = category,
            DailyPrograms = dailyPrograms,
            AvailableCrew = crewInfos,
            FtlRules = ftlRules,
            Deterministic = deterministic,
            TimeoutSeconds = timeoutSeconds,
            NumWorkers = numWorkers,
            SolveId = solveId
        };

        return await solver.SolveAsync(sizingRequest, cancellationToken);
    }

    /// <summary>
    /// Résout le programme jour par jour : calendrier → semaine type → blocs du jour.
    /// </summary>
    private static List<DailyProgram> ResolveDailyPrograms(
        DateOnly startDate,
        DateOnly endDate,
        List<AffectationSemaine> calendrier,
        Dictionary<Guid, SemaineType> stDict,
        Dictionary<Guid, BlocVol> blocDict)
    {
        var result = new List<DailyProgram>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dt = date.ToDateTime(TimeOnly.MinValue);
            var isoWeek = ISOWeek.GetWeekOfYear(dt);
            var isoYear = ISOWeek.GetYear(dt);

            // Jour de la semaine ISO (Lundi=1..Dimanche=7)
            var dayOfWeek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;
            var jourNom = HeureHelper.IndexVersJour(dayOfWeek);

            // Trouver l'affectation calendrier
            var affectation = calendrier.FirstOrDefault(a => a.Semaine == isoWeek && a.Annee == isoYear);
            if (affectation == null)
            {
                result.Add(new DailyProgram { Date = date, Blocks = [] });
                continue;
            }

            // Trouver la semaine type
            if (!stDict.TryGetValue(affectation.SemaineTypeId, out var st))
            {
                result.Add(new DailyProgram { Date = date, Blocks = [] });
                continue;
            }

            // Trouver les placements pour ce jour de semaine
            var dayBlocks = st.Placements
                .Where(p => string.Equals(p.Jour, jourNom, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Sequence)
                .Select(p =>
                {
                    if (!blocDict.TryGetValue(p.BlocId, out var bloc)) return null;
                    return MapBlocToBlockInfo(bloc);
                })
                .Where(b => b != null)
                .Cast<BlockInfo>()
                .ToList();

            result.Add(new DailyProgram { Date = date, Blocks = dayBlocks });
        }

        return result;
    }

    private static BlockInfo MapBlocToBlockInfo(BlocVol bloc)
    {
        var dpStart = HeureHelper.ParseHeure(bloc.DebutDP);
        var dpEnd = HeureHelper.ParseHeure(bloc.FinDP);
        var fdpStart = HeureHelper.ParseHeure(bloc.DebutFDP);
        var fdpEnd = HeureHelper.ParseHeure(bloc.FinFDP);

        return new BlockInfo
        {
            Id = bloc.Id,
            Code = bloc.Code,
            SectorCount = bloc.Etapes.Count,
            BlockTimeMinutes = (int)(bloc.Vols.Sum(v => v.HdvVol) * 60),
            DpStartMinutes = (int)dpStart.TotalMinutes,
            DpEndMinutes = (int)dpEnd.TotalMinutes,
            FdpStartMinutes = (int)fdpStart.TotalMinutes,
            FdpEndMinutes = (int)fdpEnd.TotalMinutes,
            RequiredCdb = bloc.TypeAvion?.NbCdb ?? 1,
            RequiredOpl = bloc.TypeAvion?.NbOpl ?? 1,
            RequiredCc = bloc.TypeAvion?.NbCc ?? 1,
            RequiredPnc = bloc.TypeAvion?.NbPnc ?? 0
        };
    }

    private static FtlRules MapFtlRules(ConfigurationScenario scenario)
    {
        // Baseline : valeurs issues du registre de contraintes (fusion multi-sources)
        var baseline = FtlRules.FromRegistry();

        // Surcharges scénario : prendre la valeur la plus restrictive entre registre et scénario
        return new FtlRules
        {
            MaxDuty7dMinutes = Math.Min(baseline.MaxDuty7dMinutes, (int)(scenario.LimitesTempsService.Max7j * 60)),
            MaxDuty14dMinutes = Math.Min(baseline.MaxDuty14dMinutes, (int)(scenario.LimitesTempsService.Max14j * 60)),
            MaxDuty28dMinutes = Math.Min(baseline.MaxDuty28dMinutes, (int)(scenario.LimitesTempsService.Max28j * 60)),
            MaxHdv28dMinutes = Math.Min(baseline.MaxHdv28dMinutes, (int)(scenario.LimitesCumulatives.H28Max * 60)),
            MinDaysOffPerMonth = Math.Max(baseline.MinDaysOffPerMonth, scenario.JoursOff.Reglementaire),
            MinRestMinutes = Math.Max(baseline.MinRestMinutes, (int)(scenario.LimitesFTL.ReposMinimum * 60)),
            MaxConsecutiveWorkDays = baseline.MaxConsecutiveWorkDays,
            MinRestDaysPerPeriod = baseline.MinRestDaysPerPeriod,
            WeeklyRestNights = baseline.WeeklyRestNights,
            MonthlyWeekendRestDays = baseline.MonthlyWeekendRestDays,
            ActiveConstraints = baseline.ActiveConstraints,
        };
    }

    private static CrewRank MapGradeToRank(Grade grade) => grade switch
    {
        Grade.CDB => CrewRank.CDB,
        Grade.OPL => CrewRank.OPL,
        Grade.CC => CrewRank.CC,
        Grade.PNC => CrewRank.PNC,
        _ => CrewRank.PNC
    };

    private static IEnumerable<DateOnly> EnumerateDates(DateOnly start, DateOnly end)
    {
        for (var d = start; d <= end; d = d.AddDays(1))
            yield return d;
    }
}
