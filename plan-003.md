# plan-003 : Refactoring du modèle C# — Entités indépendantes avec relations par référence

## Contexte

Le modèle actuel utilise l'imbrication : `SemaineType` → `List<BlocVol>` → `List<Vol>`. Ce pattern pose des problèmes pour une future migration PostgreSQL :
- Duplication des vols et blocs (le vol 101 copié 6x, la rotation Lifou dupliquée 6 jours)
- `Jour` et `Sequence` mal placés (sur le BlocVol au lieu de la relation)
- Pas de catalogue de blocs indépendant

**Objectif** : transformer en 3 catalogues indépendants (Vols, Blocs, SemainesTypes) avec relations par référence M:N, prêts pour PostgreSQL.

## Nouveau modèle de données

### Entités

Toutes les entités utilisent `Guid` (UUID) comme clé primaire, compatible PostgreSQL.

```csharp
// ── Vol (catalogue, PK = Id) ──
public class Vol
{
    public Guid Id { get; set; } = Guid.NewGuid();   // UUID — PK pour PostgreSQL
    public string Numero { get; set; } = "";          // numéro commercial (PAS unique, cf vol 301)
    public string Depart { get; set; } = "";
    public string Arrivee { get; set; } = "";
    public string HeureDepart { get; set; } = "";
    public string HeureArrivee { get; set; } = "";
    public bool MH { get; set; }
    public double HdvVol => HeureHelper.CalculerDuree(HeureDepart, HeureArrivee);
}

// ── Étape d'un bloc (référence ordonnée → Vol) ──
public class EtapeVol
{
    public int Position { get; set; }              // ordre dans la rotation (1, 2, 3...)
    public Guid VolId { get; set; }                // FK → Vol.Id (UUID)
}

// ── BlocVol (catalogue, PK = Id, UK = Code) ──
public class BlocVol
{
    public Guid Id { get; set; } = Guid.NewGuid();   // UUID — PK pour PostgreSQL
    public string Code { get; set; } = "";            // clé naturelle affichage (ex: "ROT-LIF-AM")
    public string Periode { get; set; } = "";
    public string DebutDP { get; set; } = "";
    public string FinDP { get; set; } = "";
    public string DebutFDP { get; set; } = "";
    public string FinFDP { get; set; } = "";
    public List<EtapeVol> Etapes { get; set; } = [];  // références persistées (VolId = UUID)

    // ── Transient : hydratés au chargement ──
    public List<Vol> Vols { get; set; } = [];      // hydratés depuis CatalogueVols
    public string Jour { get; set; } = "";         // rempli depuis BlocPlacement
    public int Sequence { get; set; }              // rempli depuis BlocPlacement

    // ── Propriétés calculées (inchangées) ──
    // Nom, JourIndex, NbEtapes, HdvBloc, DuréeDP/FDP/TS/TSV : fonctionnent sur Vols hydratés
}

// ── Placement d'un bloc dans une semaine type (table de jonction) ──
public class BlocPlacement
{
    public Guid BlocId { get; set; }               // FK → BlocVol.Id (UUID)
    public string Jour { get; set; } = "";         // "Lundi".."Dimanche"
    public int Sequence { get; set; }              // ordre dans la journée
}

// ── SemaineType (modifiée, PK = Id, UK = Reference) ──
public class SemaineType
{
    public Guid Id { get; set; } = Guid.NewGuid();   // UUID — PK pour PostgreSQL
    public string Reference { get; set; } = "";       // clé naturelle affichage (ex: "BS_01")
    public string Saison { get; set; } = "";
    public List<BlocPlacement> Placements { get; set; } = [];  // remplace Blocs
}

// ── AffectationSemaine (modifiée : référence par UUID) ──
public class AffectationSemaine
{
    public int Semaine { get; set; }
    public int Annee { get; set; }
    public Guid SemaineTypeId { get; set; }        // FK → SemaineType.Id (remplace SemaineTypeRef string)
}

// ── Configuration (modifiée) ──
public class Configuration
{
    // ... paramètres existants inchangés ...
    public List<Vol> CatalogueVols { get; set; } = [];
    public List<BlocVol> CatalogueBlocs { get; set; } = [];     // NOUVEAU
    public List<SemaineType> SemainesTypes { get; set; } = [];
    public List<AffectationSemaine> Calendrier { get; set; } = [];
}
```

