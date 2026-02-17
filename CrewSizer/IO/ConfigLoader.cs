using CrewSizer.Helpers;
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

        // Validation du catalogue de vols
        var volIds = new HashSet<Guid>();
        foreach (var vol in config.CatalogueVols)
        {
            if (vol.Id == Guid.Empty)
                throw new ArgumentException($"Vol '{vol.Numero}' : Id vide");
            if (!volIds.Add(vol.Id))
                throw new ArgumentException($"Vol '{vol.Numero}' : Id en double");
        }

        // Validation du catalogue de blocs
        var blocIds = new HashSet<Guid>();
        var blocCodes = new HashSet<string>();
        foreach (var bloc in config.CatalogueBlocs)
        {
            if (bloc.Id == Guid.Empty)
                throw new ArgumentException($"Bloc '{bloc.Code}' : Id vide");
            if (!blocIds.Add(bloc.Id))
                throw new ArgumentException($"Bloc '{bloc.Code}' : Id en double");
            if (string.IsNullOrWhiteSpace(bloc.Code))
                throw new ArgumentException("Bloc : Code vide");
            if (!blocCodes.Add(bloc.Code))
                throw new ArgumentException($"Bloc Code en double : '{bloc.Code}'");

            if (bloc.Etapes.Count == 0 && bloc.Vols.Count == 0)
                throw new ArgumentException($"Bloc '{bloc.Code}' : au moins un vol/étape requis");

            foreach (var etape in bloc.Etapes)
            {
                if (!volIds.Contains(etape.VolId))
                    throw new ArgumentException($"Bloc '{bloc.Code}' étape {etape.Position}: VolId '{etape.VolId}' inconnu dans le catalogue");
            }
        }

        // Validation des semaines types
        if (config.SemainesTypes.Count == 0)
            throw new ArgumentException("Au moins une semaine type est requise");

        var refsConnues = new HashSet<string>();
        var stIds = new HashSet<Guid>();
        foreach (var st in config.SemainesTypes)
        {
            if (string.IsNullOrWhiteSpace(st.Reference))
                throw new ArgumentException("Semaine type : référence vide");
            if (!refsConnues.Add(st.Reference))
                throw new ArgumentException($"Semaine type en double : '{st.Reference}'");
            if (st.Id != Guid.Empty && !stIds.Add(st.Id))
                throw new ArgumentException($"Semaine type Id en double : '{st.Reference}'");

            // Valider via Placements si présents, sinon via Blocs (résolus)
            if (st.Placements.Count > 0)
            {
                foreach (var placement in st.Placements)
                {
                    if (!blocIds.Contains(placement.BlocId))
                        throw new ArgumentException($"Semaine type '{st.Reference}' placement seq {placement.Sequence} {placement.Jour}: BlocId '{placement.BlocId}' inconnu");
                    if (HeureHelper.JourVersIndex(placement.Jour) == 0)
                        throw new ArgumentException($"Semaine type '{st.Reference}' placement seq {placement.Sequence}: jour invalide '{placement.Jour}'");
                }
            }
            else if (st.Blocs.Count > 0)
            {
                foreach (var bloc in st.Blocs)
                {
                    if (HeureHelper.JourVersIndex(bloc.Jour) == 0)
                        throw new ArgumentException($"Semaine type '{st.Reference}' bloc seq {bloc.Sequence}: jour invalide '{bloc.Jour}'");
                    if (bloc.Vols.Count == 0)
                        throw new ArgumentException($"Semaine type '{st.Reference}' bloc seq {bloc.Sequence} {bloc.Jour}: au moins un vol requis");
                }
            }
            else
            {
                throw new ArgumentException($"Semaine type '{st.Reference}' : au moins un placement/bloc requis");
            }
        }

        // Validation du calendrier
        if (config.Calendrier.Count == 0)
            throw new ArgumentException("Le calendrier est vide (au moins une affectation requise)");

        foreach (var aff in config.Calendrier)
        {
            // Valider par SemaineTypeId (nouveau) ou SemaineTypeRef (legacy)
            if (aff.SemaineTypeId != Guid.Empty)
            {
                if (!stIds.Contains(aff.SemaineTypeId))
                    throw new ArgumentException($"Calendrier S{aff.Semaine:D2}-{aff.Annee}: SemaineTypeId '{aff.SemaineTypeId}' inconnu");
            }
            else if (!string.IsNullOrEmpty(aff.SemaineTypeRef))
            {
                if (!refsConnues.Contains(aff.SemaineTypeRef))
                    throw new ArgumentException($"Calendrier S{aff.Semaine:D2}-{aff.Annee}: référence '{aff.SemaineTypeRef}' inconnue");
            }
            else
            {
                throw new ArgumentException($"Calendrier S{aff.Semaine:D2}-{aff.Annee}: ni SemaineTypeId ni SemaineTypeRef défini");
            }
        }
    }
}
