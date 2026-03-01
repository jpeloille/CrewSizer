# Phase 1A — CRUD Équipage & Référentiel Checks (Blazor + DevExpress)

> Prompt d'implémentation pour Claude Code — à injecter dans une session Rider
> Contexte : CrewSizer v2, Phase 0 terminée (Clean Architecture, Domain, Infra, Import XML OK)

---

## Objectif

Implémenter les pages Blazor Server du module **Crew** dans `CrewSizer.Web/Components/Pages/Crew/` :

1. **CrewList.razor** — Liste des membres d'équipage avec DxGrid
2. **CrewDetail.razor** — Fiche détail navigant avec onglets Checks / Infos
3. **CrewForm.razor** — Formulaire création/édition (DxPopup)
4. **CrewCheckEdit.razor** — Édition des échéances de checks par navigant
5. **CheckDefinitions.razor** — CRUD du référentiel check_definitions
6. **CrewImport.razor** — Import des 4 fichiers XML SpreadsheetML

Remplacer la page Crew existante (stub Phase 0) par ces composants.

---

## Stack & conventions

- **Blazor Server** (InteractiveServer), .NET 9
- **DevExpress Blazor v25.x** — composants `Dx*`
- **Clean Architecture** : les pages injectent les **repositories** via `@inject ICrewRepository`, `@inject ICheckDefinitionRepository`, etc. Pas d'accès direct à `AppDbContext` depuis les pages.
- **Entités Domain** existantes : `CrewMember`, `CrewCheck`, `CheckDefinition` (dans `CrewSizer.Domain.Entities`)
- **Enums Domain** existants : `CrewCategory` (PNT/PNC), `CrewRank` (CDB/OPL/CC/PNC), `CrewQualification` (flags), `RoleGroup`, `ValidityUnit`
- Fichiers `.razor` + code-behind `.razor.cs` séparés
- Nommage français pour les labels UI, anglais pour le code

---

## Arborescence cible

```
Components/Pages/Crew/
├── CrewList.razor              ← @page "/crew"
├── CrewList.razor.cs
├── CrewDetail.razor            ← composant enfant (popup ou inline)
├── CrewDetail.razor.cs
├── CrewForm.razor              ← DxPopup pour création/édition
├── CrewForm.razor.cs
├── CrewCheckEdit.razor         ← DxPopup édition échéances
├── CrewCheckEdit.razor.cs
├── CrewImport.razor            ← DxPopup import XML
├── CrewImport.razor.cs
├── CheckDefinitions.razor      ← @page "/crew/checks" (réf. checks)
└── CheckDefinitions.razor.cs
```

---

## 1. CrewList.razor — Page principale

### Layout

```
┌─────────────────────────────────────────────────────────┐
│  Stats bar : 5 cartes (Total | PNT | PNC | Actifs | ⚠) │
├─────────────────────────────────────────────────────────┤
│  Toolbar : [Recherche] [PNT|PNC|TOUS] [ACTIFS|INACTIFS] │
│            ────────────────── [Import XML] [+ Nouveau]   │
├─────────────────────────────────────────────────────────┤
│  DxGrid (crew_members)                                   │
│  Colonnes :                                              │
│   • Statut (●/○)     • Trigramme (mono, cyan)           │
│   • Nom Prénom        • Catégorie (badge PNT/PNC)        │
│   • Rang (badge)      • Qualifications (tags)            │
│   • Base              • Date entrée                      │
│   • Mini-barre checks (barres colorées)                  │
│   • Actions : [👁 Voir] [📅 Checks] [✏ Éditer] [🗑]    │
└─────────────────────────────────────────────────────────┘
```

### DxGrid config

