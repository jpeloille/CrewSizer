using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Entities;

/// <summary>Période d'indisponibilité d'un membre d'équipage</summary>
public class DisponibiliteMembre
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MembreId { get; set; }
    public string MembreCode { get; set; } = "";
    public MotifIndisponibilite Motif { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string Commentaire { get; set; } = "";

    public int NbJours => DateFin.HasValue ? (DateFin.Value.Date - DateDebut.Date).Days + 1 : 0;

    public bool CouvreDate(DateTime date) =>
        date.Date >= DateDebut.Date && (!DateFin.HasValue || date.Date <= DateFin.Value.Date);

    /// <summary>Nombre de jours chevauchant une période donnée</summary>
    public int JoursChevauche(DateTime debutPeriode, DateTime finPeriode)
    {
        var debut = DateDebut.Date < debutPeriode.Date ? debutPeriode.Date : DateDebut.Date;
        var fin = DateFin.HasValue
            ? (DateFin.Value.Date > finPeriode.Date ? finPeriode.Date : DateFin.Value.Date)
            : finPeriode.Date;
        return debut > fin ? 0 : (fin - debut).Days + 1;
    }

    public bool EstActive => !DateFin.HasValue || DateFin.Value.Date >= DateTime.Today;

    public string MotifLibelle => Motif switch
    {
        MotifIndisponibilite.CongeAnnuel => "Conge annuel",
        MotifIndisponibilite.CongeMaladie => "Maladie",
        MotifIndisponibilite.Maternite => "Maternite",
        MotifIndisponibilite.Formation => "Formation",
        MotifIndisponibilite.Detachement => "Detachement",
        MotifIndisponibilite.SansSolde => "Sans solde",
        MotifIndisponibilite.Suspension => "Suspension",
        _ => "Autre"
    };
}
