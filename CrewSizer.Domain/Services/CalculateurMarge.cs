using CrewSizer.Domain.Entities;
using CrewSizer.Domain.ValueObjects;

namespace CrewSizer.Domain.Services;

public static class CalculateurMarge
{
    public static ResultatMarge Calculer(Configuration config)
    {
        var result = new ResultatMarge
        {
            DateDebut = config.Periode.DateDebut,
            DateFin = config.Periode.DateFin,
            LibellePeriode = config.Periode.LibellePeriode,
            NbJours = config.Periode.NbJours
        };

        result.EffectifUtilise = config.Effectif;

        // 1. D_dispo (commun à toutes les catégories — mêmes FTL)
        int jOff = config.JoursOff.Reglementaire + config.JoursOff.AccordEntreprise;
        double cycle = config.LimitesFTL.TsvMoyenRetenu + config.LimitesFTL.ReposMinimum;
        int jSrvMax = (int)Math.Floor(config.Periode.NbJours * 24.0 / cycle);
        int dDispo = Math.Min(config.Periode.NbJours - jOff, jSrvMax);

        result.DDisponible = dDispo;
        result.JoursServiceMaxCycle = jSrvMax;
        result.Cycle = cycle;

        if (dDispo <= 0)
        {
            result.Alertes.Add("ERREUR: D_dispo <= 0 — trop de jours OFF par rapport au mois");
            result.StatutGlobal = "ERREUR";
            return result;
        }

        // 2. Résolution du programme via le calendrier
        var programme = CalendrierHelper.ResoudreProgramme(config, out int nbSemaines, out double semainesMois);
        result.SemainesMois = semainesMois;
        result.NbSemainesPeriode = nbSemaines;

        if (nbSemaines <= 0)
        {
            result.Alertes.Add("ERREUR: aucune semaine résolue — calendrier vide ou mois invalide");
            result.StatutGlobal = "ERREUR";
            return result;
        }

        // Étapes par rotation : moyenne sur tous les blocs résolus
        double s = programme.Count > 0
            ? programme.Average(b => b.NbEtapes)
            : 0;
        result.EtapesParRotation = s;

        if (s <= 0)
        {
            result.Alertes.Add("ERREUR: étapes par rotation = 0 — programme vide ou sans étapes");
            result.StatutGlobal = "ERREUR";
            return result;
        }

        // Agrégation directe (blocs résolus = totaux de la période)
        var groupes = programme
            .GroupBy(b => b.Nom)
            .Select(g => new
            {
                Nom = g.Key,
                BlocsMois = g.Count(),
                HdvMois = g.Sum(b => b.HdvBloc)
            })
            .ToList();

        int fTotal = 0;
        double bHdv = 0;
        foreach (var g in groupes)
        {
            fTotal += g.BlocsMois;
            bHdv += g.HdvMois;
            result.DetailProgramme.Add((g.Nom, g.BlocsMois, g.HdvMois));
        }
        result.TotalBlocs = fTotal;
        result.TotalHDV = bHdv;

        int rotations = (int)Math.Ceiling(fTotal / s);
        result.Rotations = rotations;

        // Résumé par jour de semaine (sur l'ensemble de la période)
        result.ResumeSemaine = programme
            .GroupBy(b => b.JourIndex)
            .OrderBy(g => g.Key)
            .Select(g => new ResumeSemaineJour
            {
                Jour = g.Key,
                JourNom = g.First().Jour,
                NbBlocs = g.Count(),
                Hdv = g.Sum(b => b.HdvBloc),
                Ts = g.Sum(b => b.DureeTSHeures)
            })
            .ToList();

        // 3. Capacité groupe PNT
        int nPnt = config.Effectif.Cdb + config.Effectif.Opl;
        result.PNT = CalculerGroupe("PNT", nPnt, dDispo,
            config.AbattementsPNT, config.FonctionsSolPNT,
            config.LimitesCumulatives, config.LimitesCumulatives.CumulPNT);

        // 4. Capacité groupe PNC
        int nPnc = config.Effectif.Cc + config.Effectif.Pnc;
        result.PNC = CalculerGroupe("PNC", nPnc, dDispo,
            config.AbattementsPNC, config.FonctionsSolPNC,
            config.LimitesCumulatives, config.LimitesCumulatives.CumulPNC);

        // Validation alpha
        ValiderAlpha(result, result.PNT, "PNT");
        if (nPnc > 0)
            ValiderAlpha(result, result.PNC, "PNC");

        // 5. Capacités par sous-catégorie (répartition proportionnelle)
        double cdbCap = nPnt > 0 ? (double)result.PNT.CapaciteNette * config.Effectif.Cdb / nPnt : 0;
        double oplCap = nPnt > 0 ? (double)result.PNT.CapaciteNette * config.Effectif.Opl / nPnt : 0;
        double ccCap = nPnc > 0 ? (double)result.PNC.CapaciteNette * config.Effectif.Cc / nPnc : 0;
        double pncCap = nPnc > 0 ? (double)result.PNC.CapaciteNette * config.Effectif.Pnc / nPnc : 0;

        // 6. Agrégation des besoins équipage par TypeAvion du programme
        int totalBesoinCdb = programme.Sum(b => b.TypeAvion?.NbCdb ?? 1);
        int totalBesoinOpl = programme.Sum(b => b.TypeAvion?.NbOpl ?? 1);
        int totalBesoinCc  = programme.Sum(b => b.TypeAvion?.NbCc ?? 1);
        int totalBesoinPnc = programme.Sum(b => b.TypeAvion?.NbPnc ?? 0);

        result.RotationsAvecPNC = totalBesoinPnc;
        result.RotationsSansPNC = rotations - Math.Min(rotations, totalBesoinPnc);

        // 7. Marges par sous-catégorie
        result.CDB = CalculerCategorie("CDB", config.Effectif.Cdb, cdbCap, totalBesoinCdb);
        result.OPL = CalculerCategorie("OPL", config.Effectif.Opl, oplCap, totalBesoinOpl);
        result.CC = CalculerCategorie("CC", config.Effectif.Cc, ccCap, totalBesoinCc);
        result.PNCDetail = CalculerCategorie("PNC", config.Effectif.Pnc, pncCap, totalBesoinPnc);

        // 8. Vérification HDV cumulatives par groupe
        VerifierButees(result.PNT, nPnt, bHdv,
            config.LimitesCumulatives, config.LimitesCumulatives.CumulPNT);

        if (nPnc > 0)
            VerifierButees(result.PNC, nPnc, bHdv,
                config.LimitesCumulatives, config.LimitesCumulatives.CumulPNC);

        // 9. Vérification TSV max (table EU-OPS)
        VerifierTsvMax(result, config);

        // 10. Vérification limites temps de service (duty)
        VerifierTempsService(result, config, nPnt, nPnc);

        // 11. Statut global = catégorie la plus contrainte
        var categories = new[] { result.CDB, result.OPL, result.CC, result.PNCDetail }
            .Where(c => c.Effectif > 0)
            .ToArray();

        if (categories.Length > 0)
        {
            var contraignante = categories.OrderByDescending(c => c.TauxEngagement).ThenBy(c => c.Nom).First();
            result.TauxEngagementGlobal = contraignante.TauxEngagement;
            result.CategorieContraignante = contraignante.Nom;
            result.StatutGlobal = DeterminerStatut(contraignante.TauxEngagement);
        }

        // 12. Analyses complémentaires
        if (result.PNT.Alpha > 0 && dDispo > 0)
        {
            result.NMinPNT = (int)Math.Ceiling(2.0 * rotations / (dDispo * result.PNT.Alpha));
            result.ExcedentPNT = nPnt - result.NMinPNT;
        }

        if (nPnc > 0 && result.PNC.Alpha > 0 && dDispo > 0)
        {
            result.NMinPNCGroupe = (int)Math.Ceiling(2.0 * rotations / (dDispo * result.PNC.Alpha));
            result.ExcedentPNCGroupe = nPnc - result.NMinPNCGroupe;
        }

        // Blocs absorbables (conservateur : limité par la catégorie la plus contrainte)
        if (categories.Length > 0)
        {
            double minMarge = categories.Min(c => c.Marge);
            int extraRotations = (int)Math.Max(0, Math.Floor(minMarge));
            result.BlocsAbsorbables = (int)(extraRotations * s);
        }

        // Alertes complémentaires
        GenererAlertes(result, config, rotations, dDispo);

        return result;
    }