```razor
<DxGrid Data="@_members"
        ShowFilterRow="true"
        ShowGroupPanel="true"
        AllowSort="true"
        PageSize="50"
        KeyFieldName="Id"
        SelectionMode="GridSelectionMode.Single"
        SelectedDataItemChanged="OnRowSelected"
        CssClass="crew-grid">

    <Columns>
        <DxGridDataColumn FieldName="IsActive" Caption="" Width="40px"
                          FilterRowEditorVisible="false">
            <CellDisplayTemplate>
                @{ var m = (CrewMember)context.DataItem; }
                <span class="status-dot @(m.IsActive ? "active" : "inactive")"></span>
            </CellDisplayTemplate>
        </DxGridDataColumn>

        <DxGridDataColumn FieldName="Trigram" Caption="Tri." Width="80px"
                          SortIndex="0" SortOrder="GridColumnSortOrder.Ascending">
            <CellDisplayTemplate>
                @{ var m = (CrewMember)context.DataItem; }
                <span class="trigram">@m.Trigram</span>
            </CellDisplayTemplate>
        </DxGridDataColumn>

        <DxGridDataColumn FieldName="FullName" Caption="Nom" />

        <DxGridDataColumn FieldName="Category" Caption="Cat." Width="70px"
                          GroupIndex="0">
            <CellDisplayTemplate>
                @{ var m = (CrewMember)context.DataItem; }
                <span class="badge badge-@m.Category.ToString().ToLower()">@m.Category</span>
            </CellDisplayTemplate>
        </DxGridDataColumn>

        <DxGridDataColumn FieldName="Rank" Caption="Rang" Width="70px">
            <CellDisplayTemplate>
                @{ var m = (CrewMember)context.DataItem; }
                <span class="badge badge-@m.Rank.ToString().ToLower()">@m.Rank</span>
            </CellDisplayTemplate>
        </DxGridDataColumn>

        <DxGridDataColumn FieldName="Qualifications" Caption="Qualifications"
                          AllowSort="false" FilterRowEditorVisible="false">
            <CellDisplayTemplate>
                @{ var m = (CrewMember)context.DataItem; }
                <div class="qual-tags">
                    @foreach (var q in m.QualificationList)
                    {
                        <span class="qual-tag @(q is "TRE" or "TRI" ? "special" : "")">@q</span>
                    }
                </div>
            </CellDisplayTemplate>
        </DxGridDataColumn>

        <DxGridDataColumn FieldName="BasePrimary" Caption="Base" Width="90px" />

        <DxGridDataColumn FieldName="JoinedOn" Caption="Entrée" Width="100px"
                          DisplayFormat="dd/MM/yyyy" />

        @* Mini-barre checks — template custom *@
        <DxGridDataColumn Caption="Checks" Width="120px"
                          AllowSort="false" AllowFilter="false">
            <CellDisplayTemplate>
                @{ var m = (CrewMember)context.DataItem; }
                <CheckMiniBar CrewMemberId="m.Id" Checks="@_checksMap[m.Id]" />
            </CellDisplayTemplate>
        </DxGridDataColumn>

        <DxGridDataColumn Caption="Actions" Width="130px"
                          AllowSort="false" AllowFilter="false">
            <CellDisplayTemplate>
                @{ var m = (CrewMember)context.DataItem; }
                <DxButton Text="" IconCssClass="oi oi-eye"
                          RenderStyle="ButtonRenderStyle.Link"
                          Click="() => ShowDetail(m)" />
                <DxButton Text="" IconCssClass="oi oi-calendar"
                          RenderStyle="ButtonRenderStyle.Link"
                          Click="() => ShowCheckEdit(m)" />
                <DxButton Text="" IconCssClass="oi oi-pencil"
                          RenderStyle="ButtonRenderStyle.Link"
                          Click="() => ShowForm(m)" />
                <DxButton Text="" IconCssClass="oi oi-trash"
                          RenderStyle="ButtonRenderStyle.Link"
                          RenderStyleMode="ButtonRenderStyleMode.Outline"
                          Click="() => ConfirmDelete(m)" />
            </CellDisplayTemplate>
        </DxGridDataColumn>
    </Columns>
</DxGrid>
```

### Stats bar (au-dessus du grid)

5 cartes affichant : Total équipage, PNT, PNC, Actifs, Checks critiques (expired + <30j).

Calculer côté `OnInitializedAsync` :

```csharp
_members = await CrewRepo.GetAllAsync(); // pas seulement actifs
_stats = new CrewStats
{
    Total = _members.Count,
    Pnt = _members.Count(m => m.Category == CrewCategory.PNT),
    Pnc = _members.Count(m => m.Category == CrewCategory.PNC),
    Active = _members.Count(m => m.IsActive),
    ChecksAlert = _checksMap.Values.SelectMany(c => c)
        .Count(c => c.ExpiryDate < DateTime.Today.AddDays(30))
};
```

### Toolbar : filtres rapides

Utiliser des `DxToggleButton` en groupe pour PNT/PNC/TOUS et ACTIFS/INACTIFS/TOUS. Filtrer côté client (`_filteredMembers`) ou via le FilterRow du DxGrid.

---

## 2. CrewForm.razor — Création / Édition

