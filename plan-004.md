# plan-004 : Migration de Periode vers Programme.xml

## Contexte

`Parametres.xml` doit ne contenir que des règles réglementaires et conventions (ORO.FTL, Délib 77, conventions Air Calédonie). La `Periode` (mois, année, nbJours) est un paramètre d'analyse, pas une règle — elle doit vivre dans `Programme.xml` avec le programme de vol.

Vision future : Parametres = règles, Programme = programme de vol, Équipage = données crew (à venir).

## Modifications

### 1. `IO/XmlConfigLoader.cs`

**1a. `SauvegarderParametres` (L215-218)** — retirer `<Periode>` :
```csharp
// Supprimer ces 4 lignes :
new XElement("Periode",
    new XAttribute("mois", config.Periode.Mois),
    new XAttribute("annee", config.Periode.Annee),
    new XAttribute("nbJours", config.Periode.NbJours)),
```

**1b. `SauvegarderProgramme` (L259)** — ajouter `<Periode>` comme premier enfant de `<Programme>` :
```csharp
new XElement("Programme",
    new XElement("Periode",
        new XAttribute("mois", config.Periode.Mois),
        new XAttribute("annee", config.Periode.Annee),
        new XAttribute("nbJours", config.Periode.NbJours)),
    BuildCatalogueVols(...),
```

**1c. `ChargerProgramme` (après L142)** — parser `<Periode>` si présent :
```csharp
var per = root.Element("Periode");
if (per != null)
{
    config.Periode.Mois = Attr(per, "mois", "");
    config.Periode.Annee = IntAttr(per, "annee");
    config.Periode.NbJours = IntAttr(per, "nbJours");
}
```

**1d. `MergerParametres` (L182)** — retirer `cible.Periode = source.Periode;`

**1e. `MergerProgramme` (L196)** — ajouter `cible.Periode = source.Periode;` en première ligne

### 2. `Tui/TuiApp.cs` (L296)

Ajouter `ConfigFileType.Programme` à la condition d'affichage :
```csharp
if (fileType is ConfigFileType.Parametres or ConfigFileType.Programme or ConfigFileType.Legacy)
```

### 3. Fichier `Config/Parametres.xml`

Retirer l'élément `<Periode>` s'il existe.

### 4. Fichier `Config/Programme.xml`

Ajouter `<Periode mois="Mars" annee="2026" nbJours="31" />` comme premier enfant de `<Programme>`.

## Fichiers NON modifiés

- `Models/Configuration.cs` — aucun changement au modèle
- `Commands/CommandHandler.cs` — `AutoSave()` sauvegarde déjà les 2 fichiers
- `Tui/FormOverlay.cs` — section PERIODE reste dans F2 (formulaire scalaire)
- `Services/CalculateurMarge.cs`, `CalendrierHelper.cs` — consomment `config.Periode` inchangé

## Rétrocompatibilité

- `ParseParametresInto` continue de parser `<Periode>` si présent → anciens fichiers lisibles
- L'ordre de merge (Parametres puis Programme) fait que Programme.xml a le dernier mot
- Le premier `AutoSave` redistribue correctement

## Vérification

1. `dotnet build` — 0 erreurs
2. Charger les fichiers existants → `calc` → résultat identique
3. Vérifier `Parametres.xml` sauvegardé sans `<Periode>`
4. Vérifier `Programme.xml` sauvegardé avec `<Periode>`
