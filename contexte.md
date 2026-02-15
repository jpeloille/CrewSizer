# Marge d'Engagement Pilotes — Spécification Application Console C#

## 1. Objectif

Développer une application console .NET permettant de calculer la **marge d'engagement** d'un effectif pilote à partir :

- de l'effectif PNT disponible,
- des règles d'engagement réglementaires et conventionnelles,
- du programme de vols (blocs à couvrir).

Le modèle implémenté est de **niveau 2** (contraintes cumulatives emboîtées), issu de la littérature académique sur le *workforce planning* aérien (Akyurt et al., 2021 ; Kasirzadeh et al., 2015) et des pratiques NBAA/IATA/ICF.

---

## 2. Contexte opérationnel

| Paramètre | Valeur Air Calédonie |
|---|---|
| Exploitant | Air Calédonie (TY/TPC) |
| Type avion | ATR 72-600 (mono-flotte) |
| Équipage PNT | 2 (CDB + OPL) |
| Réseau | Domestique Nouvelle-Calédonie |
| Réglementation | SEAC / DAC (proche EU-OPS / EASA FTL) |
| Étapes typiques | 25–45 min de bloc |

---

## 3. Modèle mathématique

### 3.1 Notations

| Symbole | Description | Unité |
|---|---|---|
| `N` | Effectif total pilotes opérationnels | pilotes |
| `N_cdb` | Nombre de CDB | pilotes |
| `N_opl` | Nombre d'OPL | pilotes |
| `D` | Nombre de jours dans la période analysée | jours |
| `k` | Taille d'équipage par vol | pilotes/vol |
| `s` | Étapes moyennes par jour-pilote de service | étapes |
| `F_l` | Blocs/mois sur la ligne `l` | blocs |
| `b_l` | Temps de bloc moyen sur la ligne `l` | heures |
| `H_28` | Limite HDV 28 jours glissants | heures |
| `H_90` | Limite HDV 90 jours glissants | heures |
| `H_12` | Limite HDV 12 mois glissants | heures |
| `TSV_max` | TSV journalier maximum réglementaire | heures |
| `TSV_moy` | TSV moyen retenu pour le calcul | heures |
| `R_min` | Repos minimum entre 2 TSV | heures |
| `J_off` | Jours OFF par pilote par mois | jours |
| `α` | Facteur de disponibilité | ratio [0..1] |

### 3.2 Facteur de disponibilité (α)

Le facteur α représente la proportion de la capacité brute réellement disponible pour le vol après déduction de toutes les indisponibilités.

```
D_dispo = min(D − J_off, ⌊D × 24 / (TSV_moy + R_min)⌋)

α = (N × D_dispo − ΣAbattements − ΣJours_sol) / (N × D_dispo)
```

**Explication** : `D_dispo` est borné par deux contraintes :

1. Le nombre de jours calendaires moins les OFF
2. Le nombre maximum de jours de service imposé par le cycle TSV + repos

C'est le `min()` des deux qui s'applique.

### 3.3 Contraintes cumulatives emboîtées

Chaque pilote `i` doit respecter **simultanément** :

```
h_i(28j)  ≤ H_28       (ex: 100h)
h_i(90j)  ≤ H_90       (ex: 280h)
h_i(12m)  ≤ H_12       (ex: 900h)
TSV_i(j)  ≤ TSV_max    (ex: 13h)
repos_i   ≥ R_min      (ex: 12h)
OFF_i(m)  ≥ J_off_min  (ex: 8j)
```

La **contrainte mordante** (binding constraint) est celle qui limite en premier la capacité individuelle sur la période :

```
h_max = min(H_28 − cumul_28, H_90 − cumul_90, H_12 − cumul_12)
```

Où `cumul_28`, `cumul_90`, `cumul_12` sont les HDV déjà volées sur les périodes glissantes précédentes (compteurs entrants).

### 3.4 Capacité nette

```
C_brute = N × D_dispo                          (jours-pilote)

C_net   = C_brute − Abattements − Jours_sol    (jours-pilote)
        = N × D_dispo × α

C_net_HDV = C_net × (h_max / D_dispo)          (heures de vol)
```