### DxPopup + DxFormLayout

```razor
<DxPopup @bind-Visible="_showForm"
         HeaderText="@(_isNew ? "Nouveau membre d'équipage" : $"Éditer {_form.Trigram}")"
         Width="680px" CloseOnOutsideClick="true"
         ShowFooter="true">
    <BodyContentTemplate>
        <DxFormLayout>
            <DxFormLayoutGroup Caption="Identité" ColSpanMd="12">
                <DxFormLayoutItem Caption="Nom" ColSpanMd="6">
                    <DxTextBox @bind-Text="_form.LastName" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Prénom" ColSpanMd="6">
                    <DxTextBox @bind-Text="_form.FirstName" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Trigramme" ColSpanMd="4">
                    <DxTextBox @bind-Text="_form.Trigram" MaxLength="3"
                               TextChanged="v => _form.Trigram = v.ToUpper()" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="N° Équipage" ColSpanMd="4">
                    <DxTextBox @bind-Text="_form.CrewNumber" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Catégorie" ColSpanMd="4">
                    <DxComboBox Data="@_categories" @bind-Value="_form.Category"
                                TextFieldName="Name" ValueFieldName="Value" />
                </DxFormLayoutItem>
            </DxFormLayoutGroup>

            <DxFormLayoutGroup Caption="Affectation" ColSpanMd="12">
                <DxFormLayoutItem Caption="Rang" ColSpanMd="4">
                    <DxComboBox Data="@_ranks" @bind-Value="_form.Rank" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Base principale" ColSpanMd="4">
                    <DxComboBox Data="@_bases" @bind-Value="_form.BasePrimary" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Base secondaire" ColSpanMd="4">
                    <DxComboBox Data="@_bases" @bind-Value="_form.BaseSecondary"
                                NullText="— Aucune —" AllowUserInput="false" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Type avion" ColSpanMd="4">
                    <DxTextBox @bind-Text="_form.AircraftType" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Rules Set" ColSpanMd="4">
                    <DxTextBox @bind-Text="_form.RulesSet" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Type contrat" ColSpanMd="4">
                    <DxComboBox Data='@(new[]{"PNT","PNC"})'
                                @bind-Value="_form.ContractType" />
                </DxFormLayoutItem>
            </DxFormLayoutGroup>

            <DxFormLayoutGroup Caption="Dates" ColSpanMd="12">
                <DxFormLayoutItem Caption="Date d'entrée" ColSpanMd="6">
                    <DxDateEdit @bind-Date="_form.JoinedOn" Format="dd/MM/yyyy" />
                </DxFormLayoutItem>
                <DxFormLayoutItem Caption="Disponible jusqu'au" ColSpanMd="6">
                    <DxDateEdit @bind-Date="_form.AvailableUntil"
                                NullText="Indéfini" Format="dd/MM/yyyy" />
                </DxFormLayoutItem>
            </DxFormLayoutGroup>

            <DxFormLayoutGroup Caption="Qualifications" ColSpanMd="12">
                <DxFormLayoutItem ColSpanMd="12">
                    @* Tags cliquables pour chaque qualification *@
                    <DxTagBox Data="@_allQualifications"
                              @bind-Values="_form.SelectedQualifications"
                              AllowCustomTags="false" />
                </DxFormLayoutItem>
            </DxFormLayoutGroup>

            <DxFormLayoutItem ColSpanMd="12">
                <DxCheckBox @bind-Checked="_form.IsActive" Text="Actif" />
            </DxFormLayoutItem>
        </DxFormLayout>
    </BodyContentTemplate>
    <FooterContentTemplate>
        <DxButton Text="Annuler" RenderStyle="ButtonRenderStyle.Secondary"
                  Click="() => _showForm = false" />
        <DxButton Text="@(_isNew ? "Créer" : "Enregistrer")"
                  RenderStyle="ButtonRenderStyle.Primary"
                  Click="SaveMember" />
    </FooterContentTemplate>
</DxPopup>
```

### Logique code-behind

```csharp
private async Task SaveMember()
{
    if (_isNew)
    {
        var member = MapFormToEntity(_form);
        await CrewRepo.AddAsync(member);
    }
    else
    {
        var existing = await CrewRepo.GetByIdAsync(_form.Id);
        MapFormToExisting(_form, existing);
        await CrewRepo.UpdateAsync(existing);
    }
    await LoadMembers();
    _showForm = false;
}
```

### Validation

