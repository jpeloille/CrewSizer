using CrewSizer.Domain.Services;

namespace CrewSizer.Domain.Entities;

public class Vol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Numero { get; set; } = "";
    public string Depart { get; set; } = "";
    public string Arrivee { get; set; } = "";
    public string HeureDepart { get; set; } = "";
    public string HeureArrivee { get; set; } = "";
    public bool MH { get; set; }

    public double HdvVol => HeureHelper.CalculerDuree(HeureDepart, HeureArrivee);
}