### 3.5 Besoin opérationnel

```
B_jp  = ⌈ΣF_l / s⌉ × k                        (jours-pilote)

B_HDV = Σ(F_l × b_l)                           (heures de vol)
```

Le `⌈⌉` (arrondi supérieur) traduit l'indivisibilité des rotations : un pilote commencé est un pilote engagé pour la journée.

### 3.6 Marge d'engagement

```
M_jp  = C_net − B_jp                           (jours-pilote)
M_HDV = C_net_HDV − B_HDV                      (heures de vol)

τ     = B_jp / C_net                            (taux d'engagement)
```

### 3.7 Seuils d'alerte

| Taux τ | Statut | Signification |
|---|---|---|
| τ < 85% | ✅ CONFORTABLE | Marge suffisante pour absorber les aléas |
| 85% ≤ τ < 95% | ⚠️ TENDU | Peu de marge, vigilance requise |
| τ ≥ 95% | 🔴 CRITIQUE | Risque de non-couverture du programme |

### 3.8 Indicateurs complémentaires

```
ETP_marge    = M_jp / D_dispo                   (pilotes ETP excédentaires/manquants)
Blocs_supp   = M_jp × s / k                     (blocs supplémentaires absorbables)
N_min        = ⌈B_jp / (D_dispo × α)⌉           (effectif minimum théorique)
Excédent     = N − N_min                         (pilotes en surplus ou déficit)
```

---

## 4. Architecture applicative

### 4.1 Structure du projet

```
MargeEngagement/
├── MargeEngagement.csproj
├── Program.cs                      // Point d'entrée, orchestration
├── Models/
│   ├── Parametres.cs               // Données d'entrée (effectif, limites, abattements)
│   ├── LigneVol.cs                 // Définition d'une ligne/route
│   ├── ProgrammeVols.cs            // Collection de lignes + agrégation
│   ├── ContraintesCumulatives.cs   // H_28, H_90, H_12 + compteurs entrants
│   ├── FonctionSol.cs              // RDOV, RDFE, OSV, TRI...
│   └── ResultatMarge.cs            // Résultat complet du calcul
├── Services/
│   ├── CalculateurMarge.cs         // Logique de calcul principale
│   ├── VerificateurContraintes.cs  // Vérification des butées cumulatives
│   └── AnalyseurSensibilite.cs     // Scénarios what-if (optionnel)
├── IO/
│   ├── ConfigLoader.cs             // Chargement config JSON/YAML
│   ├── ConsoleRenderer.cs          // Affichage formaté console
│   └── CsvExporter.cs              // Export résultats CSV (optionnel)
└── Config/
    └── parametres.json             // Fichier de configuration par défaut
```

### 4.2 Modèles de données

#### `Parametres`

```
- Mois                  : string       ("Mars")
- Annee                 : int          (2026)
- NbJoursMois           : int          (31)
- NbCDB                 : int
- NbOPL                 : int
- EquipageParVol        : int          (2)
- EtapesParRotation     : double       (4.0)
- TsvMax                : double       (13.0)   heures
- TsvMoyen              : double       (10.0)   heures
- BlocMoyen             : double       (0.75)   heures
- ReposMinimum          : double       (12.0)   heures
- JoursOffReglementaire : int          (8)
- JoursOffAccord        : int          (2)
- FonctionsSol          : List<FonctionSol>
- Abattements           : List<Abattement>
- Contraintes           : ContraintesCumulatives
```

#### `ContraintesCumulatives`

```
- H28Max                : double       (100.0)
- H90Max                : double       (280.0)
- H12Max                : double       (900.0)
- Cumul28Entrant        : double       (0.0)    HDV déjà volées
- Cumul90Entrant        : double       (0.0)
- Cumul12Entrant        : double       (0.0)
```

#### `LigneVol`

```
- Nom                   : string       ("NOU-LIF")
- BlocsParJour          : int          (4)
- JoursParSemaine       : int          (7)
- SemainesParMois       : double       (4.3)
- HdvParBloc            : double       (0.50)   heures
```

Propriétés calculées :

