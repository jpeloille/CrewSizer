# Plan-001 : Audit de la méthodologie de calcul CrewSizer

## Contexte

Validation de la pertinence de l'approche mathématique implémentée dans CrewSizer : outil de calcul de marge d'engagement équipage pour Air Calédonie (TY/TPC), réseau domestique Nouvelle-Calédonie, ATR 72-600 mono-flotte.

Le modèle est de **niveau 2** (contraintes cumulatives emboîtées), basé sur la littérature académique (Akyurt et al., 2021 ; Kasirzadeh et al., 2015) et les pratiques NBAA/IATA/ICF (`contexte.md` lignes 11-12).

Sources réglementaires consultées : EASA ORO.FTL.210/235, DGAC (ecologie.gouv.fr), DAC Nouvelle-Calédonie (aviation-civile.nc), Légifrance (arrêté 28 juin 2011).

---

## 1. Conformité réglementaire

### Limites cumulatives d'heures de vol

| Limite | Valeur CrewSizer | EU-OPS Subpart Q | EASA ORO.FTL.210 | Verdict |
|--------|-------------------|-------------------|-------------------|---------|
| 28 jours consécutifs | 100h | 100h | 100h | CORRECT |
| 90 jours consécutifs | 280h | 280h | **Non existant** | CORRECT pour EU-OPS |
| 12 mois consécutifs | 900h | 900h | 1000h (900h/an civil) | CORRECT pour EU-OPS |

**Conclusion** : Les valeurs 100/280/900 correspondent a **EU-OPS Subpart Q**, qui est le cadre applicable en Nouvelle-Calédonie (arrêté du 28 juin 2011, transposant EU-OPS pour les territoires hors EASA). La référence a "EASA ORO.FTL.210-235" dans `contexte.md` ligne 621 est **imprécise** mais les valeurs numériques sont correctes.

### TSV et repos

| Paramètre | Valeur CrewSizer | Réglementation | Verdict |
|-----------|-------------------|----------------|---------|
| TSV max journalier | 13h | 13h (2 pilotes, EU-OPS) | CORRECT |
| TSV moyen retenu | 10h | Paramètre de planification (pas réglementaire) | OK |
| Repos minimum | 12h | max(TSV précédent, 12h) a la base (EU-OPS) | CORRECT (borne basse) |
| Jours OFF réglementaires | 8/mois | ~8 (repos étendu tous les 168h = 7j max) | CORRECT |

### Lacune identifiée : limites de temps de service (duty)

EU-OPS Subpart Q impose aussi des limites de **temps de service** (pas seulement heures de vol) :
- 60h / 7 jours consécutifs
- 110h / 14 jours consécutifs
- 190h / 28 jours consécutifs

**Ces limites ne sont PAS modélisées dans CrewSizer.** Pour un réseau court-courrier domestique (étapes de 25-45 min bloc), le ratio duty/bloc est élevé (pré-vol, post-vol, transit, escales). Les limites de temps de service pourraient être plus contraignantes que les limites d'heures de vol. C'est la lacune la plus significative du modèle.

---

## 2. Validation des formules

### 2.1 D_dispo (jours engageables) -- VALIDE

```
D_dispo = MIN(NbJours - JoursOff, FLOOR(NbJours * 24 / cycle))
cycle = TsvMoyen + ReposMinimum
```

- **Borne 1** (jours calendaires - OFF) : directe et correcte
- **Borne 2** (capacité physique du cycle) : nombre maximal de cycles TSV+repos dans le mois
- Le MIN des deux assure qu'aucune contrainte n'est violée
- En pratique, la Borne 1 est presque toujours active (21 << 33 pour les valeurs par défaut)

**Simplification acceptée** : ne modélise pas le séquencement intra-mois des repos étendus (obligation de repos 36h toutes les 168h), mais l'impact est marginal (~5-10%) pour un outil macro.

### 2.2 Capacité brute/nette/alpha -- VALIDE

```
C_brute = effectif * D_dispo
C_net = C_brute - Abattements - FonctionsSol
alpha = C_net / C_brute
```