    private static ResultatGroupe CalculerGroupe(
        string nom, int effectif, int dDispo,
        List<Abattement> abattements, List<FonctionSol> fonctionsSol,
        LimitesCumulatives limites, CumulEntrant cumul)
    {
        var groupe = new ResultatGroupe { Nom = nom, Effectif = effectif };

        int cBrute = effectif * dDispo;
        int sigmaAbat = abattements.Sum(a => a.JoursPersonnel);
        int sigmaSol = fonctionsSol.Sum(f => f.JoursPersonnelSol);
        int cNet = cBrute - sigmaAbat - sigmaSol;

        groupe.CapaciteBrute = cBrute;
        groupe.TotalAbattements = sigmaAbat;
        groupe.TotalJoursSol = sigmaSol;
        groupe.CapaciteNette = cNet;
        groupe.Alpha = cBrute > 0 ? (double)cNet / cBrute : 0;

        // Contrainte mordante
        double h28Dispo = limites.H28Max - cumul.Cumul28Entrant;
        double h90Dispo = limites.H90Max - cumul.Cumul90Entrant;
        double h12Dispo = limites.H12Max - cumul.Cumul12Entrant;
        double hMax = Math.Min(h28Dispo, Math.Min(h90Dispo, h12Dispo));

        groupe.HMax = hMax;
        if (hMax == h28Dispo) groupe.ContrainteMordante = "28 jours";
        else if (hMax == h90Dispo) groupe.ContrainteMordante = "90 jours";
        else groupe.ContrainteMordante = "12 mois";

        groupe.CapaciteNetteHDV = dDispo > 0 ? cNet * (hMax / dDispo) : 0;

        return groupe;
    }