```
- BlocsParMois          : int   = BlocsParJour × JoursParSemaine × SemainesParMois
- HdvParMois            : double = BlocsParMois × HdvParBloc
```

#### `FonctionSol`

```
- Nom                   : string       ("RDOV")
- NbPilotes             : int          (1)
- JoursSolParMois       : int          (8)
```

Propriété calculée :

```
- JoursPiloteSol        : int   = NbPilotes × JoursSolParMois
```

#### `Abattement`

```
- Libelle               : string       ("Congés annuels")
- JoursPilote           : int          (30)
```

#### `ResultatMarge`

```
// Capacité
- EffectifTotal         : int
- DDisponible           : int
- JoursServiceMaxCycle  : int
- CapaciteBrute         : int          (jours-pilote)
- TotalAbattements      : int
- TotalJoursSol         : int
- CapaciteNette         : int          (jours-pilote)
- Alpha                 : double       (facteur de disponibilité)
- CapaciteNetteHDV      : double

// Contraintes
- HMaxPilote            : double       (contrainte mordante)
- ContrainteMordante    : string       ("28 jours" | "90 jours" | "12 mois")

// Besoin
- TotalBlocs            : int
- BesoinJoursPilote     : int
- BesoinHDV             : double

// Marge
- MargeJoursPilote      : int
- MargeHDV              : double
- TauxEngagement        : double
- Statut                : string       ("CONFORTABLE" | "TENDU" | "CRITIQUE")

// Analyses
- MargeETP              : double
- BlocsAbsorbables      : int
- NbMinPilotes          : int
- Excedent              : int

// Vérifications butées
- VerifButee28j         : (double cumul, bool ok)
- VerifButee90j         : (double cumul, bool ok)
- VerifButee12j         : (double cumul, bool ok)
```

---

## 5. Logique de calcul — `CalculateurMarge`

### 5.1 Méthode principale

```
ResultatMarge Calculer(Parametres params, ProgrammeVols programme)
```

### 5.2 Algorithme séquentiel

```
ENTRÉE: params, programme

1. Calculer les totaux d'entrée
   N           ← params.NbCDB + params.NbOPL
   J_off       ← params.JoursOffReglementaire + params.JoursOffAccord
   cycle       ← params.TsvMoyen + params.ReposMinimum
   J_srv_max   ← ⌊params.NbJoursMois × 24 / cycle⌋
   D_dispo     ← min(params.NbJoursMois − J_off, J_srv_max)
   Σ_abat      ← Σ abattements.JoursPilote
   Σ_sol       ← Σ fonctions.NbPilotes × fonctions.JoursSolParMois

2. Calculer le facteur de disponibilité
   α ← (N × D_dispo − Σ_abat − Σ_sol) / (N × D_dispo)
   ASSERT α ∈ [0, 1]  // sinon erreur de paramétrage

3. Déterminer la contrainte mordante
   h_28_dispo  ← params.Contraintes.H28Max − params.Contraintes.Cumul28Entrant
   h_90_dispo  ← params.Contraintes.H90Max − params.Contraintes.Cumul90Entrant
   h_12_dispo  ← params.Contraintes.H12Max − params.Contraintes.Cumul12Entrant
   h_max       ← min(h_28_dispo, h_90_dispo, h_12_dispo)
   mordante    ← identifier laquelle est le min

4. Calculer la capacité
   C_brute     ← N × D_dispo
   C_net       ← C_brute − Σ_abat − Σ_sol
   C_net_HDV   ← C_net × (h_max / D_dispo)

5. Calculer le besoin opérationnel
   F_total     ← Σ ligne.BlocsParMois
   B_HDV       ← Σ ligne.HdvParMois
   B_jp        ← ⌈F_total / s⌉ × k

6. Calculer la marge
   M_jp        ← C_net − B_jp
   M_HDV       ← C_net_HDV − B_HDV
   τ           ← B_jp / C_net

7. Déterminer le statut
   SI τ < 0.85 → "CONFORTABLE"
   SI τ < 0.95 → "TENDU"
   SINON       → "CRITIQUE"

8. Calculer les indicateurs complémentaires
   ETP_marge   ← M_jp / D_dispo
   blocs_supp  ← M_jp × s / k
   N_min       ← ⌈B_jp / (D_dispo × α)⌉
   excédent    ← N − N_min

9. Vérifier les butées individuelles (répartition égale)
   hdv_pp      ← B_HDV / N
   check_28    ← cumul28 + hdv_pp  vs H_28
   check_90    ← cumul90 + hdv_pp  vs H_90
   check_12    ← cumul12 + hdv_pp  vs H_12

SORTIE: ResultatMarge
```

