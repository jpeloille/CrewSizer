namespace CrewSizer.Application.Common.Dtos;

public record SemaineTypeDto
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = "";
    public string Saison { get; init; } = "";
    public List<BlocPlacementDto> Placements { get; init; } = [];
}

public record BlocPlacementDto(Guid BlocId, string Jour, int Sequence);