    private static void VerifierButees(
        ResultatGroupe groupe, int effectif, double bHdvTotal,
        LimitesCumulatives limites, CumulEntrant cumul)
    {
        if (effectif <= 0) return;

        groupe.HdvParPersonne = bHdvTotal / effectif;
        double c28 = cumul.Cumul28Entrant + groupe.HdvParPersonne;
        double c90 = cumul.Cumul90Entrant + groupe.HdvParPersonne;
        double c12 = cumul.Cumul12Entrant + groupe.HdvParPersonne;

        groupe.Verif28j = (c28, c28 <= limites.H28Max);
        groupe.Verif90j = (c90, c90 <= limites.H90Max);
        groupe.Verif12m = (c12, c12 <= limites.H12Max);
    }

    /// <summary>Vérifie le TSV max de chaque bloc contre la table EU-OPS</summary>
    private static void VerifierTsvMax(ResultatMarge result, Configuration config)
    {
        if (config.TableTsvMax.Count == 0) return;

        // Vérifier les blocs uniques (1 exemplaire par semaine type, pas de doublons)
        var blocsUniques = CalendrierHelper.BlocsUniques(config);
        foreach (var bloc in blocsUniques)
        {
            var debutTsv = BlocVol.ParseHeure(bloc.DebutFDP);
            double tsvMaxAutorise = -1;

            // Recherche dans la table : trouver la bande horaire correspondante
            foreach (var entree in config.TableTsvMax)
            {
                var debutBande = BlocVol.ParseHeure(entree.DebutBande);
                var finBande = BlocVol.ParseHeure(entree.FinBande);

                bool dansBande;
                if (finBande > debutBande)
                    dansBande = debutTsv >= debutBande && debutTsv < finBande;
                else // passage minuit (ex: 22:00-05:59)
                    dansBande = debutTsv >= debutBande || debutTsv < finBande;

                if (!dansBande) continue;

                // Chercher le max pour ce nombre d'étapes
                int nbEtapes = bloc.NbEtapes;
                if (entree.MaxParEtapes.TryGetValue(nbEtapes, out double max))
                {
                    tsvMaxAutorise = max;
                }
                else
                {
                    // Prendre la clé la plus élevée <= nbEtapes, ou la plus grande disponible
                    var cles = entree.MaxParEtapes.Keys.OrderBy(k => k).ToList();
                    var cle = cles.LastOrDefault(k => k <= nbEtapes);
                    if (cle == 0 && cles.Count > 0)
                        cle = cles.Last(); // fallback : prendre le max d'étapes disponible
                    if (cle > 0 && entree.MaxParEtapes.TryGetValue(cle, out double maxFallback))
                        tsvMaxAutorise = maxFallback;
                }

                break;
            }

            bool conforme = tsvMaxAutorise < 0 || bloc.DureeTSVHeures <= tsvMaxAutorise;

            result.VerificationsTSV.Add(new VerifTsvMax
            {
                Nom = bloc.Nom,
                Jour = bloc.JourIndex,
                JourNom = bloc.Jour,
                NbEtapes = bloc.NbEtapes,
                TsvDuree = bloc.DureeTSVHeures,
                TsvMaxAutorise = tsvMaxAutorise,
                Conforme = conforme
            });

            if (!conforme)
                result.TousBlocsConformesTSV = false;
        }
    }