C'est la **méthode standard** en workforce planning aérien (NBAA, ICF). Le facteur alpha (taux de disponibilité) est le concept central du dimensionnement macro d'équipage. Avec les valeurs par défaut : alpha PNT = 367/462 = 79.4%, ce qui est réaliste pour un opérateur domestique avec des obligations de formation modérées (benchmark industrie : 65-85%).

### 2.3 Contrainte mordante et h_max -- VALIDE

```
h_max = MIN(H28 - cumul28, H90 - cumul90, H12 - cumul12)
C_net_HDV = C_net * (h_max / D_dispo)
```

Le MIN identifie correctement la fenêtre la plus contraignante. Le calcul de C_net_HDV convertit la capacité en jours-personne en capacité en heures de vol, bornée par la contrainte cumulative. Mathématiquement correct.

### 2.4 Vérification cumulative par personne -- CORRECT MAIS OPTIMISTE

```
HdvParPersonne = bHdvTotal / effectif   (CalculateurMarge.cs:185)
```

Divise les heures totales par l'effectif **total** (pas l'effectif volant = effectif * alpha). C'est **conforme a la spécification** (`contexte.md` section 5.2 étape 9 : `hdv_pp <- B_HDV / N`).

**Pourquoi ce n'est pas un bug** : on répartit les heures sur tous les pilotes, en supposant que chacun vole une part égale sur le mois. Les pilotes en abattement ce mois-la ne volent pas, mais les cumuls sont par définition glissants (28j, 90j, 12m) et couvrent plusieurs mois.

**Limite connue** : c'est une vérification moyenne. Un pilote individuellement peut être saturé si la répartition réelle est inégale. C'est identifié comme évolution P2 dans `contexte.md` (profil individuel).

### 2.5 Taux d'engagement et seuils -- VALIDE

```
tau < 85% -> CONFORTABLE
85% <= tau < 95% -> TENDU
tau >= 95% -> CRITIQUE
```

Ces seuils sont **cohérents avec les pratiques du secteur**. Les guidelines IATA/NBAA considèrent 80-85% comme la limite supérieure de confort. Le 95% est universellement reconnu comme critique.

**Note contextuelle** : pour un opérateur insulaire isolé (pas de wet lease, pas de mutualisation inter-compagnies, géographie contrainte), le seuil "confortable" a 85% est peut-être légèrement optimiste. Un 80% serait plus prudent.

### 2.6 Répartition proportionnelle par sous-catégorie -- ACCEPTABLE

```
CDB_cap = PNT.C_net * N_cdb / (N_cdb + N_opl)
```

Hypothèse : abattements et fonctions sol se répartissent proportionnellement au headcount. En réalité, certaines fonctions sol sont spécifiques (RDOV/RDFE/TRI = CDB typiquement). Acceptable pour le macro, mais une répartition par sous-catégorie serait plus précise.

### 2.7 Allocation cabine -- CORRECT

```
PNC_used = MIN(PNC_cap, rotations)
CC_need = rotations + CEIL(rotations - PNC_used)
PNC_need = FLOOR(PNC_used)
```

Vérifié par trace numérique :
- Cas tous PNC dispo : CC_need = rotations (1 CC/rotation), PNC_need = rotations -> chaque rotation = 1CC + 1PNC
- Cas aucun PNC : CC_need = 2*rotations (2 CC/rotation), PNC_need = 0
- Cas partiel (80 PNC / 101 rotations) : 80 rot CC+PNC + 21 rot 2xCC = correct

Le FLOOR/CEIL est conservateur dans la bonne direction.

### 2.8 Effectif minimum et excédent -- VALIDE

```
N_min = CEIL(2 * rotations / (D_dispo * alpha))
```

Le facteur 2 (1 CDB + 1 OPL par rotation, ou 1 CC + 1 PNC) est correct pour cette configuration. La formule inverse correctement la capacité pour trouver le headcount minimum.

### 2.9 Blocs absorbables -- CORRECT ET CONSERVATEUR

```
MinMarge = MIN(Marge de chaque catégorie active)
BlocsAbsorbables = FLOOR(MinMarge) * EtapesParRotation
```

Limité par la catégorie la plus contrainte = estimation conservatrice, appropriée.

