# plan-005 : Intégration des données équipage depuis exports Excel XML

## Contexte

L'application utilise un `Effectif` statique (CDB=12, OPL=10, CC=8, PNC=6). La vision est de charger les données réelles d'équipage depuis les exports du système AIMS (4 fichiers Excel XML Spreadsheet) pour :
- Disposer de la liste nominative des membres d'équipage
- Suivre le statut des qualifications/checks par personne
- Dériver l'effectif automatiquement depuis la crew list

## Fichiers source (Config/)

| Fichier | Contenu | Lignes données |
|---|---|---|
| `PntCrewList.xml` | 20 PNT (pilotes), 15 colonnes | CDB + OPL |
| `PncCrewList.xml.xml` | ~19 PNC (cabin crew), même format | CC + PNC |
| `CrewCheckStatus.xml` | ~10 PNT, 27 types de checks, dates + couleurs | Statuts qualifications |
| `Check Description.xml` | 47 définitions de checks, validité/renouvellement | Référentiel |

Format commun : Excel XML Spreadsheet (`<Workbook>` ns `urn:schemas-microsoft-com:office:spreadsheet`).

## Plan d'implémentation (7 étapes)

### Étape 1 — Modèle `Models/Equipage.cs` (nouveau fichier)

Enums : `TypeContrat` (PNT/PNC), `Grade` (CDB/OPL/CC/PNC), `StatutCheck` (Valide/ExpirationProche/Avertissement/Expire/NonApplicable), `GroupeCheck` (Cockpit/Cabine)

Classes :
- **`MembreEquipage`** : Id (Guid), Code (3 lettres), Nom, Actif, Contrat, Grade, Matricule, DateEntree, DateFin, Roles (List\<string\>, split sur "+"), Categorie, ReglesApplicables (List\<string\>), Bases (List\<string\>), TypeAvion, Qualifications (List\<StatutQualification\>)
- **`StatutQualification`** : CodeCheck, DateExpiration (DateTime?), Statut (StatutCheck)
- **`DefinitionCheck`** : Id (Guid), Code, Description, Primaire, Groupe, ValiditeNombre, ValiditeUnite, FinDeMois, FinDAnnee, RenouvellementNombre, RenouvellementUnite, AvertissementNombre, AvertissementUnite
- **`DonneesEquipage`** : DateExtraction, Membres (List\<MembreEquipage\>), Checks (List\<DefinitionCheck\>), propriétés calculées NbCdb/NbOpl/NbCc/NbPnc, méthode `CalculerEffectif()`

### Étape 2 — Parseur Excel XML `IO/ExcelXmlParser.cs` (nouveau fichier)

Utilitaire générique réutilisable :
- `record ExcelCell(string? Value, string? DataType, string? StyleId)`
- `ParseWorksheet(chemin, worksheetIndex)` → `List<List<ExcelCell>>` — gère `ss:MergeAcross`, `ss:StyleID`, `ss:Type`
- `ParseStyleColors(chemin)` → `Dictionary<string, string>` (StyleID → couleur hex)
- Helpers : `ParseDateTime`, `CouleurVersStatut`, `CellValue`, `CellBool`

Namespace XML : `urn:schemas-microsoft-com:office:spreadsheet` (préfixe `ss`)

Mapping couleurs → statuts :
- `#00FF00` (vert) → Valide
- `#FF9900` (orange) → ExpirationProche
- `#FFFF00` (jaune) → Avertissement
- `#FF99CC` (rose) → Expiré
- Blanc/vide → NonApplicable

### Étape 3 — Loader `IO/EquipageLoader.cs` (nouveau fichier)

`Charger(cheminPnt?, cheminPnc?, cheminCheckStatus?, cheminCheckDesc?)` → `DonneesEquipage`

Pipeline :
1. Charger Check Descriptions (row 0-1 = headers, data à partir de row 2, 21 colonnes)
2. Charger PNT crew list (row 0 = titre, row 1 = headers, data row 2+)
3. Charger PNC crew list (même format) — **déduplication par Code** (cas LAURANS LRS présent dans les 2 listes, garder contrat PNT, fusionner rôles)
4. Charger Check Statuses — headers row 1 au format `"DESCRIPTION(CODE)"`, extraire code entre parenthèses ; mapper StyleID → couleur → statut

Colonnes CrewList (index 0-14) : S, Crew name, Crew code, Department, R for Seniority, Seniority, Crew Number, Joined on, Available Until, Crew Role, Cat., Rules Set, Crew Base, A/C type, Contract Type

