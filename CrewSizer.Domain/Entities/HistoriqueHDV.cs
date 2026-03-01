namespace CrewSizer.Domain.Entities;

/// <summary>Historique des heures de vol individuelles d'un membre</summary>
public class HistoriqueHDV
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MembreId { get; set; }
    public string MembreCode { get; set; } = "";
    public DateTime DateReleve { get; set; }
    public double Cumul28j { get; set; }
    public double Cumul90j { get; set; }
    public double Cumul12m { get; set; }

    public double Marge28j(double limite = 100) => limite - Cumul28j;
    public double Marge90j(double limite = 280) => limite - Cumul90j;
    public double Marge12m(double limite = 900) => limite - Cumul12m;

    /// <summary>Heures volables max avant la prochaine butée</summary>
    public double HeuresDisponibles(double l28 = 100, double l90 = 280, double l12 = 900)
        => Math.Min(Marge28j(l28), Math.Min(Marge90j(l90), Marge12m(l12)));

    /// <summary>Contrainte mordante individuelle</summary>
    public string ContrainteMordante(double l28 = 100, double l90 = 280, double l12 = 900)
    {
        var m28 = Marge28j(l28);
        var m90 = Marge90j(l90);
        var m12 = Marge12m(l12);
        var min = Math.Min(m28, Math.Min(m90, m12));
        if (min == m28) return "28 jours";
        if (min == m90) return "90 jours";
        return "12 mois";
    }

    public (double cumul, double limite, bool ok) Verif28j(double hdvSupp, double l = 100)
        => (Cumul28j + hdvSupp, l, Cumul28j + hdvSupp <= l);
    public (double cumul, double limite, bool ok) Verif90j(double hdvSupp, double l = 280)
        => (Cumul90j + hdvSupp, l, Cumul90j + hdvSupp <= l);
    public (double cumul, double limite, bool ok) Verif12m(double hdvSupp, double l = 900)
        => (Cumul12m + hdvSupp, l, Cumul12m + hdvSupp <= l);
}