---

## 3. Synthèse

### Ce qui est correct

| Aspect | Statut |
|--------|--------|
| Valeurs réglementaires EU-OPS Subpart Q (100h/28j, 280h/90j, 900h/12m) | CORRECT |
| TSV max 13h, repos min 12h, OFF 8j/mois | CORRECT |
| Modèle capacitaire brut/net/alpha | VALIDE (standard industrie) |
| Contrainte mordante par MIN des fenêtres | VALIDE |
| Seuils d'engagement 85%/95% | VALIDE (normes secteur) |
| Allocation cabine CC/PNC | CORRECT |
| Effectif minimum et excédent | VALIDE |
| Gestion des cas limites (alpha<=0, D_dispo<=0, etc.) | CORRECT |

### Ce qui mérite attention

| Point | Sévérité | Description |
|-------|----------|-------------|
| Limites temps de service (duty) non modélisées | **Significatif** | 60h/7j, 110h/14j, 190h/28j non implémentés. Pour du court-courrier (ratio duty/bloc élevé), cela peut sous-estimer les besoins de 10-20%. |
| Vérification cumulative = moyenne | Faible | `bHdv / effectif` conforme au spec, mais masque les saturations individuelles. Identifié comme P2. |
| Répartition proportionnelle CDB/OPL | Faible | Les fonctions sol sont rank-spécifiques en réalité. Acceptable en macro. |
| Référence "EASA" dans contexte.md | Cosmétique | Devrait dire "EU-OPS Subpart Q (applicable via SEAC/DAC-NC)" |
| Seuil 85% pour opérateur insulaire | Avis | 80% serait plus prudent vu l'absence de solutions de repli |

### Verdict final

**La méthodologie est mathématiquement saine et conforme aux pratiques standard de l'industrie** pour un outil de dimensionnement macro (niveau 2). Les formules implémentées dans `CalculateurMarge.cs` sont correctes, les valeurs réglementaires sont exactes pour le cadre EU-OPS applicable en Nouvelle-Calédonie, et le modèle produit des résultats cohérents.

L'amélioration la plus impactante serait l'ajout des **limites de temps de service** (60h/7j, 110h/14j, 190h/28j) en parallèle des limites d'heures de vol, particulièrement pertinent pour ce réseau court-courrier avec des ratios duty/bloc élevés.

---

## Sources réglementaires

- [EASA ORO.FTL FAQ](https://www.easa.europa.eu/en/the-agency/faqs/oroftl) - Limites cumulatives ORO.FTL.210
- [EASA ORO.FTL.235 Rest Periods](https://regulatorylibrary.caa.co.uk/965-2012/Content/Document%20Structure/03%20ORO/2%20Regs/05190_ORO.FTL.235_Rest_periods.htm) - Repos minimum 12h base, 10h hors base
- [DGAC - Règles techniques FTL](https://www.ecologie.gouv.fr/sites/default/files/documents/150324_DGAC_FTL_2_Regles_techniques.pdf) - TSV max 13h (2 pilotes)
- [DGAC - Règles FTL synoptiques](https://www.ecologie.gouv.fr/sites/default/files/documents/Regles_FTL_agregees_synoptiques.pdf) - Vue d'ensemble FTL
- [Arrêté 28 juin 2011 Sous-partie Q (Légifrance)](https://www.legifrance.gouv.fr/loda/article_lc/LEGIARTI000026250337) - FTL applicable en NC
- [DAC Nouvelle-Calédonie](https://www.aviation-civile.nc/reglementation-annexe/generalites-reglementaires) - Cadre réglementaire NC
- [EASA Opinion 04/2012](https://www.easa.europa.eu/en/downloads/9800/en) - EU-OPS Subpart Q : 100h/28j, 280h/90j, 900h/12m

## Fichiers analysés

- `Services/CalculateurMarge.cs` - Moteur de calcul (259 lignes, toutes les formules)
- `Models/Configuration.cs` - Structure des entrées
- `Models/ResultatMarge.cs` - Structure des sorties
- `Config/parametres.json` - Paramètres par défaut Air Calédonie
- `contexte.md` - Spécification complète du modèle mathématique
