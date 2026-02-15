# Plan-002 : Migration vers BlocVol + Table FTL + Limites Duty

## Contexte

L'audit plan-001 a validé la méthodologie mais identifié une lacune significative : les limites de temps de service (duty) ne sont pas modélisées. En parallèle, l'utilisateur souhaite remplacer le modèle d'entrée "lignes de vol" (fréquence) par des "blocs de vol" (rotations complètes) organisés en planning hebdomadaire, avec vérification du TSV max selon la table EU-OPS (nb étapes + heure de début).

## Changements confirmés par l'utilisateur

- **Bloc type** = rotation complète (ex: NOU-LIF-NOU), occurrence unique par jour
- **Programme** = planning hebdomadaire type x SemainesMois
- **HDV** = dérivé des étapes (chaque étape a son HdvEtape)
- **Table TSV max** = f(nb étapes, heure début) - table complète EU-OPS paramétrable
- **Limites duty** = 60h/7j, 110h/14j, 190h/28j - à implémenter

---

## Phase 1 : Modèle de données (`Models/Configuration.cs`)

### Supprimer
- Classe `LigneVol` (lignes 80-93)
- `Effectif.EtapesParRotation` (ligne 32) - sera dérivé du programme

### Ajouter les nouvelles classes

```csharp
public class Etape
{
    public string Depart { get; set; } = "";
    public string Arrivee { get; set; } = "";
    public double HdvEtape { get; set; }
}

public class BlocVol
{
    public string Nom { get; set; } = "";
    public int Jour { get; set; }           // 1=Lun..7=Dim
    public string DebutTS { get; set; } = "";   // "HH:mm"
    public string FinTS { get; set; } = "";
    public string DebutTSV { get; set; } = "";
    public string FinTSV { get; set; } = "";
    public List<Etape> Etapes { get; set; } = [];

    // Propriétés calculées
    [JsonIgnore] public int NbEtapes => Etapes.Count;
    [JsonIgnore] public double HdvBloc => Etapes.Sum(e => e.HdvEtape);
    [JsonIgnore] public double DureeTSHeures => (ParseHeure(FinTS) - ParseHeure(DebutTS)).TotalHours;
    [JsonIgnore] public double DureeTSVHeures => (ParseHeure(FinTSV) - ParseHeure(DebutTSV)).TotalHours;
    [JsonIgnore] public string JourNom => Jour switch {
        1=>"Lun",2=>"Mar",3=>"Mer",4=>"Jeu",5=>"Ven",6=>"Sam",7=>"Dim",_=>"?"};

    private static TimeSpan ParseHeure(string hhmm) { ... }
}

public class EntreeTsvMax
{
    public string DebutBande { get; set; } = "";  // "HH:mm"
    public string FinBande { get; set; } = "";
    public Dictionary<int, double> MaxParEtapes { get; set; } = new();
}

public class LimitesTempsService
{
    public double Max7j { get; set; } = 60;
    public double Max14j { get; set; } = 110;
    public double Max28j { get; set; } = 190;
}
```

### Modifier `Configuration`

```csharp
public List<BlocVol> Programme { get; set; } = [];  // remplace List<LigneVol>
public double SemainesMois { get; set; } = 4.3;      // NEW (déplacé de LigneVol)
public List<EntreeTsvMax> TableTsvMax { get; set; } = [];  // NEW
public LimitesTempsService LimitesTempsService { get; set; } = new();  // NEW
```

---

## Phase 2 : Modèle de sortie (`Models/ResultatMarge.cs`)

### Ajouter après ligne 48 (avant Alertes)

```csharp
// Vérifications FTL (NEW)
public List<VerifTsvMax> VerificationsTSV { get; set; } = [];
public bool TousBlocsConformesTSV { get; set; } = true;
public VerifTempsService VerifTempsServicePNT { get; set; } = new();
public VerifTempsService VerifTempsServicePNC { get; set; } = new();
public List<(int jour, string jourNom, int nbBlocs, double hdv, double ts)> ResumeSemaine { get; set; } = [];
public double SemainesMois { get; set; }
```

Avec les classes :
- `VerifTsvMax` : par bloc (nom, jour, nbEtapes, tsvDuree, tsvMaxAutorise, conforme)
- `VerifTempsService` : par groupe (totalTS semaine/mois, vérif 7j/14j/28j avec tuple valeur/limite/ok)

---

## Phase 3 : Moteur de calcul (`Services/CalculateurMarge.cs`)

### 3a. Remplacer l'agrégation programme (lignes 34, 59-73)

- `EtapesParRotation` : dérivé de `Programme.Average(b => b.NbEtapes)` au lieu de lu depuis config
- Agrégation : grouper les blocs par nom, calculer totaux hebdo, multiplier par `SemainesMois`
- `DetailProgramme` : alimenté par groupement par nom (rétro-compatible avec renderers)
- `fTotal` et `bHdv` : totaux mensuels dérivés des totaux hebdo x SemainesMois
- `ResumeSemaine` : totaux par jour de semaine

### 3b. Vérification TSV max (NEW)

Pour chaque bloc du programme :
1. Lookup dans `TableTsvMax` : trouver la bande horaire correspondant à `DebutTSV`
2. Chercher le TSV max pour `NbEtapes` dans cette bande
3. Vérifier `DureeTSVHeures <= tsvMax`
4. Si non conforme : ajouter alerte + marquer `TousBlocsConformesTSV = false`

### 3c. Vérification limites duty (NEW)

- Calculer TS total hebdomadaire = `Programme.Sum(b => b.DureeTSHeures)`
- Par personne (distribution égale) : `tsPerPersonne7j = tsHebdo / effectif`
- Projeter sur 14j (x2) et 28j (x4)
- Vérifier contre `LimitesTempsService.Max7j/14j/28j`
- Alertes si dépassement