Utiliser `FluentValidation` si déjà câblé, sinon les attributs `[Required]` basiques :

- `Trigram` : requis, exactement 3 chars, unique
- `LastName` / `FirstName` : requis
- `Category`, `Rank` : requis
- `BasePrimary` : requis
- `JoinedOn` : requis

---

## 3. CrewDetail.razor — Fiche détail

### Layout

```
┌──────────────────────────────────────────────────────┐
│  [Avatar TRI] NOM Prénom                              │
│  PNT | CDB | N° 642 | ● Actif      [Checks] [Éditer]│
├────────────────┬─────────────────────────────────────┤
│  [Checks ⚠3]  │  [Informations]                      │
├────────────────┴─────────────────────────────────────┤
│                                                       │
│  ONGLET CHECKS :                                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐             │
│  │ OPC  SIM │ │ LPC  SIM │ │ FT1  SIM │  ...        │
│  │ 12/06/26 │ │ 30/09/26 │ │ Expiré!  │             │
│  │ 112j ✓   │ │ 223j     │ │ -15j ⚠   │             │
│  └──────────┘ └──────────┘ └──────────┘             │
│                                                       │
│  ONGLET INFOS :                                       │
│  Trigramme: LRS        N° équipage: 642              │
│  Catégorie: PNT        Rang: CDB                     │
│  Base: GEA / NOU       Type avion: AT7               │
│  Rules Set: TY_R...    Entrée: 01/02/2005            │
│  ...                                                  │
└──────────────────────────────────────────────────────┘
```

### Composant DxTabs

```razor
<DxTabs>
    <DxTabPage Text="Checks & Qualifications">
        @* Badges récap : X expiré(s), Y critique(s) <30j, Z attention <90j *@
        @* Tags qualifications : CDB, TRE, TRI... *@
        @* Grid de check-cards colorées *@
        @foreach (var check in _sortedChecks)
        {
            <div class="check-card @check.StatusCss">
                <div class="check-header">
                    <span class="check-code">@check.Code</span>
                    @if (check.RequiresSimulator)
                    {
                        <span class="sim-badge">SIM</span>
                    }
                </div>
                <div class="check-date">@check.ExpiryDate.ToString("dd MMM yyyy")</div>
                <div class="check-days @check.StatusCss">
                    @check.DaysLabel
                </div>
                <div class="check-desc">@check.Description</div>
            </div>
        }
    </DxTabPage>
    <DxTabPage Text="Informations">
        @* Grille 2 colonnes label/valeur *@
    </DxTabPage>
</DxTabs>
```

### Logique statut check

Créer un helper `CheckStatusHelper.cs` dans le projet Web (ou shared) :

```csharp
public static class CheckStatusHelper
{
    public static string GetStatus(DateTime? expiryDate)
    {
        if (expiryDate is null) return "none";
        var days = (expiryDate.Value - DateTime.Today).Days;
        return days switch
        {
            < 0 => "expired",
            < 30 => "critical",
            < 90 => "warning",
            _ => "valid"
        };
    }

    public static string GetCssClass(string status) => status switch
    {
        "expired" => "check-expired",
        "critical" => "check-critical",
        "warning" => "check-warning",
        "valid" => "check-valid",
        _ => ""
    };

    public static string GetDaysLabel(DateTime? expiryDate)
    {
        if (expiryDate is null) return "—";
        var days = (expiryDate.Value - DateTime.Today).Days;
        return days < 0
            ? $"Expiré depuis {Math.Abs(days)}j"
            : $"{days}j restants";
    }
}
```

---

## 4. CrewCheckEdit.razor — Édition des checks d'un navigant

### Concept

DxPopup large (820px) affichant un **DxGrid éditable** groupé par `RoleGroup` avec :
- Colonne statut (pastille colorée)
- Colonne code (mono, bold)
- Colonne description
- Colonne validité (label calculé : "6 mois", "1 an", "3 ans")
- Colonne SIM (badge si `RequiresSimulator`)
- Colonne **Échéance** → `DxDateEdit` inline
- Colonne **Dernière réalisation** → `DxDateEdit` inline
- Colonne J restants (calculé, coloré)

### Filtrage par qualifications

Ne montrer que les checks applicables au navigant :

