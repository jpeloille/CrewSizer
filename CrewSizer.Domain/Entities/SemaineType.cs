namespace CrewSizer.Domain.Entities;

public class SemaineType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = "";
    public string Saison { get; set; } = "";
    public List<BlocPlacement> Placements { get; set; } = [];
    public List<BlocVol> Blocs { get; set; } = [];
}
