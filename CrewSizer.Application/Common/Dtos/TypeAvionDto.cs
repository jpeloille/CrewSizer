namespace CrewSizer.Application.Common.Dtos;

public record TypeAvionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Libelle { get; init; } = "";
    public int NbCdb { get; init; }
    public int NbOpl { get; init; }
    public int NbCc { get; init; }
    public int NbPnc { get; init; }
}
