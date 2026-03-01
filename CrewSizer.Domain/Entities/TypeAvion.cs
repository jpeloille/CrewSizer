namespace CrewSizer.Domain.Entities;

public class TypeAvion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Libelle { get; set; } = "";
    public int NbCdb { get; set; } = 1;
    public int NbOpl { get; set; } = 1;
    public int NbCc { get; set; } = 1;
    public int NbPnc { get; set; } = 0;
}
