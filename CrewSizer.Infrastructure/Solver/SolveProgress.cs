using CrewSizer.Domain.Sizing;

namespace CrewSizer.Infrastructure.Solver;

public record SolveProgress
{
    public required string SolveId { get; init; }

    /// <summary>"running", "completed", "error"</summary>
    public required string Status { get; init; }

    public int SolutionsFound { get; init; }

    /// <summary>Meilleur objectif courant (nb min de navigants).</summary>
    public int BestObjective { get; init; }

    public double ElapsedSeconds { get; init; }

    /// <summary>Secondes depuis la dernière amélioration de l'objectif.</summary>
    public double? SecondsSinceLastImprovement { get; init; }

    /// <summary>Catégorie en cours de résolution ("PNT" ou "PNC").</summary>
    public string? CurrentCategory { get; init; }

    /// <summary>Résultat final (rempli uniquement quand Status == "completed").</summary>
    public CombinedSizingResult? Result { get; init; }

    public string? ErrorMessage { get; init; }
}