### 5.3 Cas limites à gérer

| Cas | Traitement |
|---|---|
| `D_dispo ≤ 0` | Erreur : trop de jours OFF par rapport au mois |
| `α ≤ 0` | Erreur : abattements supérieurs à la capacité brute |
| `α > 1` | Erreur : incohérence paramétrage |
| `C_net ≤ 0` | Alerte : aucune capacité disponible |
| `s = 0` | Erreur : division par zéro |
| `k = 0` | Erreur : division par zéro |
| `h_max ≤ 0` | Alerte : compteurs entrants déjà saturés |
| `N_cdb < ⌈B_jp/k⌉` ou `N_opl < ⌈B_jp/k⌉` | Alerte : déséquilibre CDB/OPL |

---

## 6. Vérificateur de contraintes — `VerificateurContraintes`

### 6.1 Responsabilité

Vérifier qu'aucune butée réglementaire n'est franchie pour la période analysée, en partant des compteurs entrants.

### 6.2 Méthode

```
VerificationResult Verifier(Parametres params, double hdvParPiloteMois)
```

Retourne pour chaque butée (28j, 90j, 12m) :

- cumul projeté = compteur_entrant + hdv_mois_par_pilote
- ok = cumul_projeté ≤ limite
- marge_residuelle = limite − cumul_projeté
- contrainte_mordante = identification de la butée limitante

### 6.3 Alerte déséquilibre CDB/OPL

Chaque vol nécessitant 1 CDB + 1 OPL, vérifier :

```
SI N_cdb < ⌈B_jp / (k × D_dispo × α)⌉  →  "Insuffisance CDB"
SI N_opl < ⌈B_jp / (k × D_dispo × α)⌉  →  "Insuffisance OPL"
```

---

## 7. Format d'entrée — `parametres.json`

```json
{
  "periode": {
    "mois": "Mars",
    "annee": 2026,
    "nbJours": 31
  },
  "effectif": {
    "cdb": 12,
    "opl": 10,
    "equipageParVol": 2,
    "etapesParRotation": 4.0
  },
  "limitesFTL": {
    "tsvMaxJournalier": 13.0,
    "tsvMoyenRetenu": 10.0,
    "blocMoyenEtape": 0.75,
    "reposMinimum": 12.0
  },
  "limitesCumulatives": {
    "h28Max": 100,
    "h90Max": 280,
    "h12Max": 900,
    "cumul28Entrant": 0,
    "cumul90Entrant": 0,
    "cumul12Entrant": 0
  },
  "joursOff": {
    "reglementaire": 8,
    "accordEntreprise": 2
  },
  "fonctionsSol": [
    { "nom": "RDOV",    "nbPilotes": 1, "joursSolMois": 8 },
    { "nom": "RDFE",    "nbPilotes": 1, "joursSolMois": 8 },
    { "nom": "OSV",     "nbPilotes": 1, "joursSolMois": 6 },
    { "nom": "TRI/TRE", "nbPilotes": 2, "joursSolMois": 4 }
  ],
  "abattements": [
    { "libelle": "Congés annuels",          "joursPilote": 30 },
    { "libelle": "Maladie / arrêts",        "joursPilote": 8  },
    { "libelle": "Formation CRM/LOFT",      "joursPilote": 10 },
    { "libelle": "Simulateur OPC/LPC",      "joursPilote": 6  },
    { "libelle": "Visites médicales",       "joursPilote": 2  },
    { "libelle": "Contrôles en ligne CEL",  "joursPilote": 4  },
    { "libelle": "Journées syndicales/DP",  "joursPilote": 2  },
    { "libelle": "Autres",                  "joursPilote": 3  }
  ],
  "programme": [
    { "nom": "NOU-LIF", "blocsJour": 4, "joursSemaine": 7, "semainesMois": 4.3, "hdvBloc": 0.50 },
    { "nom": "NOU-MAR", "blocsJour": 4, "joursSemaine": 6, "semainesMois": 4.3, "hdvBloc": 0.58 },
    { "nom": "NOU-UVE", "blocsJour": 2, "joursSemaine": 5, "semainesMois": 4.3, "hdvBloc": 0.67 },
    { "nom": "NOU-TGJ", "blocsJour": 2, "joursSemaine": 4, "semainesMois": 4.3, "hdvBloc": 0.42 },
    { "nom": "NOU-KNQ", "blocsJour": 2, "joursSemaine": 5, "semainesMois": 4.3, "hdvBloc": 0.50 },
    { "nom": "NOU-ILP", "blocsJour": 2, "joursSemaine": 5, "semainesMois": 4.3, "hdvBloc": 0.42 },
    { "nom": "NOU-BMY", "blocsJour": 2, "joursSemaine": 2, "semainesMois": 4.3, "hdvBloc": 0.58 }
  ]
}
```

