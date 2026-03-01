namespace CrewSizer.Domain.Entities;

public class AffectationSemaine
{
    public int Semaine { get; set; }
    public int Annee { get; set; }
    public Guid SemaineTypeId { get; set; }
    public string SemaineTypeRef { get; set; } = "";
}