**Convention** : chaque entité a un `Id` (Guid, PK) + une clé naturelle humaine (`Numero`, `Code`, `Reference`). Les FK entre entités utilisent le `Guid Id`, jamais la clé naturelle. La clé naturelle sert uniquement à l'affichage et aux commandes utilisateur.

### Point Vol 301

Le vol 301 (NOU→KNQ) existe avec 2 horaires différents (15:00 PM et 10:30 AM). Le numéro de vol n'est donc **PAS** une clé unique. Chaque variante a son propre UUID :
```xml
<Vol id="a1b2c3d4-..." numero="301" depart="NOU" arrivee="KNQ" heureDepart="15:00" heureArrivee="15:30" />
<Vol id="e5f6a7b8-..." numero="301" depart="NOU" arrivee="KNQ" heureDepart="10:30" heureArrivee="11:00" />
```

### Nouveau format XML

Les UUID sont sérialisés comme attributs. Les FK (vol, bloc, ref) utilisent les UUID des entités référencées.

```xml
<Programme>
  <CatalogueVols>
    <Vol id="a1b2c3d4-e5f6-7890-abcd-ef1234567890" numero="101"
         depart="NOU" arrivee="LIF" heureDepart="07:00" heureArrivee="07:30" />
    <Vol id="b2c3d4e5-f6a7-8901-bcde-f12345678901" numero="102"
         depart="LIF" arrivee="NOU" heureDepart="08:00" heureArrivee="08:30" />
  </CatalogueVols>
  <CatalogueBlocs>
    <Bloc id="c3d4e5f6-a7b8-9012-cdef-123456789012" code="ROT-LIF-AM"
          periode="AM" debutDP="06:15" finDP="09:00" debutFDP="06:45" finFDP="08:30">
      <Etape position="1" vol="a1b2c3d4-e5f6-7890-abcd-ef1234567890" />
      <Etape position="2" vol="b2c3d4e5-f6a7-8901-bcde-f12345678901" />
    </Bloc>
  </CatalogueBlocs>
  <SemainesTypes>
    <SemaineType id="d4e5f6a7-b8c9-0123-defa-234567890123" reference="BS_01" saison="BASSE">
      <Placement sequence="1" jour="Lundi" bloc="c3d4e5f6-a7b8-9012-cdef-123456789012" />
      <Placement sequence="2" jour="Lundi" bloc="..." />
    </SemaineType>
  </SemainesTypes>
  <Calendrier>
    <Affectation semaine="10" annee="2026" ref="d4e5f6a7-b8c9-0123-defa-234567890123" />
  </Calendrier>
</Programme>
```

## Plan d'implémentation (9 étapes)

### Étape 1 — Extraire `HeureHelper` (nouveau fichier, zéro casse)

**Fichier** : créer `Helpers/HeureHelper.cs`

- Extraire de `BlocVol` les méthodes statiques : `ParseHeure`, `CalculerDuree`, `JourVersIndex`, `IndexVersJour`
- Dans `BlocVol`, déléguer vers `HeureHelper` (les appels `BlocVol.CalculerDuree()` existants restent valides)
- Dans `Vol.HdvVol`, remplacer `BlocVol.CalculerDuree` par `HeureHelper.CalculerDuree`
- **Test** : compilation + `calc` identique

### Étape 2 — Ajouter les nouvelles entités (additif, zéro casse)

**Fichier** : `Models/Configuration.cs`

- Ajouter `Vol.Id` (Guid, défaut `Guid.NewGuid()`)
- Ajouter `BlocVol.Id` (Guid, défaut `Guid.NewGuid()`)
- Ajouter `BlocVol.Code` (string, défaut "")
- Ajouter `SemaineType.Id` (Guid, défaut `Guid.NewGuid()`)
- Créer classe `EtapeVol` (Position int, VolId Guid)
- Ajouter `BlocVol.Etapes` (List\<EtapeVol\>, défaut [])
- Créer classe `BlocPlacement` (BlocId Guid, Jour string, Sequence int)
- Ajouter `SemaineType.Placements` (List\<BlocPlacement\>, défaut [])
- Ajouter `Configuration.CatalogueBlocs` (List\<BlocVol\>, défaut [])
- Modifier `AffectationSemaine` : ajouter `SemaineTypeId` (Guid), conserver temporairement `SemaineTypeRef` (string)
- **Conserver temporairement** `SemaineType.Blocs` et `BlocVol.Jour/Sequence`
- **Test** : compilation, zéro impact (nouvelles propriétés ignorées)