```csharp
private List<CheckDefinition> GetApplicableChecks(CrewMember member)
{
    return _allCheckDefs
        .Where(d => !d.IsDisabled)
        .Where(d => d.RoleGroup switch
        {
            RoleGroup.TRI => member.HasQualification(CrewQualification.TRI),
            RoleGroup.TRE => member.HasQualification(CrewQualification.TRE),
            RoleGroup.CDB => member.Rank == CrewRank.CDB,
            _ => true // ALL, COCKPIT → afficher pour tous
        })
        .OrderBy(d => d.SortOrder)
        .ToList();
}
```

### DxGrid en mode édition

```razor
<DxGrid Data="@_editableChecks"
        EditMode="GridEditMode.EditCell"
        GroupBy="GroupName"
        ShowGroupPanel="false"
        CustomizeEditModel="OnCustomizeEditModel">
    <Columns>
        <DxGridDataColumn FieldName="StatusDot" Caption="" Width="35px" />
        <DxGridDataColumn FieldName="Code" Caption="Code" Width="80px" ReadOnly="true" />
        <DxGridDataColumn FieldName="Description" Width="180px" ReadOnly="true" />
        <DxGridDataColumn FieldName="ValidityLabel" Caption="Valid." Width="70px" ReadOnly="true" />
        <DxGridDataColumn FieldName="RequiresSimulator" Caption="Sim" Width="45px" ReadOnly="true" />
        <DxGridDataColumn FieldName="ExpiryDate" Caption="Échéance" Width="140px">
            <EditSettings>
                <DxDateEditSettings Format="dd/MM/yyyy" />
            </EditSettings>
        </DxGridDataColumn>
        <DxGridDataColumn FieldName="LastCompletedDate" Caption="Dernière réal." Width="140px">
            <EditSettings>
                <DxDateEditSettings Format="dd/MM/yyyy" />
            </EditSettings>
        </DxGridDataColumn>
        <DxGridDataColumn FieldName="DaysRemaining" Caption="J rest." Width="75px" ReadOnly="true" />
    </Columns>
</DxGrid>
```

### Sauvegarde

```csharp
private async Task SaveChecks()
{
    foreach (var row in _editableChecks.Where(r => r.IsDirty))
    {
        var existing = await CheckRepo.FindAsync(_memberId, row.CheckDefinitionId);
        if (existing is not null)
        {
            existing.ExpiryDate = row.ExpiryDate;
            existing.LastCompletedDate = row.LastCompletedDate;
            await CheckRepo.UpdateAsync(existing);
        }
        else if (row.ExpiryDate.HasValue)
        {
            await CheckRepo.AddAsync(new CrewCheck
            {
                CrewMemberId = _memberId,
                CheckDefinitionId = row.CheckDefinitionId,
                ExpiryDate = row.ExpiryDate.Value,
                LastCompletedDate = row.LastCompletedDate
            });
        }
    }
    _showCheckEdit = false;
    await LoadMembers(); // refresh grid
}
```

---

## 5. CheckDefinitions.razor — Référentiel checks

### Route : `@page "/crew/checks"`

### Layout

```
┌──────────────────────────────────────────────────────────┐
│  📖 Référentiel des Checks                               │
│  24 types définis — 6 nécessitant simulateur              │
├──────────────────────────────────────────────────────────┤
│  [Recherche] [ALL|COCKPIT|CABINE|TRI|TRE|CDB|...]       │
│                                         [+ Nouveau check] │
├──────────────────────────────────────────────────────────┤
│  DxGrid (check_definitions) — édition inline             │
│                                                           │
│  Colonnes :                                               │
│   • Toggle actif/désactivé                                │
│   • Code (mono, bold) — ex: OPC                          │
│   • Description — ex: Operator Proficiency Check         │
│   • Groupe (badge) — COCKPIT, ALL, TRI...                │
│   • Validité — "6 mois", "1 an", "3 ans"                │
│   • Warning — "2 mois", "3 mois"                        │
│   • SIM (badge si simulateur requis)                     │
│   • Primaire (●)                                         │
│   • EOM (arrondi fin de mois)                            │
│   • Ordre                                                │
│   • Actions : [✏ Éditer] [🗑 Supprimer]                 │
└──────────────────────────────────────────────────────────┘
```

### DxGrid config

```razor
<DxGrid Data="@_filteredDefs"
        ShowFilterRow="true"
        AllowSort="true"
        PageSize="50"
        KeyFieldName="Id"
        EditMode="GridEditMode.PopupEditForm">

    @* ... colonnes similaires au prototype React *@

</DxGrid>
```

### Formulaire check definition (DxPopup)