Colonnes Check Description (index 0-20) : Code, Description, Primary, Seat, Crew Role Group, Sort, Disabled, Auto Ack, Crew Check Alert, Crew Check Group, #validité, Unité, Descr, FinMois, FinAnnée, #renouvellement, Unité, Descr, #avertissement, Unité, Descr

Date d'extraction parsée depuis le titre : `"CREW LIST [AVAILABLE FROM:16/02/2026 CATEGORY:Pilot]"`

### Étape 4 — Intégration `Configuration` + `XmlConfigLoader`

**`Models/Configuration.cs`** : ajouter `public DonneesEquipage? Equipage { get; set; }`

**`IO/XmlConfigLoader.cs`** :
- Ajouter `Equipage` dans `ConfigFileType`
- `DetecterType` : si root = `"Workbook"` → retourner `ConfigFileType.Unknown` (les fichiers Excel XML ne sont pas chargés automatiquement, seulement via `import crew`)
- Ajouter `SauvegarderEquipage(chemin, equipage)` — format XML normalisé `<Equipage>/<Checks>/<Membres>/<Qualification>`
- Ajouter `ChargerEquipage(chemin)` — relit le format normalisé
- Ajouter `MergerEquipage(cible, source)`

Format XML normalisé (sauvegarde) :
```xml
<Equipage dateExtraction="2026-02-16">
  <Checks>
    <Check id="..." code="CRM" description="CRM" primaire="true" groupe="Cockpit"
           validite="1" validiteUnite="Year(s)" />
  </Checks>
  <Membres>
    <Membre id="..." code="PLL" nom="PELOILLE Julien" contrat="PNT" grade="CDB" ...>
      <Qualification check="CRM" expiration="2026-12-31" statut="Valide" />
    </Membre>
  </Membres>
</Equipage>
```

### Étape 5 — `Commands/CommandHandler.cs` — nouvelles commandes

| Commande | Description |
|---|---|
| `import crew [dossier]` | Charge les 4 fichiers Excel XML depuis un dossier (auto-détection par pattern `*PntCrewList*`, `*PncCrewList*`, `*CheckStatus*`, `*CheckDesc*`) |
| `show crew` | Affiche la liste des membres (Code, Nom, Grade, Matricule, Entrée, Rôles) |
| `show checks [code]` | Affiche le statut des qualifications (tableau croisé crew × checks) |
| `set effectif auto` | Dérive l'effectif depuis l'équipage chargé |

AutoSave : ajouter `_equipagePath` et sauvegarde en format normalisé `Equipage.xml`.

### Étape 6 — `Program.cs` — arguments CLI

Ajouter `--crew <dossier>` pour charger l'équipage au démarrage. Texte d'aide à mettre à jour.

### Étape 7 — Validation et nettoyage

Vérifications :
- Membres uniques : ~38 (20 PNT + 19 PNC − 1 doublon LRS)
- Comptages : NbCdb ≈ 9, NbOpl ≈ 10, NbCc ≈ 13, NbPnc ≈ 4-6
- Sauvegarder en `Equipage.xml` → recharger → données identiques
- `calc` avec effectif statique = même résultat qu'avant (non-régression)

## Impact par fichier

| Fichier | Impact | Motif |
|---|---|---|
| `Models/Equipage.cs` | **Nouveau** | Modèle complet équipage |
| `IO/ExcelXmlParser.cs` | **Nouveau** | Parseur générique Excel XML |
| `IO/EquipageLoader.cs` | **Nouveau** | Chargement 4 fichiers AIMS |
| `Models/Configuration.cs` | Faible | Ajout `Equipage?` |
| `IO/XmlConfigLoader.cs` | Moyen | ConfigFileType + Charger/Sauvegarder Equipage |
| `Commands/CommandHandler.cs` | Moyen | `import crew`, `show crew`, `show checks` |
| `Program.cs` | Faible | Argument `--crew` |
| `Services/CalculateurMarge.cs` | **Zéro** | Inchangé |

## Points d'attention

- **Déduplication LRS** : LAURANS apparaît dans PNT et PNC (rôle RPN). Garder contrat PNT, fusionner rôles, compter une seule fois.
- **Double extension** : `PncCrewList.xml.xml` — le code accepte ce nom tel quel.
- **Espace dans le nom** : `Check Description.xml` — chemins entre guillemets.
- **Cellules style sans Data** : dans CrewCheckStatus, `ss:StyleID="ss010"` (rose) sans `<Data>` = attribut expiré sans date → `StatutCheck.Expire` avec `DateExpiration = null`.
- **Check Description row skip** : 2 lignes d'en-tête (sections fusionnées + colonnes), données à partir de row index 2.
