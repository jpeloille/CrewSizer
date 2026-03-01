namespace CrewSizer.Application.Common.Dtos;

public record VolDto
{
    public Guid Id { get; init; }
    public string Numero { get; init; } = "";
    public string Depart { get; init; } = "";
    public string Arrivee { get; init; } = "";
    public string HeureDepart { get; init; } = "";
    public string HeureArrivee { get; init; } = "";
    public bool MH { get; init; }
    public double HdvVol { get; init; }
}
