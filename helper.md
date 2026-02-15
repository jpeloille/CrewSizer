# CrewSizer - Reference des commandes

## calc

Lancer le calcul de marge d'engagement et afficher le rapport complet :
capacite, besoin, marge par categorie (CDB, OPL, CC, PNC),
verifications cumulatives, TSV max et duty.

```
calc
```

Le calcul utilise la configuration courante.
Modifiez les parametres avec `set` avant de relancer `calc` pour des scenarios what-if.

---

## edit

Ouvrir le formulaire de saisie interactif.
**Raccourci clavier : F2**

Permet de modifier tous les parametres de la configuration.

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner champ |
| PgUp/PgDn | Section suivante |
| Enter | Editer le champ |
| Suppr | Supprimer element |
| F2 | Valider et fermer |
| Esc | Annuler et fermer |

---

## prog

Ouvrir le gestionnaire de programme.
**Raccourci clavier : F3**

Navigation hierarchique a 7 niveaux :

### Semaines Types (niveau 1)

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner semaine type |
| Fleches gauche/droite | Changer la saison (BASSE/HAUTE) |
| Enter | Voir les blocs |
| Insert | Ajouter une semaine type |
| Suppr | Supprimer la semaine type |
| C | Ouvrir le catalogue des vols types |
| Tab | Basculer vers le calendrier |
| Esc | Fermer |

### Blocs (niveau 2)

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner bloc |
| Enter | Editer le bloc |
| Insert | Ajouter un bloc |
| Suppr | Supprimer le bloc |
| Esc | Retour aux semaines types |

### Bloc Edit (niveau 3)

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner champ |
| Fleches gauche/droite | Cycler selecteur (Jour, Periode) |
| Enter | Editer champ texte |
| Enter (sur Vols) | Ouvrir la liste des vols du bloc |
| Esc | Retour a la table des blocs |

### Vols du bloc (niveau 4)

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner vol |
| Enter | Editer le vol |
| Insert | Ajouter depuis le catalogue |
| M | Toggle MH (modification horaire) |
| Suppr | Supprimer le vol du bloc |
| Esc | Retour a l'edition du bloc |

### Vol Edit (niveau 5)

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner champ |
| Enter | Editer le champ |
| Esc (en edition) | Annuler |
| Esc (hors edition) | Retour a la liste des vols |

### Catalogue vols types (via C depuis niveau 1)

Le catalogue est le referentiel des vols avec leurs horaires standard.
Accessible aussi en mode selection depuis la liste des vols (Insert).

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner vol type |
| Enter | Mode gestion : popup d'edition / Mode selection : copier dans le bloc |
| Insert | Ajouter un vol type (popup) |
| Suppr | Supprimer le vol type |
| Esc | Retour |

#### Popup d'edition (dans le catalogue)

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner champ |
| Enter | Editer le champ |
| Esc | Fermer la popup |

### Calendrier (via Tab depuis niveau 1)

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner semaine |
| Fleches gauche/droite | Changer la semaine type assignee |
| Tab | Retour aux semaines types |
| Esc | Fermer |

---

## show

Afficher la configuration courante.

```
show [section]
```

Sans argument : affiche toutes les sections.

| Section | Description |
|---------|-------------|
| `effectif` | Effectif CDB/OPL/CC/PNC |
| `ftl` | Limites FTL (TSV, repos) |
| `semtypes` | Semaines types et leurs blocs |
| `calendrier` | Calendrier d'affectation |
| `programme` | Programme resolu pour le mois courant |
| `abat` | Abattements PNT et PNC |
| `sol` | Fonctions sol PNT et PNC |
| `cumul` | Limites et compteurs cumulatifs |
| `off` | Jours OFF |

**Exemples :**
```
show
show semtypes
show calendrier
show programme
show effectif
```

---

## set

Modifier un parametre de la configuration.

```
set <parametre> <valeur>
```

### Effectif

```
set cdb 14              Nombre de CDB
set opl 12              Nombre d'OPL
set cc 10               Nombre de CC
set pnc 8               Nombre de PNC
```

### Periode

```
set mois Mars 2026 31   Nom du mois, annee, nb jours
```

### Calendrier

```
set cal 10 2026 BS_01         Affecter S10-2026 a BS_01
set cal 10-14 2026 HS_01      Affecter S10 a S14 (plage)
```

### FTL