Champs :
- `Code` : TextBox, uppercase, unique (validation)
- `Description` : TextBox
- `RoleGroup` : ComboBox (ALL, COCKPIT, CABINE, TRI, TRE, CDB, ICSS, FORMATEUR, SUP_AEL)
- `ValidityCount` : SpinEdit (min 1)
- `ValidityUnit` : ComboBox (Month, Year)
- `WarningCount` : SpinEdit (min 1)
- `WarningUnit` : ComboBox (Month, Year)
- `SortOrder` : SpinEdit
- `RequiresSimulator` : CheckBox
- `IsPrimary` : CheckBox
- `EndOfMonth` : CheckBox (arrondi fin de mois)
- `IsDisabled` : CheckBox (pour désactiver sans supprimer — dérogations)

---

## 6. CrewImport.razor — Import XML

### DxPopup avec zones d'upload

```razor
<DxPopup @bind-Visible="_showImport"
         HeaderText="Import XML SpreadsheetML"
         Width="640px">
    <BodyContentTemplate>
        @* Zone drag & drop *@
        <div class="import-zone" @ondragover:preventDefault
             @ondrop="HandleDrop" @ondragenter="() => _dragOver = true"
             @ondragleave="() => _dragOver = false">
            <InputFile OnChange="OnFilesSelected" accept=".xml" multiple />
            <p>Glisser-déposer les fichiers XML ou cliquer</p>
            <small>PntCrewList.xml · PncCrewList.xml · CrewCheckStatus.xml · Check_Description.xml</small>
        </div>

        @* Liste des fichiers sélectionnés avec statut *@
        @foreach (var f in _selectedFiles)
        {
            <div class="import-file @(f.Matched ? "ok" : "error")">
                <span>@f.FileName</span>
                <span>@(f.Matched ? $"✓ {f.MatchedType}" : "⚠ Non reconnu")</span>
            </div>
        }

        @* Preview *@
        @if (_selectedFiles.Any(f => f.Matched))
        {
            <div class="import-preview">
                → @_selectedFiles.Count(f => f.Matched) / @_selectedFiles.Count fichier(s) reconnu(s)<br/>
                → Mode : Upsert (mise à jour si trigramme existant)<br/>
                → Correction auto encodage UTF-8 (double-encoding latin1)
            </div>
        }
    </BodyContentTemplate>
    <FooterContentTemplate>
        <DxButton Text="Annuler" Click="() => _showImport = false"
                  RenderStyle="ButtonRenderStyle.Secondary" />
        <DxButton Text="Importer" Click="ExecuteImport"
                  RenderStyle="ButtonRenderStyle.Primary"
                  Enabled="@_selectedFiles.Any(f => f.Matched)" />
    </FooterContentTemplate>
</DxPopup>
```

### Logique d'import

Les importers existent déjà (Phase 0) : `ICrewListImporter`, `ICheckDescriptionImporter`, `ICrewCheckStatusImporter`. Les appeler séquentiellement :

```csharp
private async Task ExecuteImport()
{
    _importing = true;
    var results = new List<string>();

    // 1. Check_Description d'abord (référentiel)
    var descFile = _selectedFiles.FirstOrDefault(f => f.Type == ImportFileType.CheckDescription);
    if (descFile is not null)
    {
        using var stream = descFile.File.OpenReadStream(10 * 1024 * 1024);
        var result = await CheckDescImporter.ImportAsync(stream);
        results.Add($"Check_Description : {result.InsertedCount} insérés, {result.UpdatedCount} mis à jour");
    }

    // 2. CrewList PNT + PNC
    foreach (var crewFile in _selectedFiles.Where(f => f.Type is ImportFileType.PntCrew or ImportFileType.PncCrew))
    {
        using var stream = crewFile.File.OpenReadStream(10 * 1024 * 1024);
        var result = await CrewImporter.ImportAsync(stream);
        results.Add($"{crewFile.FileName} : {result.InsertedCount} insérés, {result.UpdatedCount} mis à jour");
    }

    // 3. CrewCheckStatus en dernier (dépend des 2 précédents)
    var statusFile = _selectedFiles.FirstOrDefault(f => f.Type == ImportFileType.CheckStatus);
    if (statusFile is not null)
    {
        using var stream = statusFile.File.OpenReadStream(10 * 1024 * 1024);
        var result = await CheckStatusImporter.ImportAsync(stream);
        results.Add($"CrewCheckStatus : {result.InsertedCount} insérés, {result.UpdatedCount} mis à jour");
    }

    _importResults = results;
    _importing = false;
    await LoadMembers();
}
```

