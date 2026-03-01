# CrewSizer v2 — Module 1 : Dimensionnement CP-SAT

> Prototype OR-Tools prêt à intégrer dans la solution Clean Architecture.

## Structure

```
CrewSizer.sln
├── src/
│   ├── CrewSizer.Domain/           ← Enums, Entities, Interfaces (zéro dépendance)
│   ├── CrewSizer.Application/      ← ISizingSolver, SizingService, DTOs
│   └── CrewSizer.Infrastructure/   ← OrToolsSizingSolver (Google.OrTools CP-SAT)
└── tests/
    └── CrewSizer.Infrastructure.Tests/  ← 11 tests unitaires xUnit
```

## Prérequis

- .NET 9 SDK
- NuGet : `Google.OrTools 9.12.4544`

## Build & Test

```bash
dotnet restore
dotnet build
dotnet test --verbosity normal
```

## Intégration dans ta solution existante

### 1. Copier les fichiers dans tes projets existants

| Fichier source | Destination |
|---|---|
| `Domain/Enums/CrewEnums.cs` | → `CrewSizer.Domain/Enums/` (si pas déjà présent) |
| `Domain/Entities/Entities.cs` | → fusionner avec tes entités existantes |
| `Domain/Interfaces/Repositories.cs` | → `CrewSizer.Domain/Interfaces/` |
| `Application/Sizing/*` | → `CrewSizer.Application/Sizing/` |
| `Infrastructure/Solver/OrToolsSizingSolver.cs` | → `CrewSizer.Infrastructure/Solver/` |
| `Infrastructure/DependencyInjection.cs` | → fusionner avec ton DI existant |

### 2. Ajouter le NuGet dans Infrastructure

```xml
<PackageReference Include="Google.OrTools" Version="9.12.4544" />
```

### 3. Enregistrer dans Program.cs

```csharp
builder.Services.AddSolverServices();
```

### 4. Appeler depuis un composant Blazor

```csharp
@inject SizingService SizingService

var result = await SizingService.ComputeAsync(
    CrewCategory.PNT,
    new DateOnly(2026, 3, 1),
    new DateOnly(2026, 3, 31));

if (result.IsFeasible)
{
    // Afficher MinimumCrewByRank, MarginByRank, CriticalDays...
}
```

## Modèle CP-SAT — Variables & Contraintes

### Variables
| Variable | Type | Description |
|---|---|---|
| `assign[c,d,b]` | BoolVar | Navigant c affecté au bloc b le jour d |
| `works[c,d]` | BoolVar | Navigant c travaille le jour d |
| `isUsed[c]` | BoolVar | Navigant c utilisé au moins une fois |

### Contraintes FTL (EASA ORO.FTL)
| Contrainte | Formulation CP-SAT |
|---|---|
| Couverture blocs | `sum(assign[CDB,d,b]) == 1`, `sum(assign[OPL,d,b]) == 1` |
| Max 1 bloc/jour | `sum(assign[c,d,*]) <= 1` |
| Indisponibilités | `assign[c,d,*] == 0` si navigant en congé |
| Temps de service 7j | `sum(duty × assign) ≤ 3600 min` (fenêtre glissante) |
| Temps de service 14j | `sum(duty × assign) ≤ 6600 min` |
| Temps de service 28j | `sum(duty × assign) ≤ 11400 min` |
| HDV 28j | `sum(block_time × assign) ≤ 6000 min` |
| Jours OFF 28j | `sum(works[c,d]) ≤ 20` (= 28 - 8 OFF) |
| Repos minimum | `assign[c,d,b1] + assign[c,d+1,b2] ≤ 1` si repos < 12h |
| **Repos 2j local / max 6j ON** | **C11a: sum(works) ≤ 6 dans fenêtre 7j ; C11b: ON-OFF-ON interdit** |
| **Repos 36h + 2 nuitées / sem. civile** | **Par semaine lun→dim : 2 jours consécutifs OFF** |
| **Repos mensuel 3j + week-end** | **1×/mois calendaire : Ven+Sam+Dim ou Sam+Dim+Lun OFF** |

### Objectif
```
Minimize sum(isUsed[c]) → effectif minimum requis
```

## Tests unitaires — 11 scénarios

| # | Scénario | Assertion |
|---|---|---|
| 1 | Basse saison 1 semaine | Feasible, CDB+OPL trouvés |
| 2 | Mois complet 28j | Feasible, contrainte mordante identifiée |
| 3 | Minimum requis | ≥ 2 CDB, ≥ 2 OPL |
| 4 | Haute vs basse saison | Haute saison requiert ≥ basse saison |
| 5 | Effectif insuffisant | Infeasible (1 CDB + 1 OPL) |
| 6 | Zéro OPL | Infeasible |
| 7 | 3 CDB en congé | Toujours feasible (7 restants) |
| 8 | 9 CDB en congé | Infeasible (1 restant < 2 requis) |
| 9 | Effectif serré | Jours critiques identifiés |
| 10 | Jour sans vol | Pas d'affectation |
| 11 | FTL strictes | Plus de navigants requis |
| 12 | Repos insuffisant | Pas de double affectation soir→matin |
| 13 | Contrainte mordante | Identifiée sur mois complet |
| 14 | Marge positive | Avec effectif complet |
| 15 | Performance | < 30s sur mois complet |
| 16 | Cohérence affectations | 1 CDB + 1 OPL par bloc |
| 17 | Pas de doublon | Aucun navigant sur 2 blocs le même jour |

## Prochaines étapes

- **Module 2** : Rostering CP-SAT (le plus complexe — contraintes FTL complètes)
- **Module 3** : Planification campagnes simulateur
- **Module 4** : Validation congés (test de faisabilité)
