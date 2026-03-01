namespace CrewSizer.Application.Common.Dtos;

public record CalculSnapshotDto
{
    public Guid Id { get; init; }
    public Guid ScenarioId { get; init; }
    public DateTime DateCalcul { get; init; }
    public string? CalculePar { get; init; }
    public double TauxEngagementGlobal { get; init; }
    public string StatutGlobal { get; init; } = "";
    public string CategorieContraignante { get; init; } = "";
    public int TotalBlocs { get; init; }
    public double TotalHDV { get; init; }
    public int Rotations { get; init; }
    public string ResultatJson { get; init; } = "";
}

public record CalculSnapshotListItemDto
{
    public Guid Id { get; init; }
    public Guid ScenarioId { get; init; }
    public DateTime DateCalcul { get; init; }
    public string? CalculePar { get; init; }
    public double TauxEngagementGlobal { get; init; }
    public string StatutGlobal { get; init; } = "";
    public string CategorieContraignante { get; init; } = "";
}