    /// <summary>Vérifie les limites de temps de service (duty) EU-OPS</summary>
    private static void VerifierTempsService(ResultatMarge result, Configuration config, int nPnt, int nPnc)
    {
        // TS hebdo moyen = total TS de la période / nombre de semaines
        var programme = CalendrierHelper.ResoudreProgramme(config, out int nbSem, out _);
        double tsTotalPeriode = programme.Sum(b => b.DureeTSHeures);
        double tsHebdo = nbSem > 0 ? tsTotalPeriode / nbSem : 0;
        double tsMensuel = tsTotalPeriode;
        var limites = config.LimitesTempsService;

        // PNT
        if (nPnt > 0)
        {
            double tsParPersonne = tsHebdo / nPnt;
            result.VerifTempsServicePNT = new VerifTempsService
            {
                TotalTSHebdo = tsHebdo,
                TotalTSMensuel = tsMensuel,
                TSParPersonneHebdo = tsParPersonne,
                Verif7j = (tsParPersonne, limites.Max7j, tsParPersonne <= limites.Max7j),
                Verif14j = (tsParPersonne * 2, limites.Max14j, tsParPersonne * 2 <= limites.Max14j),
                Verif28j = (tsParPersonne * 4, limites.Max28j, tsParPersonne * 4 <= limites.Max28j)
            };
        }

        // PNC
        if (nPnc > 0)
        {
            double tsParPersonne = tsHebdo / nPnc;
            result.VerifTempsServicePNC = new VerifTempsService
            {
                TotalTSHebdo = tsHebdo,
                TotalTSMensuel = tsMensuel,
                TSParPersonneHebdo = tsParPersonne,
                Verif7j = (tsParPersonne, limites.Max7j, tsParPersonne <= limites.Max7j),
                Verif14j = (tsParPersonne * 2, limites.Max14j, tsParPersonne * 2 <= limites.Max14j),
                Verif28j = (tsParPersonne * 4, limites.Max28j, tsParPersonne * 4 <= limites.Max28j)
            };
        }
    }

    private static ResultatCategorie CalculerCategorie(string nom, int effectif, double capacite, int besoin)
    {
        double tau = capacite > 0
            ? besoin / capacite
            : 0;

        string statut = effectif <= 0
            ? "SANS_EFFECTIF"
            : DeterminerStatut(tau);

        return new ResultatCategorie
        {
            Nom = nom,
            Effectif = effectif,
            Capacite = capacite,
            Besoin = besoin,
            Marge = capacite - besoin,
            TauxEngagement = tau,
            Statut = statut
        };
    }

    private static string DeterminerStatut(double tau)
    {
        if (tau < 0.85) return "CONFORTABLE";
        if (tau < 0.95) return "TENDU";
        return "CRITIQUE";
    }

    private static void ValiderAlpha(ResultatMarge result, ResultatGroupe groupe, string label)
    {
        if (groupe.Alpha <= 0)
            result.Alertes.Add($"ERREUR: alpha {label} <= 0 — abattements supérieurs à la capacité brute");
        else if (groupe.Alpha > 1)
            result.Alertes.Add($"ERREUR: alpha {label} > 1 — incohérence paramétrage");
    }

