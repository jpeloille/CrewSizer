// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

namespace CrewSizer.Domain.Sizing;

/// <summary>
/// Registre central des contraintes FTL par source réglementaire.
/// Les dictionnaires sont statiques (les définitions ne changent pas à l'exécution).
/// <see cref="MergeToFtlRules"/> compose les valeurs les plus restrictives.
/// </summary>
public static class ConstraintRegistry
{
    // ── Dictionnaire ORO.FTL (EASA / EU-OPS Subpart Q) ──────

    public static IReadOnlyDictionary<string, ConstraintDefinition> OroFtl { get; } =
        new Dictionary<string, ConstraintDefinition>
        {
            ["C5"] = new()
            {
                Code = "C5",
                Name = "TSV 7j ≤ 60h",
                Description = "Temps de service cumulé sur fenêtre glissante de 7 jours ≤ 60 heures.",
                Source = ConstraintSource.OroFtl,
                FtlRulesProperty = nameof(FtlRules.MaxDuty7dMinutes),
                DefaultValue = 60 * 60, // 3 600 minutes
                Unit = "minutes",
                LegalReference = "ORO.FTL.210(b)",
                LowerIsMoreRestrictive = true,
            },
            ["C6"] = new()
            {
                Code = "C6",
                Name = "TSV 14j ≤ 110h",
                Description = "Temps de service cumulé sur fenêtre glissante de 14 jours ≤ 110 heures.",
                Source = ConstraintSource.OroFtl,
                FtlRulesProperty = nameof(FtlRules.MaxDuty14dMinutes),
                DefaultValue = 110 * 60, // 6 600 minutes
                Unit = "minutes",
                LegalReference = "ORO.FTL.210(b)",
                LowerIsMoreRestrictive = true,
            },
            ["C7"] = new()
            {
                Code = "C7",
                Name = "TSV 28j ≤ 190h",
                Description = "Temps de service cumulé sur fenêtre glissante de 28 jours ≤ 190 heures.",
                Source = ConstraintSource.OroFtl,
                FtlRulesProperty = nameof(FtlRules.MaxDuty28dMinutes),
                DefaultValue = 190 * 60, // 11 400 minutes
                Unit = "minutes",
                LegalReference = "ORO.FTL.210(b)",
                LowerIsMoreRestrictive = true,
            },
            ["C8"] = new()
            {
                Code = "C8",
                Name = "HDV 28j ≤ 100h",
                Description = "Heures de vol cumulées sur 28 jours ≤ 100 heures.",
                Source = ConstraintSource.OroFtl,
                FtlRulesProperty = nameof(FtlRules.MaxHdv28dMinutes),
                DefaultValue = 100 * 60, // 6 000 minutes
                Unit = "minutes",
                LegalReference = "ORO.FTL.210(a)",
                LowerIsMoreRestrictive = true,
            },
            ["C10"] = new()
            {
                Code = "C10",
                Name = "Repos min 12h",
                Description = "Repos minimum de 12 heures entre deux services de vol consécutifs.",
                Source = ConstraintSource.OroFtl,
                FtlRulesProperty = nameof(FtlRules.MinRestMinutes),
                DefaultValue = 12 * 60, // 720 minutes
                Unit = "minutes",
                LegalReference = "ORO.FTL.235(a)",
                LowerIsMoreRestrictive = false,
            },
        };

    // ── Dictionnaire Convention compagnie (Air Calédonie / TPC) ──