### Reconnaissance automatique des fichiers

```csharp
private ImportFileType? DetectFileType(string fileName)
{
    var lower = fileName.ToLowerInvariant();
    if (lower.Contains("pntcrew") || lower.Contains("pnt_crew") || lower.Contains("pntcrewlist"))
        return ImportFileType.PntCrew;
    if (lower.Contains("pnccrew") || lower.Contains("pnc_crew") || lower.Contains("pnccrewlist"))
        return ImportFileType.PncCrew;
    if (lower.Contains("checkstatus") || lower.Contains("check_status") || lower.Contains("crewcheckstatus"))
        return ImportFileType.CheckStatus;
    if (lower.Contains("check_desc") || lower.Contains("checkdesc") || lower.Contains("check_definition"))
        return ImportFileType.CheckDescription;
    return null;
}
```

---

## 7. Composant CheckMiniBar.razor

Composant réutilisable affichant les barres colorées miniatures pour la colonne checks du DxGrid.

```razor
@* Components/Shared/CheckMiniBar.razor *@
<div class="check-mini-bar">
    @foreach (var check in Checks.OrderBy(c => c.ExpiryDate))
    {
        <div class="check-mini @CheckStatusHelper.GetStatus(check.ExpiryDate)"
             title="@check.CheckCode: @check.ExpiryDate?.ToString("dd/MM/yyyy")">
        </div>
    }
</div>

@code {
    [Parameter] public Guid CrewMemberId { get; set; }
    [Parameter] public IEnumerable<CrewCheck> Checks { get; set; } = [];
}
```

---

## 8. Repositories nécessaires

Vérifier que ces méthodes existent dans les interfaces + implémentations :

### ICrewRepository

```csharp
Task<List<CrewMember>> GetAllAsync();              // tous (actifs + inactifs)
Task<List<CrewMember>> GetAllActiveAsync();
Task<CrewMember?> GetByIdAsync(Guid id);
Task<CrewMember?> GetByTrigramAsync(string trigram);
Task AddAsync(CrewMember member);
Task UpdateAsync(CrewMember member);
Task DeleteAsync(Guid id);
```

### ICrewCheckRepository (à créer si manquant)

```csharp
Task<List<CrewCheck>> GetByMemberIdAsync(Guid memberId);
Task<CrewCheck?> FindAsync(Guid memberId, Guid checkDefinitionId);
Task AddAsync(CrewCheck check);
Task UpdateAsync(CrewCheck check);
Task DeleteByMemberIdAsync(Guid memberId);  // cascade delete
```

### ICheckDefinitionRepository

```csharp
Task<List<CheckDefinition>> GetAllAsync();
Task<List<CheckDefinition>> GetActiveAsync();       // !IsDisabled
Task<CheckDefinition?> GetByIdAsync(Guid id);
Task<CheckDefinition?> GetByCodeAsync(string code);
Task AddAsync(CheckDefinition def);
Task UpdateAsync(CheckDefinition def);
Task DeleteAsync(Guid id);
```

Si des méthodes manquent, les ajouter dans l'interface Domain + l'implémentation Infrastructure.

---

## 9. CSS à ajouter

Créer `wwwroot/css/crew.css` et l'inclure dans le layout :

