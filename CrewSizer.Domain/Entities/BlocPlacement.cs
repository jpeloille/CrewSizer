namespace CrewSizer.Domain.Entities;

public class BlocPlacement
{
    public Guid BlocId { get; set; }
    public string Jour { get; set; } = "";
    public int Sequence { get; set; }
}