### Étape 3 — Créer `CatalogueResolver` (nouveau fichier, zéro casse)

**Fichier** : créer `Services/CatalogueResolver.cs`

```
CatalogueResolver.HydraterBlocs(config)
  → Pour chaque bloc du catalogue, résout Etapes → Vols depuis CatalogueVols

CatalogueResolver.ResoudreSemainesTypes(config)
  → Pour chaque SemaineType, résout Placements → crée des copies BlocVol
    avec Jour/Sequence du placement + Vols hydratés

CatalogueResolver.ResoudreTout(config)
  → Pipeline complet : HydraterBlocs puis ResoudreSemainesTypes
```

La résolution peuple une propriété transiente `List<BlocVol>` accessible pour le code downstream. `CalendrierHelper.ResoudreProgramme` peut ensuite continuer à fonctionner avec les blocs résolus.

- **Test** : unitaire sur données construites programmatiquement

### Étape 4 — Réécrire `XmlConfigLoader` (point de basculement)

**Fichier** : `IO/XmlConfigLoader.cs`

**Chargement** (nouveau format) :
1. `ParseCatalogueVols` : lit `<CatalogueVols>/<Vol id=... numero=... />`
2. Nouveau `ParseCatalogueBlocs` : lit `<CatalogueBlocs>/<Bloc code=...>/<Etape position=... vol=... />`
3. `ParseSemainesTypes` : lit `<SemaineType>/<Placement sequence=... jour=... bloc=... />` au lieu de `<Bloc>` imbriqué
4. Appeler `CatalogueResolver.ResoudreTout(config)` en fin de chargement

**Détection ancien format** : si `<SemainesTypes>/<SemaineType>` contient `<Bloc>` (pas `<Placement>`), convertir automatiquement :
- Extraire les vols uniques → CatalogueVols (générer `Id = Guid.NewGuid()`)
- Extraire les blocs uniques par signature (DP/FDP + vols) → CatalogueBlocs (générer `Id = Guid.NewGuid()`, `Code = "ROT-" + arrivée1 + "-" + période`)
- Convertir les blocs imbriqués → BlocPlacements (référence par `BlocVol.Id`)
- Générer `SemaineType.Id` et migrer `AffectationSemaine.SemaineTypeRef` → `SemaineTypeId`

**Sauvegarde** : toujours le nouveau format.

**Fichier** : `IO/ConfigLoader.cs` (validations) — adapter en parallèle :
- Vol.Id unique dans CatalogueVols
- BlocVol.Code unique dans CatalogueBlocs
- Chaque EtapeVol.VolId → Vol.Id existant
- Chaque BlocPlacement.BlocCode → BlocVol.Code existant
- st.Placements.Count > 0 (remplace st.Blocs.Count > 0)

- **Test** : charger ancien XML → sauvegarder → recharger → vérifier `calc` identique

### Étape 5 — Adapter `CalendrierHelper`

**Fichier** : `Services/CalendrierHelper.cs`

- `ResoudreProgramme` : itère sur `st.Placements`, résout chaque `BlocPlacement.BlocId` vers un BlocVol dans `config.CatalogueBlocs` (dict par Id), copie avec Jour/Sequence du placement. Remplace `blocs.AddRange(st.Blocs)`. Le lookup `AffectationSemaine` utilise `SemaineTypeId` (Guid) au lieu de `SemaineTypeRef` (string).
- `BlocsUniques` : retourne `config.CatalogueBlocs` au lieu de `SelectMany(st => st.Blocs)`
- **Test** : `calc` produit le même résultat

### Étape 6 — Adapter `CommandHandler`

**Fichier** : `Commands/CommandHandler.cs`

Nouvelles commandes (ou adaptation des existantes) :