    private static void GenererAlertes(ResultatMarge result, Configuration config, int rotations, int dDispo)
    {
        // Butées HDV saturées
        if (result.PNT.HMax <= 0)
            result.Alertes.Add("ALERTE: compteurs cumulatifs PNT déjà saturés (h_max <= 0)");
        if (config.Effectif.Cc + config.Effectif.Pnc > 0 && result.PNC.HMax <= 0)
            result.Alertes.Add("ALERTE: compteurs cumulatifs PNC déjà saturés (h_max <= 0)");

        // Dépassement butées HDV
        if (!result.PNT.Verif28j.ok) result.Alertes.Add("ALERTE: dépassement butée 28j PNT");
        if (!result.PNT.Verif90j.ok) result.Alertes.Add("ALERTE: dépassement butée 90j PNT");
        if (!result.PNT.Verif12m.ok) result.Alertes.Add("ALERTE: dépassement butée 12m PNT");

        int nPnc = config.Effectif.Cc + config.Effectif.Pnc;
        if (nPnc > 0)
        {
            if (!result.PNC.Verif28j.ok) result.Alertes.Add("ALERTE: dépassement butée 28j PNC");
            if (!result.PNC.Verif90j.ok) result.Alertes.Add("ALERTE: dépassement butée 90j PNC");
            if (!result.PNC.Verif12m.ok) result.Alertes.Add("ALERTE: dépassement butée 12m PNC");
        }

        // TSV max non conforme
        foreach (var v in result.VerificationsTSV.Where(v => !v.Conforme))
        {
            result.Alertes.Add(
                $"ALERTE: TSV dépassé bloc '{v.Nom}' {v.JourNom} — " +
                $"{v.TsvDuree:F2}h > {v.TsvMaxAutorise:F2}h max ({v.NbEtapes} étapes)");
        }

        // Limites temps de service (duty)
        if (!result.VerifTempsServicePNT.Verif7j.ok)
            result.Alertes.Add($"ALERTE: duty PNT dépasse 7j ({result.VerifTempsServicePNT.Verif7j.valeur:F1}h > {result.VerifTempsServicePNT.Verif7j.limite:F0}h)");
        if (!result.VerifTempsServicePNT.Verif14j.ok)
            result.Alertes.Add($"ALERTE: duty PNT dépasse 14j ({result.VerifTempsServicePNT.Verif14j.valeur:F1}h > {result.VerifTempsServicePNT.Verif14j.limite:F0}h)");
        if (!result.VerifTempsServicePNT.Verif28j.ok)
            result.Alertes.Add($"ALERTE: duty PNT dépasse 28j ({result.VerifTempsServicePNT.Verif28j.valeur:F1}h > {result.VerifTempsServicePNT.Verif28j.limite:F0}h)");

        if (nPnc > 0)
        {
            if (!result.VerifTempsServicePNC.Verif7j.ok)
                result.Alertes.Add($"ALERTE: duty PNC dépasse 7j ({result.VerifTempsServicePNC.Verif7j.valeur:F1}h > {result.VerifTempsServicePNC.Verif7j.limite:F0}h)");
            if (!result.VerifTempsServicePNC.Verif14j.ok)
                result.Alertes.Add($"ALERTE: duty PNC dépasse 14j ({result.VerifTempsServicePNC.Verif14j.valeur:F1}h > {result.VerifTempsServicePNC.Verif14j.limite:F0}h)");
            if (!result.VerifTempsServicePNC.Verif28j.ok)
                result.Alertes.Add($"ALERTE: duty PNC dépasse 28j ({result.VerifTempsServicePNC.Verif28j.valeur:F1}h > {result.VerifTempsServicePNC.Verif28j.limite:F0}h)");
        }

        // Déséquilibre CDB/OPL
        if (result.PNT.Alpha > 0 && dDispo > 0)
        {
            int minParCat = (int)Math.Ceiling(rotations / (dDispo * result.PNT.Alpha));
            if (config.Effectif.Cdb < minParCat)
                result.Alertes.Add($"ALERTE: insuffisance CDB ({config.Effectif.Cdb} < {minParCat} minimum)");
            if (config.Effectif.Opl < minParCat)
                result.Alertes.Add($"ALERTE: insuffisance OPL ({config.Effectif.Opl} < {minParCat} minimum)");
        }
    }

    /// <summary>Calcule les résultats pour chaque mois de la période, avec clampage aux bornes</summary>
    public static List<ResultatMarge> CalculerParMois(Configuration config)
    {
        var resultats = new List<ResultatMarge>();
        foreach (var (mois, annee) in config.Periode.MoisCouverts())
        {
            var debutMois = new DateOnly(annee, mois, 1);
            var finMois = new DateOnly(annee, mois, DateTime.DaysInMonth(annee, mois));
            // Clamper aux bornes de la période globale
            var debut = debutMois < config.Periode.DateDebut ? config.Periode.DateDebut : debutMois;
            var fin = finMois > config.Periode.DateFin ? config.Periode.DateFin : finMois;

            var configMois = CloneConfigAvecPeriode(config, new Periode { DateDebut = debut, DateFin = fin });
            resultats.Add(Calculer(configMois));
        }
        return resultats;
    }

    private static Configuration CloneConfigAvecPeriode(Configuration source, Periode periode)
    {
        return new Configuration
        {
            Periode = periode,
            Effectif = source.Effectif,
            LimitesFTL = source.LimitesFTL,
            LimitesCumulatives = source.LimitesCumulatives,
            JoursOff = source.JoursOff,
            LimitesTempsService = source.LimitesTempsService,
            FonctionsSolPNT = source.FonctionsSolPNT,
            FonctionsSolPNC = source.FonctionsSolPNC,
            AbattementsPNT = source.AbattementsPNT,
            AbattementsPNC = source.AbattementsPNC,
            TableTsvMax = source.TableTsvMax,
            Calendrier = source.Calendrier,
            CatalogueVols = source.CatalogueVols,
            CatalogueBlocs = source.CatalogueBlocs,
            SemainesTypes = source.SemainesTypes,
            Equipage = source.Equipage
        };
    }
}