    public static IReadOnlyDictionary<string, ConstraintDefinition> Convention { get; } =
        new Dictionary<string, ConstraintDefinition>
        {
            ["C11b"] = new()
            {
                Code = "C11b",
                Name = "Min 2j OFF consécutifs",
                Description = "Repos périodique d'au moins 2 jours consécutifs. Le pattern ON-OFF-ON est interdit.",
                Source = ConstraintSource.ConventionCompagnie,
                FtlRulesProperty = nameof(FtlRules.MinRestDaysPerPeriod),
                DefaultValue = 2,
                Unit = "jours",
                LegalReference = "Convention collective TPC",
                LowerIsMoreRestrictive = false,
            },
            ["C13"] = new()
            {
                Code = "C13",
                Name = "Repos WE mensuel 3j",
                Description = "Au moins 1 repos de 3 jours consécutifs incluant samedi + dimanche par mois calendaire.",
                Source = ConstraintSource.ConventionCompagnie,
                FtlRulesProperty = nameof(FtlRules.MonthlyWeekendRestDays),
                DefaultValue = 3,
                Unit = "jours",
                LegalReference = "Convention collective TPC",
                LowerIsMoreRestrictive = false,
            },
            ["C14"] = new()
            {
                Code = "C14",
                Name = "RDOV 2,5j bureau/sem.",
                Description = "Navigant RDOV : repos samedi+dimanche, max 2,5 jours de vol par semaine civile (3+2 ou 2+3).",
                Source = ConstraintSource.ConventionCompagnie,
                LegalReference = "Convention collective TPC — fonction RDOV",
            },
        };

    // ── Dictionnaire Délibération n°77 (Nouvelle-Calédonie) ──

    public static IReadOnlyDictionary<string, ConstraintDefinition> Deliberation77 { get; } =
        new Dictionary<string, ConstraintDefinition>
        {
            ["C9"] = new()
            {
                Code = "C9",
                Name = "8 jours OFF/mois",
                Description = "Minimum 8 jours OFF par période glissante de 28 jours.",
                Source = ConstraintSource.Deliberation77,
                FtlRulesProperty = nameof(FtlRules.MinDaysOffPerMonth),
                DefaultValue = 8,
                Unit = "jours",
                LegalReference = "Délibération n°77",
                LowerIsMoreRestrictive = false,
            },
            ["C11a"] = new()
            {
                Code = "C11a",
                Name = "Max 6j consécutifs",
                Description = "Pas plus de 6 jours de travail consécutifs avant un repos obligatoire.",
                Source = ConstraintSource.Deliberation77,
                FtlRulesProperty = nameof(FtlRules.MaxConsecutiveWorkDays),
                DefaultValue = 6,
                Unit = "jours",
                LegalReference = "Délibération n°77",
                LowerIsMoreRestrictive = true,
            },
            ["C12"] = new()
            {
                Code = "C12",
                Name = "Repos hebdo 2 nuitées",
                Description = "Au moins 2 jours consécutifs OFF (couvrant 2 nuitées) par semaine civile (lundi→dimanche).",
                Source = ConstraintSource.Deliberation77,
                FtlRulesProperty = nameof(FtlRules.WeeklyRestNights),
                DefaultValue = 2,
                Unit = "nuitées",
                LegalReference = "Délibération n°77",
                LowerIsMoreRestrictive = false,
            },
        };

    // ── Dictionnaire Structural (contraintes du modèle) ──────

    public static IReadOnlyDictionary<string, ConstraintDefinition> Structural { get; } =
        new Dictionary<string, ConstraintDefinition>
        {
            ["C1"] = new()
            {
                Code = "C1",
                Name = "Indisponibilités",
                Description = "Un navigant indisponible (congé, formation, maladie) ne peut être affecté.",
                Source = ConstraintSource.Structural,
            },
            ["C2"] = new()
            {
                Code = "C2",
                Name = "Max 1 bloc/jour",
                Description = "Chaque navigant est affecté à au plus 1 bloc de vols par jour calendaire.",
                Source = ConstraintSource.Structural,
            },
            ["C3"] = new()
            {
                Code = "C3",
                Name = "Lien isUsed ↔ works",
                Description = "Variable technique : isUsed[c] = 1 ssi le navigant c travaille au moins 1 jour sur l'horizon.",
                Source = ConstraintSource.Structural,
            },
            ["C4"] = new()
            {
                Code = "C4",
                Name = "Couverture par rang",
                Description = "Chaque bloc doit avoir le nombre requis de CDB, OPL, CC et PNC (avec polyvalence CC→PNC).",
                Source = ConstraintSource.Structural,
            },
        };

