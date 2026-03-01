using CrewSizer.Domain.Entities;

namespace CrewSizer.Domain.Services;

public static class CatalogueResolver
{
    /// <summary>
    /// Hydrate les Vols de chaque BlocVol du catalogue depuis CatalogueVols,
    /// en résolvant les Etapes (VolId) vers les objets Vol.
    /// </summary>
    public static void HydraterBlocs(Configuration config)
    {
        var volsDict = config.CatalogueVols.ToDictionary(v => v.Id);
        foreach (var bloc in config.CatalogueBlocs)
        {
            bloc.Vols.Clear();
            foreach (var etape in bloc.Etapes.OrderBy(e => e.Position))
            {
                if (volsDict.TryGetValue(etape.VolId, out var vol))
                    bloc.Vols.Add(vol);
            }
        }
    }

    /// <summary>
    /// Résout les Placements de chaque SemaineType en créant des copies de BlocVol
    /// avec Jour/Sequence issus du placement et Vols hydratés depuis le catalogue.
    /// Peuple SemaineType.Blocs (propriété transiente).
    /// </summary>
    public static void ResoudreSemainesTypes(Configuration config)
    {
        var blocsDict = config.CatalogueBlocs.ToDictionary(b => b.Id);
        foreach (var st in config.SemainesTypes)
        {
            st.Blocs.Clear();
            foreach (var placement in st.Placements
                         .OrderBy(p => HeureHelper.JourVersIndex(p.Jour))
                         .ThenBy(p => p.Sequence))
            {
                if (!blocsDict.TryGetValue(placement.BlocId, out var blocTemplate))
                    continue;

                var blocResolu = new BlocVol
                {
                    Id = blocTemplate.Id,
                    Code = blocTemplate.Code,
                    Periode = blocTemplate.Periode,
                    DebutDP = blocTemplate.DebutDP,
                    FinDP = blocTemplate.FinDP,
                    DebutFDP = blocTemplate.DebutFDP,
                    FinFDP = blocTemplate.FinFDP,
                    Etapes = blocTemplate.Etapes,
                    Vols = blocTemplate.Vols,
                    Jour = placement.Jour,
                    Sequence = placement.Sequence,
                    TypeAvionId = blocTemplate.TypeAvionId,
                    TypeAvion = blocTemplate.TypeAvion
                };
                st.Blocs.Add(blocResolu);
            }
        }
    }

    /// <summary>
    /// Pipeline complet : hydrater les blocs du catalogue puis résoudre les semaines types.
    /// À appeler après le chargement de la configuration.
    /// </summary>
    public static void ResoudreTout(Configuration config)
    {
        HydraterBlocs(config);
        ResoudreSemainesTypes(config);
    }
}