```css
/* Status dot */
.status-dot { width: 8px; height: 8px; border-radius: 50%; display: inline-block; }
.status-dot.active { background: #10b981; box-shadow: 0 0 6px rgba(16,185,129,0.5); }
.status-dot.inactive { background: #64748b; }

/* Trigram */
.trigram { font-family: 'JetBrains Mono', monospace; font-weight: 700; font-size: 14px; color: #06b6d4; letter-spacing: 1px; }

/* Badges */
.badge { display: inline-flex; padding: 2px 8px; border-radius: 4px; font-family: 'JetBrains Mono', monospace; font-size: 11px; font-weight: 600; }
.badge-pnt { background: rgba(59,130,246,0.15); color: #3b82f6; }
.badge-pnc { background: rgba(139,92,246,0.15); color: #8b5cf6; }
.badge-cdb { background: rgba(245,158,11,0.15); color: #f59e0b; }
.badge-opl { background: rgba(6,182,212,0.15); color: #06b6d4; }
.badge-cc  { background: rgba(139,92,246,0.15); color: #8b5cf6; }

/* Qualification tags */
.qual-tags { display: flex; gap: 3px; flex-wrap: wrap; }
.qual-tag { font-family: 'JetBrains Mono', monospace; font-size: 10px; font-weight: 600; padding: 1px 6px; border-radius: 3px; background: #0f172a; color: #94a3b8; border: 1px solid #1e293b; }
.qual-tag.special { border-color: #f59e0b; color: #f59e0b; }

/* Check mini bar */
.check-mini-bar { display: flex; gap: 2px; }
.check-mini { width: 6px; height: 18px; border-radius: 2px; }
.check-mini.valid { background: #10b981; }
.check-mini.warning { background: #f59e0b; }
.check-mini.critical { background: #ef4444; }
.check-mini.expired { background: #7f1d1d; }

/* Check cards */
.check-card { padding: 12px; border-radius: 8px; border: 1px solid #1e293b; background: #1a2332; }
.check-card.check-valid { border-left: 3px solid #10b981; }
.check-card.check-warning { border-left: 3px solid #f59e0b; }
.check-card.check-critical { border-left: 3px solid #ef4444; }
.check-card.check-expired { border-left: 3px solid #ef4444; background: rgba(127,29,29,0.1); }
.sim-badge { font-family: 'JetBrains Mono', monospace; font-size: 9px; font-weight: 700; padding: 1px 5px; border-radius: 3px; background: rgba(245,158,11,0.15); color: #f59e0b; }

/* Stats cards */
.stats-bar { display: flex; gap: 16px; margin-bottom: 20px; }
.stat-card { flex: 1; padding: 14px 18px; background: #1a2332; border: 1px solid #1e293b; border-radius: 12px; display: flex; align-items: center; gap: 14px; }
.stat-value { font-size: 22px; font-weight: 700; font-family: 'JetBrains Mono', monospace; }
.stat-label { font-size: 11px; color: #64748b; text-transform: uppercase; letter-spacing: 0.5px; }

/* Import zone */
.import-zone { border: 2px dashed #1e293b; border-radius: 12px; padding: 40px; text-align: center; cursor: pointer; transition: 0.2s; }
.import-zone:hover { border-color: #3b82f6; background: rgba(59,130,246,0.05); }
```

---

## 10. Navigation

Mettre à jour `NavMenu.razor` pour ajouter les entrées :

```razor
<DxMenuItem Text="Équipage" IconCssClass="oi oi-people" NavigateUrl="/crew" />
<DxMenuItem Text="Réf. Checks" IconCssClass="oi oi-book" NavigateUrl="/crew/checks" />
```

---

## 11. Pré-requis à vérifier avant de coder

1. **`CrewMember.FullName`** — propriété calculée `$"{LastName} {FirstName}"` doit exister
2. **`CrewMember.QualificationList`** — propriété calculée qui split les qualifications flags en `List<string>`
3. **`CrewMember.HasQualification(CrewQualification q)`** — méthode qui teste le flag
4. **`CrewMember.IsExaminer`** — propriété calculée `HasQualification(TRE) || HasQualification(TRI)`
5. **Navigation `crew_checks`** — la navigation `CrewMember.Checks` (ICollection<CrewCheck>) doit être chargée avec `Include` dans le repository
6. **`CheckDefinition`** doit avoir `RoleGroup` mappé en enum + `ValidityUnit` / `WarningUnit` comme enum `ValidityUnit { Month, Year }`
7. **`ICrewCheckRepository`** — à créer si n'existe pas encore
8. **DI** — Enregistrer tous les repos dans `ServiceCollectionExtensions.cs` (Infrastructure)

---

## 12. Ordre d'implémentation recommandé

1. Vérifier/compléter les repositories et méthodes manquantes
2. Créer `CheckStatusHelper.cs` + `crew.css`
3. `CheckMiniBar.razor` (composant partagé)
4. `CrewList.razor` + `.razor.cs` (page principale avec DxGrid)
5. `CrewForm.razor` (popup création/édition)
6. `CrewDetail.razor` (popup fiche détail avec onglets)
7. `CrewCheckEdit.razor` (popup édition échéances)
8. `CrewImport.razor` (popup import XML)
9. `CheckDefinitions.razor` (page CRUD référentiel)
10. Mettre à jour `NavMenu.razor`
11. Supprimer l'ancien stub Crew de la Phase 0
12. Tester : import XML → vérifier données → éditer → vérifier persistance
