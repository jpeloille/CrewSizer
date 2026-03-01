using CrewSizer.Domain.Enums;

namespace CrewSizer.Domain.Entities;

/// <summary>Compétence composite regroupant plusieurs checks requis</summary>
public class Competence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Libelle { get; set; } = "";
    public TypeCompetence Type { get; set; } = TypeCompetence.Qualification;
    public GroupeCheck Groupe { get; set; }
    public List<string> ChecksRequis { get; set; } = [];

    /// <summary>Vérifie si un membre possède cette compétence (tous les checks requis non expirés)</summary>
    public bool EstValide(MembreEquipage membre)
    {
        if (ChecksRequis.Count == 0) return true;
        return ChecksRequis.All(code => membre.Qualifications
            .Any(q => q.CodeCheck.Equals(code, StringComparison.OrdinalIgnoreCase)
                      && q.Statut != StatutCheck.Expire));
    }

    /// <summary>Statut agrégé : pire statut parmi les checks requis</summary>
    public StatutCheck StatutPour(MembreEquipage membre)
    {
        if (ChecksRequis.Count == 0) return StatutCheck.NonApplicable;

        var pire = StatutCheck.Valide;
        foreach (var code in ChecksRequis)
        {
            var qualif = membre.Qualifications
                .FirstOrDefault(q => q.CodeCheck.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (qualif == null) return StatutCheck.Expire;
            if (qualif.Statut > pire) pire = qualif.Statut;
        }
        return pire;
    }

    /// <summary>Prochaine date d'expiration parmi les checks requis</summary>
    public DateTime? ProchaineExpiration(MembreEquipage membre)
    {
        DateTime? min = null;
        foreach (var code in ChecksRequis)
        {
            var qualif = membre.Qualifications
                .FirstOrDefault(q => q.CodeCheck.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (qualif?.DateExpiration != null && (min == null || qualif.DateExpiration < min))
                min = qualif.DateExpiration;
        }
        return min;
    }
}