| Commande | Avant | Après |
|---|---|---|
| `add vol` | (n'existait pas) | Ajoute dans CatalogueVols |
| `add bloc` | Créait BlocVol inline dans SemaineType | Crée dans CatalogueBlocs (+ optionnellement un placement) |
| `add placement` | (n'existait pas) | Ajoute BlocPlacement dans SemaineType |
| `del bloc` | Supprimait de st.Blocs | Supprime du CatalogueBlocs (+ tous ses placements) |
| `del placement` | (n'existait pas) | Supprime un placement d'une SemaineType |
| `del vol` | (n'existait pas) | Supprime de CatalogueVols (vérifie pas de référence dans blocs) |
| `set cal` | Inchangé | Inchangé |

Alternative ergonomique : garder `add bloc <stRef> <seq> <jour> <per> <dp> <fdp> <vols...>` avec la même syntaxe mais qui en interne (1) crée les vols manquants, (2) crée le bloc, (3) crée le placement.

### Étape 7 — Adapter `ProgrammeOverlay` (TUI)

**Fichier** : `Tui/ProgrammeOverlay.cs`

- Navigation : `SemainesTypes[i]` → `Placements[j]` → résoudre `BlocCode` → `Vols[k]`
- `CurrentBloc` : résout le placement courant vers le CatalogueBlocs
- Édition d'un bloc : modifie l'entrée catalogue (impacte tous les placements — comportement attendu). Afficher indicateur "(utilisé Nx)" dans le titre.
- Édition de Jour/Sequence : modifie le BlocPlacement, pas le catalogue
- Insert (ajout) : proposer de choisir parmi les blocs existants du catalogue
- Nouvelle vue `CatalogueBlocs` accessible par touche dédiée

### Étape 8 — Adapter `FormOverlay` et `ScreenRenderer`

**Fichiers** : `Tui/FormOverlay.cs`, `Tui/ScreenRenderer.cs`

- FormOverlay : adapter `AddBlocVolFields` et `RebuildSemainesTypes` pour le nouveau modèle
- ScreenRenderer : afficher Code du bloc, nombre de placements, vues catalogue

### Étape 9 — Nettoyage

- Supprimer `SemaineType.Blocs` si tout le code utilise `Placements` + résolution
- Ou le garder comme propriété transiente de commodité
- Supprimer l'ancien code de parsing XML imbriqué une fois la migration confirmée

## Impact par fichier

| Fichier | Impact | Motif |
|---|---|---|
| `Helpers/HeureHelper.cs` | Nouveau | Extraction utilitaires |
| `Services/CatalogueResolver.cs` | Nouveau | Hydratation catalogues |
| `Models/Configuration.cs` | **Fort** | Nouvelles entités, restructuration |
| `IO/XmlConfigLoader.cs` | **Fort** | Réécriture parse/build |
| `Commands/CommandHandler.cs` | **Fort** | Nouvelles commandes, logique CRUD |
| `Tui/ProgrammeOverlay.cs` | **Fort** | Navigation par placements |
| `IO/ConfigLoader.cs` | Moyen | Nouvelles validations |
| `Tui/FormOverlay.cs` | Moyen | Adaptation formulaires |
| `Tui/ScreenRenderer.cs` | Faible | Affichage Code, compteurs |
| `Services/CalendrierHelper.cs` | Faible | Résolution placements |
| `Services/CalculateurMarge.cs` | **Zéro** | Consomme List\<BlocVol\> résolu |
| `IO/ConsoleRenderer.cs` | **Zéro** | Consomme ResultatMarge |
| `Models/ResultatMarge.cs` | **Zéro** | Inchangé |

## Vérification

1. **Après étape 1-3** : compilation OK, tests unitaires CatalogueResolver
2. **Après étape 4** : charger `programme.xml` actuel (auto-détection ancien format) → sauvegarder en nouveau format → recharger → `calc` identique au résultat actuel
3. **Après étape 5** : `show programme` affiche le même programme résolu
4. **Après étape 6-8** : tester toutes les commandes TUI (add/del bloc, set cal, navigation ProgrammeOverlay)
5. **Final** : comparer le `ResultatMarge` avant/après refactoring — doit être strictement identique
