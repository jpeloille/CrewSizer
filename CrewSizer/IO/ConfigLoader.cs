using CrewSizer.Models;

namespace CrewSizer.IO;

public static class ConfigLoader
{
    public static void Valider(Configuration config)
    {
        if (config.Periode.NbJours <= 0)
            throw new ArgumentException("Le nombre de jours du mois doit être > 0");

        if (config.Effectif.Cdb < 0 || config.Effectif.Opl < 0)
            throw new ArgumentException("L'effectif PNT ne peut pas être négatif");

        if (config.Effectif.Cc < 0 || config.Effectif.Pnc < 0)
            throw new ArgumentException("L'effectif PNC ne peut pas être négatif");

        if (config.Effectif.Cdb + config.Effectif.Opl == 0)
            throw new ArgumentException("L'effectif PNT total doit être > 0");

        if (config.LimitesFTL.TsvMoyenRetenu <= 0)
            throw new ArgumentException("Le TSV moyen doit être > 0");

        if (config.LimitesFTL.ReposMinimum <= 0)
            throw new ArgumentException("Le repos minimum doit être > 0");

        // Validation des semaines types
        if (config.SemainesTypes.Count == 0)
            throw new ArgumentException("Au moins une semaine type est requise");

        var refsConnues = new HashSet<string>();
        foreach (var st in config.SemainesTypes)
        {
            if (string.IsNullOrWhiteSpace(st.Reference))
                throw new ArgumentException("Semaine type : référence vide");

            if (!refsConnues.Add(st.Reference))
                throw new ArgumentException($"Semaine type en double : '{st.Reference}'");

            if (st.Blocs.Count == 0)
                throw new ArgumentException($"Semaine type '{st.Reference}' : au moins un bloc requis");

            foreach (var bloc in st.Blocs)
            {
                if (BlocVol.JourVersIndex(bloc.Jour) == 0)
                    throw new ArgumentException($"Semaine type '{st.Reference}' bloc seq {bloc.Sequence}: jour invalide '{bloc.Jour}'");

                if (bloc.Vols.Count == 0)
                    throw new ArgumentException($"Semaine type '{st.Reference}' bloc seq {bloc.Sequence} {bloc.Jour}: au moins un vol requis");
            }
        }

        // Validation du calendrier
        if (config.Calendrier.Count == 0)
            throw new ArgumentException("Le calendrier est vide (au moins une affectation requise)");

        foreach (var aff in config.Calendrier)
        {
            if (!refsConnues.Contains(aff.SemaineTypeRef))
                throw new ArgumentException($"Calendrier S{aff.Semaine:D2}-{aff.Annee}: référence '{aff.SemaineTypeRef}' inconnue");
        }
    }
}