    // ── Toutes les contraintes (union des 4 dictionnaires) ───

    public static IReadOnlyDictionary<string, ConstraintDefinition> All { get; } =
        BuildAll();

    /// <summary>Contraintes groupées par source réglementaire.</summary>
    public static IReadOnlyDictionary<ConstraintSource, IReadOnlyList<ConstraintDefinition>> BySource()
    {
        return All.Values
            .GroupBy(c => c.Source)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ConstraintDefinition>)g.OrderBy(c => c.Code).ToList());
    }

    /// <summary>
    /// Compose un <see cref="FtlRules"/> en prenant la valeur la plus restrictive
    /// pour chaque propriété parmi toutes les sources.
    /// Les <paramref name="overrides"/> permettent de surcharger des valeurs
    /// (ex : valeurs saisies par l'utilisateur dans un scénario).
    /// </summary>
    public static FtlRules MergeToFtlRules(IEnumerable<ConstraintDefinition>? overrides = null)
    {
        // Partir des valeurs par défaut de chaque dictionnaire
        var all = All.Values
            .Where(c => c.FtlRulesProperty != null && c.DefaultValue.HasValue)
            .ToList();

        if (overrides != null)
            all.AddRange(overrides.Where(c => c.FtlRulesProperty != null && c.DefaultValue.HasValue));

        int Resolve(string propertyName, int fallback)
        {
            var matching = all.Where(c => c.FtlRulesProperty == propertyName).ToList();
            if (matching.Count == 0) return fallback;

            // Prendre la plus restrictive
            var first = matching[0];
            if (first.LowerIsMoreRestrictive == true)
                return matching.Min(c => c.DefaultValue!.Value);
            if (first.LowerIsMoreRestrictive == false)
                return matching.Max(c => c.DefaultValue!.Value);
            return matching[0].DefaultValue!.Value;
        }

        return new FtlRules
        {
            MaxDuty7dMinutes = Resolve(nameof(FtlRules.MaxDuty7dMinutes), 60 * 60),
            MaxDuty14dMinutes = Resolve(nameof(FtlRules.MaxDuty14dMinutes), 110 * 60),
            MaxDuty28dMinutes = Resolve(nameof(FtlRules.MaxDuty28dMinutes), 190 * 60),
            MaxHdv28dMinutes = Resolve(nameof(FtlRules.MaxHdv28dMinutes), 100 * 60),
            MinDaysOffPerMonth = Resolve(nameof(FtlRules.MinDaysOffPerMonth), 8),
            MinRestMinutes = Resolve(nameof(FtlRules.MinRestMinutes), 12 * 60),
            MaxConsecutiveWorkDays = Resolve(nameof(FtlRules.MaxConsecutiveWorkDays), 6),
            MinRestDaysPerPeriod = Resolve(nameof(FtlRules.MinRestDaysPerPeriod), 2),
            WeeklyRestNights = Resolve(nameof(FtlRules.WeeklyRestNights), 2),
            MonthlyWeekendRestDays = Resolve(nameof(FtlRules.MonthlyWeekendRestDays), 3),
            ActiveConstraints = all.DistinctBy(c => c.Code).OrderBy(c => c.Code).ToList(),
        };
    }

    // ──────────────────────────────────────────────────────────

    private static Dictionary<string, ConstraintDefinition> BuildAll()
    {
        var result = new Dictionary<string, ConstraintDefinition>();
        foreach (var kvp in Structural) result[kvp.Key] = kvp.Value;
        foreach (var kvp in OroFtl) result[kvp.Key] = kvp.Value;
        foreach (var kvp in Convention) result[kvp.Key] = kvp.Value;
        foreach (var kvp in Deliberation77) result[kvp.Key] = kvp.Value;
        return result;
    }
}