---

## 8. Format de sortie console

### 8.1 Affichage principal attendu

```
══════════════════════════════════════════════════════════════
  MARGE D'ENGAGEMENT PILOTES — Mars 2026 (31 jours)
══════════════════════════════════════════════════════════════

  EFFECTIF
  ────────────────────────────────────────────────────────────
  CDB .......................... 12
  OPL .......................... 10
  TOTAL ........................ 22 pilotes
  Équipage / vol ............... 2

  CAPACITÉ
  ────────────────────────────────────────────────────────────
  D_dispo (j engageables) ...... 21 j/pilote
  Cycle TSV+repos .............. 22.0 h
  C_brute ...................... 462 jours-pilote
  Abattements .................. -65 jours-pilote
  Fonctions sol ................ -30 jours-pilote
  C_net ........................ 367 jours-pilote
  α (disponibilité) ............ 79.4%
  C_net HDV .................... 1 748 HDV

  CONTRAINTES CUMULATIVES
  ────────────────────────────────────────────────────────────
  h_max (mordante) ............. 100.0 HDV  [butée: 28 jours]
  Vérif 28j .................... 9.7 / 100.0   ✅
  Vérif 90j .................... 9.7 / 280.0   ✅
  Vérif 12m .................... 9.7 / 900.0   ✅

  BESOIN OPÉRATIONNEL
  ────────────────────────────────────────────────────────────
  Blocs / mois ................. 404
  HDV / mois ................... 212.9 HDV
  s (étapes/rotation) .......... 4
  Besoin ....................... 204 jours-pilote

  MARGE
  ════════════════════════════════════════════════════════════
  ▐ JOURS-PILOTE .............. +163                        ▐
  ▐ HDV ....................... +1 535                       ▐
  ▐ TAUX D'ENGAGEMENT ........ 55.6%                        ▐
  ▐ STATUT ................... ✅ CONFORTABLE                ▐
  ════════════════════════════════════════════════════════════

  ANALYSES
  ────────────────────────────────────────────────────────────
  Marge ETP .................... +7.8 pilotes
  Blocs absorbables ............ +326
  Effectif minimum ............. 13 pilotes
  Excédent ..................... +9 pilotes

  PROGRAMME DÉTAILLÉ
  ────────────────────────────────────────────────────────────
  Ligne       Blocs/mois  HDV/mois
  NOU-LIF          120      60.2
  NOU-MAR          103      59.8
  NOU-UVE           43      28.8
  NOU-TGJ           34      14.4
  NOU-KNQ           43      21.5
  NOU-ILP           43      18.1
  NOU-BMY           17      10.0
  ─────────────────────────────────
  TOTAL             404     212.9
```

### 8.2 Codes de retour

| Code | Signification |
|---|---|
| `0` | Calcul terminé, statut CONFORTABLE |
| `1` | Calcul terminé, statut TENDU |
| `2` | Calcul terminé, statut CRITIQUE |
| `10` | Erreur : fichier config introuvable |
| `11` | Erreur : paramètres invalides (α hors bornes, division par zéro…) |
| `12` | Erreur : dépassement de butée cumulative |

