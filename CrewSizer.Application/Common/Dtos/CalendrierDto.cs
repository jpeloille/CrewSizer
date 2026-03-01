namespace CrewSizer.Application.Common.Dtos;

public record CalendrierDto
{
    public Guid ScenarioId { get; init; }
    public List<AffectationSemaineDto> Affectations { get; init; } = [];
}
