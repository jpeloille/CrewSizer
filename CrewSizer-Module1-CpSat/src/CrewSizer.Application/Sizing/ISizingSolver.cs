// CrewSizer – Airline Crew Sizing & Rostering Software
// Copyright © 2026 Julien PELOILLE – Tous droits réservés

namespace CrewSizer.Application.Sizing;

/// <summary>
/// Interface du solver de dimensionnement (Module 1).
/// Implémenté par OrToolsSizingSolver dans l'Infrastructure.
/// </summary>
public interface ISizingSolver
{
    /// <summary>
    /// Vérifie que le programme de vols est couvrable avec l'effectif donné
    /// et calcule l'effectif minimum requis par rang.
    /// </summary>
    /// <param name="request">Données d'entrée (programme résolu, équipage, règles FTL).</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    /// <returns>Résultat du dimensionnement.</returns>
    Task<SizingResult> SolveAsync(SizingRequest request, CancellationToken cancellationToken = default);
}