---

## 9. Options de ligne de commande

```
MargeEngagement.exe [options]

Options:
  -c, --config <path>       Fichier JSON de paramètres (défaut: parametres.json)
  -o, --output <path>       Exporter le résultat en CSV
  -s, --scenario <key=val>  Modifier un paramètre pour test what-if
                            ex: --scenario cdb=14 --scenario etapes=5
  -q, --quiet               Sortie minimale (tau + statut uniquement)
  -v, --verbose             Afficher le détail par ligne et par butée
  --json                    Sortie au format JSON (pour intégration pipeline)
  --help                    Afficher l'aide
```

### Exemples d'utilisation

```bash
# Calcul standard
MargeEngagement.exe -c parametres_mars2026.json

# Scénario what-if : et si on passe à 14 CDB ?
MargeEngagement.exe -c parametres.json --scenario cdb=14

# Export JSON pour intégration Synaxis
MargeEngagement.exe -c parametres.json --json > marge_result.json

# Mode silencieux pour script batch
MargeEngagement.exe -c parametres.json -q
# Sortie: 55.6% CONFORTABLE
```

---

## 10. Tests unitaires attendus

### 10.1 Cas nominaux

| Test | Entrée | Résultat attendu |
|---|---|---|
| Calcul standard | Params par défaut (22 pilotes, programme type) | τ ≈ 55%, CONFORTABLE |
| Effectif réduit | 14 pilotes, même programme | τ > 85%, TENDU ou CRITIQUE |
| Aucun vol | Programme vide | M = C_net, τ = 0% |
| Compteurs chargés | cumul28 = 80h | h_max = 20h, contrainte mordante 28j |

### 10.2 Cas limites

| Test | Entrée | Résultat attendu |
|---|---|---|
| D_dispo = 0 | J_off = 31 | Erreur : aucun jour engageable |
| Alpha négatif | Abattements > C_brute | Erreur : paramétrage incohérent |
| h_max ≤ 0 | cumul28 = 105 | Alerte : butée 28j déjà dépassée |
| Division par zéro | s = 0 | Erreur gérée proprement |
| Déséquilibre CDB/OPL | 2 CDB, 20 OPL | Alerte insuffisance CDB |

### 10.3 Tests de non-régression

Vérifier que les résultats du moteur C# correspondent exactement aux valeurs du tableur Excel de référence (Marge_Engagement_V2.xlsx) pour le jeu de données par défaut.

---

## 11. Évolutions futures

| Priorité | Évolution | Description |
|---|---|---|
| P1 | Multi-période | Calculer sur 3/6/12 mois avec report automatique des compteurs cumulatifs |
| P1 | Import ORLANDO/SPS | Parser les exports programme de vols depuis les EFB |
| P2 | Profil individuel | Compteurs par pilote (pas uniquement moyenne) pour détecter les pilotes saturés |
| P2 | Intégration Synaxis | Injecter les résultats dans le système de crew management |
| P3 | Optimisation OR-Tools | Remplacer le calcul macro par un solveur de crew pairing |
| P3 | Interface web Blazor | Dashboard interactif pour le RDOV |

---

## 12. Références

1. **Akyurt, İ.Z. et al.** (2021). *A new mathematical model for determining optimal workforce planning of pilots in an airline company*. Complex & Intelligent Systems, 8(1):429-441.
2. **Kasirzadeh, A. et al.** (2015). *Airline crew scheduling: models, algorithms, and data sets*. EURO Journal on Transportation and Logistics.
3. **ICF** (2018). *Why Airlines Are Running Out of Pilots* — Productivity & pilot-to-aircraft ratio analysis.
4. **NBAA** Management Guide — Pilot staffing formulas (days-needed & flight-hours methods).
5. **EASA** ORO.FTL.210-235 — Flight Time Limitations (applicable via SEAC en NC).
6. **Deveci, M. & Demirel, N.C.** (2018). *A survey of the literature on airline crew scheduling*. Engineering Applications of AI, 74:54-69.