### 3d. Alertes (modifier `GenererAlertes`)

Ajouter alertes pour :
- TSV max dépassé par bloc (avec détail : nom, jour, valeurs)
- Limites duty dépassées PNT et PNC (7j, 14j, 28j)

---

## Phase 4 : Persistance

### `IO/ConfigLoader.cs`
- `Valider()` : supprimer validation EtapesParRotation, ajouter validation BlocVol (jour 1-7, au moins 1 étape), SemainesMois > 0
- `Charger()`/`Sauvegarder()` : pas de changement (System.Text.Json gère automatiquement)

### `IO/XmlConfigLoader.cs`
- Remplacer `ParseProgramme()` : parser `<Bloc>` avec `<Etape>` enfants
- Remplacer `BuildProgramme()` : sérialiser la nouvelle structure
- Ajouter parsing `SemainesMois`, `TableTsvMax`, `LimitesTempsService`

### `Config/parametres.json`
- Réécrire complètement avec la structure BlocVol + TableTsvMax + LimitesTempsService
- Exemple de programme : blocs avec étapes, un par jour/rotation

---

## Phase 5 : Commandes TUI (`Commands/CommandHandler.cs`)

- `show programme` : affichage grille hebdomadaire (jour, bloc, TS, TSV, étapes, HDV)
- `add bloc <nom> <jour> <debutTS> <finTS> <debutTSV> <finTSV> <dep-arr:hdv> [...]` : remplace `add ligne`
- `del bloc <n>` : remplace `del ligne`
- `set semmois <val>` : nouveau, remplace `set etapes`
- Supprimer `set etapes`

---

## Phase 6 : Formulaire TUI (`Tui/FormOverlay.cs`)

- Supprimer `AddLigneVolFields`, remplacer par `AddBlocVolFields` (nom, jour, TS, TSV, + sous-champs étapes)
- Ajouter champ `SemainesMois` dans la section programme
- Supprimer `EtapesParRotation` de la section effectif
- Mettre à jour `ApplyTo()` pour reconstruire `List<BlocVol>` depuis les champs formulaire
- Mettre à jour `AddNewListItem` case "prog" pour les champs BlocVol

---

## Phase 7 : Renderers (optionnel, si temps)

Les renderers (`ConsoleRenderer.cs`, `TuiRenderer.cs`) fonctionnent sans modification car `DetailProgramme` garde le même format tuple. Optionnellement, ajouter :
- Section "VERIFICATIONS FTL" avec détail par bloc
- Section "TEMPS DE SERVICE" avec vérification duty

---

## Phase 8 : Help + nettoyage

- `Tui/HelpContent.cs` : mettre à jour aide pour add/del/show bloc
- Supprimer tout code mort lié à `LigneVol`

---

## Ordre d'exécution

```
Phase 1 (modèle) -> Phase 2 (sortie) -> Phase 3 (calcul) -> compilation OK
                                      -> Phase 4 (persistance)
                                      -> Phase 5 (commandes)
                                      -> Phase 6 (formulaire)
                                      -> Phase 7 (renderers)
                                      -> Phase 8 (help + nettoyage)
```

Phases 4-6 sont indépendantes entre elles, toutes dépendent de 1-3.

## Table TSV max EU-OPS par défaut

| Bande horaire | 1 étape | 2 | 3 | 4 | 5 | 6 | 7 | 8+ |
|---------------|---------|------|------|------|------|------|------|------|
| 06:00-13:29 | 13.00 | 12.75 | 12.50 | 12.25 | 12.00 | 11.75 | 11.50 | 11.25 |
| 13:30-17:59 | 12.25 | 12.00 | 11.75 | 11.50 | 11.25 | 11.00 | 10.75 | 10.50 |
| 18:00-21:59 | 11.00 | 10.75 | 10.50 | 10.25 | 10.00 | 9.75 | 9.50 | 9.25 |
| 22:00-05:59 | 11.00 | 10.75 | 10.50 | 10.25 | 10.00 | 9.75 | 9.50 | 9.25 |

Stockée dans `tableTsvMax` en JSON, modifiable par l'utilisateur.

## Vérification

1. `dotnet build` passe sans erreur
2. `dotnet run -- -c Config/parametres.json` produit un rapport cohérent
3. Mode TUI : `show programme` affiche la grille hebdo
4. `add bloc` / `del bloc` fonctionnent
5. `calc` montre les vérifications TSV et duty dans les alertes
6. Round-trip JSON : load -> save -> reload = identique
7. Les totaux mensuels (blocs, HDV) sont cohérents avec l'ancien modèle

## Fichiers modifiés

| Fichier | Type de changement |
|---------|-------------------|
| `Models/Configuration.cs` | MAJEUR - nouveau modèle |
| `Models/ResultatMarge.cs` | MODÉRÉ - nouveaux champs |
| `Services/CalculateurMarge.cs` | MAJEUR - agrégation + vérifications |
| `IO/ConfigLoader.cs` | MINEUR - validation |
| `IO/XmlConfigLoader.cs` | MODÉRÉ - parse/build programme |
| `Commands/CommandHandler.cs` | MAJEUR - commandes bloc |
| `Tui/FormOverlay.cs` | MAJEUR - formulaire bloc |
| `Tui/HelpContent.cs` | MINEUR - texte aide |
| `Config/parametres.json` | MAJEUR - nouveau format |
| `IO/ConsoleRenderer.cs` | AUCUN (rétro-compatible) |
| `IO/TuiRenderer.cs` | AUCUN (rétro-compatible) |
