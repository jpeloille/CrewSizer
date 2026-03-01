// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

namespace CrewSizer.Domain.Sizing;

/// <summary>Source réglementaire d'une contrainte du solver.</summary>
public enum ConstraintSource
{
    /// <summary>Contrainte structurelle du modèle (pas de source juridique).</summary>
    Structural,

    /// <summary>EASA ORO.FTL / EU-OPS Subpart Q (arrêté 28 juin 2011).</summary>
    OroFtl,

    /// <summary>Convention d'entreprise Air Calédonie (TPC).</summary>
    ConventionCompagnie,

    /// <summary>Délibération n°77 — réglementation locale Nouvelle-Calédonie.</summary>
    Deliberation77
}
