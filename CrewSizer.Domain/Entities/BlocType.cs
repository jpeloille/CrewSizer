namespace CrewSizer.Domain.Entities;

public class BlocType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Libelle { get; set; } = "";
    public string DebutPlage { get; set; } = "";   // HH:mm
    public string FinPlage { get; set; } = "";      // HH:mm
    public double FdpMax { get; set; }
    public bool HauteSaison { get; set; }
}
