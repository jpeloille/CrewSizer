using CrewSizer.Domain.Services;

namespace CrewSizer.Domain.Entities;

public class BlocVol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public int Sequence { get; set; }
    public string Jour { get; set; } = "";
    public string Periode { get; set; } = "";
    public string DebutDP { get; set; } = "";
    public string FinDP { get; set; } = "";
    public string DebutFDP { get; set; } = "";
    public string FinFDP { get; set; } = "";
    public Guid? BlocTypeId { get; set; }
    public BlocType? BlocType { get; set; }
    public Guid TypeAvionId { get; set; }
    public TypeAvion? TypeAvion { get; set; }
    public List<EtapeVol> Etapes { get; set; } = [];
    public List<Vol> Vols { get; set; } = [];

    // ── Propriétés calculées ──

    public string Nom => DeriveNom();

    public int JourIndex => JourVersIndex(Jour);

    public int NbEtapes => Vols.Count;

    public double HdvBloc => Vols.Sum(v => v.HdvVol);

    public double DureeDPHeures => HeureHelper.CalculerDuree(DebutDP, FinDP);

    public double DureeFDPHeures => HeureHelper.CalculerDuree(DebutFDP, FinFDP);

    public double DureeTSHeures => DureeDPHeures;

    public double DureeTSVHeures => DureeFDPHeures;

    public string JourNom => Jour;

    private string DeriveNom()
    {
        if (Vols.Count == 0) return "";
        return Vols[0].Depart + string.Concat(Vols.Select(v => $"-{v.Arrivee}"));
    }

    public static int JourVersIndex(string jour) => HeureHelper.JourVersIndex(jour);

    public static string IndexVersJour(int idx) => HeureHelper.IndexVersJour(idx);

    public static TimeSpan ParseHeure(string hhmm) => HeureHelper.ParseHeure(hhmm);

    public static double CalculerDuree(string debut, string fin) => HeureHelper.CalculerDuree(debut, fin);
}
