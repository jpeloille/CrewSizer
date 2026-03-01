using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Entities;

/// <summary>Affectation d'un membre d'équipage à une activité sur une date</summary>
public class AffectationEquipage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MembreId { get; set; }
    public DateTime Date { get; set; }
    public TypeActivite Activite { get; set; } = TypeActivite.Vol;
    public Guid BlocId { get; set; }
    public string BlocCode { get; set; } = "";
    public string Commentaire { get; set; } = "";
    public double HeuresVol { get; set; }
    public double TempsService { get; set; }

    public bool EstProductif => Activite is TypeActivite.Vol or TypeActivite.Reserve;
    public bool EstIndisponible => Activite is TypeActivite.Repos or TypeActivite.Conge;
}
