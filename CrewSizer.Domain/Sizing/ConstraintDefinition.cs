// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

namespace CrewSizer.Domain.Sizing;

/// <summary>
/// Définition d'une contrainte solver avec ses métadonnées réglementaires.
/// Immutable par conception (record).
/// </summary>
public sealed record ConstraintDefinition
{
    /// <summary>Code unique (C1, C2, ... C13).</summary>
    public required string Code { get; init; }

    /// <summary>Nom court lisible.</summary>
    public required string Name { get; init; }

    /// <summary>Description longue (texte affichable UI).</summary>
    public required string Description { get; init; }

    /// <summary>Source réglementaire.</summary>
    public required ConstraintSource Source { get; init; }

    /// <summary>
    /// Nom de la propriété FtlRules correspondante (null pour contraintes structurelles).
    /// Ex : "MaxDuty7dMinutes", "MinDaysOffPerMonth".
    /// </summary>
    public string? FtlRulesProperty { get; init; }

    /// <summary>Valeur par défaut (en unité de base : minutes ou jours).</summary>
    public int? DefaultValue { get; init; }

    /// <summary>Unité de la valeur ("minutes", "jours", "nuitées").</summary>
    public string? Unit { get; init; }

    /// <summary>Référence juridique exacte (ex : "ORO.FTL.210(b)(1)").</summary>
    public string? LegalReference { get; init; }

    /// <summary>
    /// Sens de la restrictivité pour la fusion multi-sources :
    /// true  = la valeur la plus basse est la plus restrictive (ex : max duty, max consécutifs).
    /// false = la valeur la plus haute est la plus restrictive (ex : min repos, min jours OFF).
    /// null  = pas de fusion (contrainte structurelle ou booléen).
    /// </summary>
    public bool? LowerIsMoreRestrictive { get; init; }
}