```
set tsv 10.5            TSV moyen retenu (h)
set tsvmax 13.0         TSV max journalier (h)
set repos 12.0          Repos minimum (h)
```

### Jours OFF

```
set off 8 2             Reglementaire + accord entreprise
```

### Limites cumulatives

```
set h28max 100          Max HDV sur 28 jours
set h90max 280          Max HDV sur 90 jours
set h12max 900          Max HDV sur 12 mois
```

### Compteurs entrants

```
set cumul pnt 28 50     Cumul 28j entrant PNT = 50h
set cumul pnt 90 100    Cumul 90j entrant PNT = 100h
set cumul pnt 12 400    Cumul 12m entrant PNT = 400h
set cumul pnc 28 50     Idem pour PNC
set cumul pnc 90 100
set cumul pnc 12 400
```

---

## add

Ajouter un element a la configuration.

### Semaine type

```
add semtype <reference> <saison>
```

**Exemple :**
```
add semtype HS_01 Haute
```

### Bloc de vol (dans une semaine type)

```
add bloc <ref> <seq> <jour> <periode> <debutDP> <finDP> <debutFDP> <finFDP> <vol> [<vol> ...]
```

Format vol : `numero-depart-arrivee-heureDepart-heureArrivee`

**Exemple :**
```
add bloc BS_01 1 Lundi matin 06:10 11:50 06:10 11:30 201-NOU-LIF-07:00-07:40 202-LIF-NOU-08:10-08:50
```

### Abattement

```
add abat pnt <libelle> <jours>
add abat pnc <libelle> <jours>
```

Remplacer les espaces par `_` dans le libelle.

**Exemple :**
```
add abat pnt Stage_pilotage 5
```

### Fonction sol

```
add sol pnt <nom> <nbPersonnes> <joursMois>
add sol pnc <nom> <nbPersonnes> <joursMois>
```

**Exemple :**
```
add sol pnt Instructeur 1 6
```

---

## del

Supprimer un element. Utilisez `show` pour voir les numeros/references.

### Semaine type

Supprime la semaine type et toutes ses affectations calendrier.

```
del semtype <reference>
```

**Exemple :**
```
del semtype BS_01
```

### Bloc de vol

```
del bloc <reference_semtype> <index>
```

L'index est 1-based (visible via `show semtypes`).

**Exemple :**
```
del bloc BS_01 3        Supprimer le 3e bloc de BS_01
```

### Abattement

```
del abat pnt <index>
del abat pnc <index>
```

**Exemple :**
```
del abat pnt 2          Supprimer le 2e abattement PNT
```

### Fonction sol

```
del sol pnt <index>
del sol pnc <index>
```

**Exemple :**
```
del sol pnt 1           Supprimer la 1ere fonction sol PNT
```

---

## save

Sauvegarder la configuration dans un fichier.
Le format est detecte par l'extension (`.xml` ou `.json`).

```
save <chemin>
save                    Sauvegarde dans le fichier courant
```

**Exemples :**
```
save config_mars.xml
save parametres_v2.json
save
```

---

## load

Charger une configuration depuis un fichier.
Le format est detecte par l'extension (`.xml` ou `.json`).
La configuration courante est remplacee.

```
load <chemin>
```

**Exemples :**
```
load config_mars.xml
load parametres.json
```

---

## new

Reinitialiser la configuration aux valeurs par defaut.

```
new
```

> Attention : les modifications non sauvegardees seront perdues.

---

## help

Afficher l'aide.
**Raccourci clavier : F1**

```
help
```

| Touche | Action |
|--------|--------|
| Fleches haut/bas | Selectionner commande |
| PgUp/PgDn | Defiler le detail |
| Esc | Fermer l'aide |

---

## quit / exit

Quitter l'application.
**Raccourcis : F10, Escape**

```
quit
exit
```

Si des modifications n'ont pas ete sauvegardees, un avertissement sera affiche.

---

## Raccourcis clavier globaux

| Touche | Action |
|--------|--------|
| F1 | Aide |
| F2 | Formulaire de configuration |
| F3 | Gestionnaire de programme |
| F10 | Quitter |
| Esc | Quitter |

---

## Lancement

```bash
# Mode TUI interactif
dotnet run

# Mode TUI avec fichier de configuration
dotnet run -- --tui

# Mode batch (calcul + sortie)
dotnet run -- -c <chemin.json|.xml>

# Mode batch avec entree standard
dotnet run -- -c <chemin> -i
```
