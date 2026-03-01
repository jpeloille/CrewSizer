namespace CrewSizer.Application.Common.Dtos;

public record BlocTypeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Libelle { get; init; } = "";
    public string DebutPlage { get; init; } = "";
    public string FinPlage { get; init; } = "";
    public double FdpMax { get; init; }
    public bool HauteSaison { get; init; }
}
