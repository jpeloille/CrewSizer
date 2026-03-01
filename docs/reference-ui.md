# Référence UI — CrewSizer.Web (Blazor Server + DevExpress v25)

> Document de référence exhaustif décrivant l'architecture UI, les espaces fonctionnels,
> les composants DevExpress, les patterns de layout, le système responsive et les conventions
> de développement de l'application CrewSizer.Web.

---

## Table des matières

1. [Vue d'ensemble — Les 5 espaces fonctionnels](#1-vue-densemble--les-5-espaces-fonctionnels)
2. [Le Drawer — Architecture détaillée](#2-le-drawer--architecture-détaillée)
3. [Le NavMenu — Navigation](#3-le-navmenu--navigation)
4. [Zone de contenu — Patterns viewport-fill](#4-zone-de-contenu--patterns-viewport-fill)
5. [Catalogue des composants DevExpress layout](#5-catalogue-des-composants-devexpress-layout)
6. [Patterns CRUD DxGrid PopupEditForm](#6-patterns-crud-dxgrid-popupeditform)
7. [Patterns de rendu conditionnel](#7-patterns-de-rendu-conditionnel)
8. [Patterns de styling](#8-patterns-de-styling)
9. [Patterns spécifiques](#9-patterns-spécifiques)
10. [Anti-patterns et workarounds](#10-anti-patterns-et-workarounds)
11. [Checklist pour nouvelles pages](#11-checklist-pour-nouvelles-pages)

---

## 1. Vue d'ensemble — Les 5 espaces fonctionnels

### Schéma global

```
+--------------------------------------------------------------+
|                       APP SHELL                              |
|  (App.razor - theme Fluent Light, rendermode, ressources DX) |
|                                                              |
|  +-------------+--------------------------------------------+
|  |             |         HEADER (TargetContent)              |
|  |   DRAWER    |  [menu] Bouton toggle + [Accueil] + titre   |
|  |             +--------------------------------------------+
|  |  +-------+  |                                            |
|  |  |HEADER |  |                                            |
|  |  |CrewSzr|  |        ZONE DE CONTENU                     |
|  |  +-------+  |         (@Body)                            |
|  |  |       |  |                                            |
|  |  | BODY  |  |   Page active rendue ici                   |
|  |  |NavMenu|  |   (Dashboard, Scenario, Vols...)           |
|  |  |       |  |                                            |
|  |  +-------+  |                                            |
|  |  |FOOTER |  |                                            |
|  |  |User+  |  |                                            |
|  |  |Logout |  |                                            |
|  |  +-------+  |                                            |
|  +-------------+--------------------------------------------+
+--------------------------------------------------------------+
```

### 1.1 App Shell (`Components/App.razor`)

L'enveloppe racine de l'application. Responsabilités :

| Responsabilité | Détail |
|---|---|
| **Theme DevExpress** | `BootstrapTheme.Fluent` cloné avec `ThemeMode.Light` |
| **Ressources** | `@DxResourceManager` injecte CSS/JS DevExpress |
| **Render mode** | SSR statique (`null`) pour `/login`, `/register`, `/logout` ; `InteractiveServer` pour le reste |
| **Head** | Balises `<meta>`, polices, liens CSS globaux (`site.css`, `icons.css`) |

```csharp
// App.razor - Configuration du thème
static readonly ITheme ActiveTheme = Themes.Fluent.Clone(properties => {
    properties.Mode = ThemeMode.Light;
});
```

**Rendu conditionnel** : `App.razor` inspecte `HttpContext.Request.Path` pour déterminer le `PageRenderMode`. Les pages auth SSR ne créent pas de circuit SignalR.

### 1.2 Le Drawer (panneau latéral)

Composant `Drawer.razor` — double `DxDrawer` imbriqué pour gérer desktop (Shrink) et mobile (Overlap). Détaillé en [section 2](#2-le-drawer--architecture-détaillée).

### 1.3 Le NavMenu (navigation)

Composant `NavMenu.razor` — `DxMenu` vertical avec 5 entrées racine et 1 sous-menu. Détaillé en [section 3](#3-le-navmenu--navigation).

### 1.4 Zone de contenu (`TargetContent`)

La zone principale où `@Body` est rendu. Contient une barre supérieure (bouton menu, bouton Accueil, titre) et le conteneur de page. Détaillée en [section 4](#4-zone-de-contenu--patterns-viewport-fill).

### 1.5 Layout alternatif (`AuthLayout.razor`)

Layout séparé pour les pages d'authentification SSR :
- Fond gradient bleu (`#1a237e` vers `#283593`)
- Card blanche centrée (flex center/center)
- Pas de drawer, pas de navigation, pas d'interactivité Blazor
- Styling 100% inline dans le composant

### Flux de rendu complet

```
App.razor
  +-> Routes.razor (routeur Blazor)
       +-> MainLayout.razor (layout par defaut)
       |    +-> Drawer.razor (double drawer responsive)
       |         +-> HeaderTemplate : titre "CrewSizer" + bouton toggle
       |         +-> BodyTemplate : NavMenu.razor (DxMenu vertical)
       |         +-> FooterTemplate : AuthorizeView (nom user + deconnexion)
       |         +-> TargetContent : boutons nav + @Body (page active)
       |
       +-> AuthLayout.razor (layout SSR auth)
            +-> @Body (Login, Register, Logout)
```

### Arborescence fichiers

```
CrewSizer.Web/Components/
+-- App.razor                     # Point d'entree, config theme
+-- Routes.razor                  # Routeur, layout par defaut
+-- _Imports.razor                # Namespaces globaux
+-- Layout/
|   +-- MainLayout.razor          # Layout principal avec drawer
|   +-- MainLayout.razor.css
|   +-- Drawer.razor              # Double-drawer responsive
|   +-- Drawer.razor.css
|   +-- NavMenu.razor             # Menu vertical DxMenu
|   +-- NavMenu.razor.css
|   +-- AuthLayout.razor          # Layout SSR auth
+-- Pages/                        # 12 pages
|   +-- Dashboard/
|   +-- Scenario/
|   +-- Programme/
|   +-- Equipage/
|   +-- Resultats/
|   +-- Auth/
+-- Shared/
|   +-- DrawerStateComponentBase.cs
+-- Services/
    +-- ActiveScenarioService.cs
```

---

## 2. Le Drawer — Architecture détaillée

### 2.1 Technique du double-drawer

Le `DxDrawer` DevExpress ne supporte qu'un seul `Mode` à la fois. Pour basculer entre `Shrink` (desktop) et `Overlap` (mobile), le composant `Drawer.razor` utilise **deux instances imbriquées** avec masquage CSS conditionnel :

```
+-- DxDrawer EXTERNE (mobile) ------------------------------------+
|  Mode: Overlap                                                  |
|  IsOpen: ToggledDrawer (true = ouvert en mobile)                |
|  CSS: "navigation-drawer mobile"                                |
|  ApplyBackgroundShading: false (shading custom)                 |
|  ClosedCssClass: "panel-closed"                                 |
|                                                                 |
|  TargetContent -------------------------------------------------+
|  | +-- DxDrawer INTERNE (desktop) ----------------------------+ |
|  | |  Mode: Shrink                                            | |
|  | |  IsOpen: !ToggledDrawer (logique inversee)               | |
|  | |  CSS: "navigation-drawer"                                | |
|  | |  OpenCssClass: "panel-open"                              | |
|  | |                                                          | |
|  | |  TargetContent: <div shading> + @TargetContent           | |
|  | +----------------------------------------------------------+ |
|  +--------------------------------------------------------------+
+------------------------------------------------------------------+
```

**Code source** (`Drawer.razor`) :

```razor
<div class="drawer-container">
    <DxDrawer PanelWidth="@PanelWidth"
              CssClass="@(CssClass + " mobile")"
              Mode="DrawerMode.Overlap"
              IsOpen="@ToggledDrawer"
              BodyTemplate="BodyTemplate"
              HeaderTemplate="HeaderTemplate"
              FooterTemplate="FooterTemplate"
              ApplyBackgroundShading="false"
              ClosedCssClass="panel-closed">
        <TargetContent>
            <DxDrawer PanelWidth="@PanelWidth"
                      CssClass="@CssClass"
                      Mode="DrawerMode.Shrink"
                      IsOpen="@(!ToggledDrawer)"
                      BodyTemplate="BodyTemplate"
                      HeaderTemplate="HeaderTemplate"
                      FooterTemplate="FooterTemplate"
                      OpenCssClass="panel-open">
                <TargetContent>
                    <div class="navigation-drawer-shading"></div>
                    @TargetContent
                </TargetContent>
            </DxDrawer>
        </TargetContent>
    </DxDrawer>
</div>
```

**Paramètres du composant** :

```csharp
[Parameter] public string? CssClass { get; set; }
[Parameter] public string? PanelWidth { get; set; }      // "240px"
[Parameter] public RenderFragment? TargetContent { get; set; }
[Parameter] public RenderFragment? BodyTemplate { get; set; }
[Parameter] public RenderFragment? HeaderTemplate { get; set; }
[Parameter] public RenderFragment? FooterTemplate { get; set; }
```

**Pourquoi cette complexité ?**

| Approche | Avantage | Inconvénient |
|---|---|---|
| Un seul DxDrawer + JS pour changer Mode | Plus simple | Nécessite JS interop, casse SSR |
| Deux DxDrawer + CSS media queries | Zero JS, SSR compatible | Deux instances DOM |

L'approche double-drawer a été choisie pour garantir zéro dépendance JavaScript et compatibilité SSR.

### 2.2 Gestion de l'état via URL

L'état ouvert/fermé du drawer est persisté dans le **query parameter** `?toggledSidebar=true` :

```
/scenario                     -> drawer desktop ouvert (etat par defaut)
/scenario?toggledSidebar=true -> drawer desktop ferme, drawer mobile ouvert
```

**Classes de base** (`DrawerStateComponentBase.cs`) :

```csharp
public abstract class DrawerStateComponentBase : ComponentBase {
    [SupplyParameterFromQuery(Name = "toggledSidebar")]
    public bool ToggledDrawer { get; set; }

    protected string AddDrawerStateToUrl(string baseUrl);         // Preserve l'etat
    protected string AddDrawerStateToUrlToggled(string baseUrl);  // Inverse l'etat
    protected string RemoveDrawerStateFromUrl(string baseUrl);    // Retire le parametre
}

public abstract class DrawerStateLayoutComponentBase : LayoutComponentBase {
    // Memes proprietes/methodes, herite de LayoutComponentBase au lieu de ComponentBase
}
```

**Deux classes abstraites** car `LayoutComponentBase` et `ComponentBase` sont des hiérarchies séparées. `MainLayout` hérite de `DrawerStateLayoutComponentBase`, `Drawer.razor` hérite de `DrawerStateComponentBase`.

**Implémentation du builder URL** :

```csharp
internal static class DrawerStateUrlBuilder {
    public const string DrawerStateQueryParameterName = "toggledSidebar";

    public static string AddStateToUrl(string baseUrl, bool toggledDrawer, NavigationManager nav) {
        return nav.GetUriWithQueryParameters(baseUrl,
            new Dictionary<string, object?> {
                [DrawerStateQueryParameterName] = toggledDrawer ? true : null
            });
    }
}
```

**Logique inversée** entre les deux drawers :

| État | `ToggledDrawer` | Drawer externe (mobile) | Drawer interne (desktop) |
|---|---|---|---|
| Par défaut | `false` | Fermé (`IsOpen=false`) | Ouvert (`IsOpen=true`) |
| Toggled | `true` | Ouvert (`IsOpen=true`) | Fermé (`IsOpen=false`) |

**Avantages de l'approche URL** :
- État persiste lors des navigations (compatible Enhanced Navigation Blazor)
- Pas besoin de service Scoped (stateless)
- Fonctionne avec le bouton back du navigateur
- Compatible SSR + InteractiveServer
- URL partageable/bookmarkable

### 2.3 Responsive — Media queries

**Breakpoint unique** : `768px` (tablettes en mode portrait)

**Drawer.razor.css** — Bascule d'affichage :

```css
/* --- DESKTOP (> 768px) --- */
/* Drawer interne (shrink) visible */
::deep .navigation-drawer > .dxbl-drawer-panel {
    display: flex;
}
/* Drawer externe (overlap) cache */
::deep .navigation-drawer.mobile > .dxbl-drawer-panel {
    display: none;
}
/* Pas de backdrop */
.navigation-drawer-shading {
    display: none;
}
/* Bouton menu cache si drawer ouvert */
::deep .panel-open:not(.mobile) .nav-buttons-container .menu-button {
    display: none;
}

/* --- MOBILE (<= 768px) --- */
@media (max-width: 768px) {
    /* Drawer interne cache */
    ::deep .navigation-drawer > .dxbl-drawer-panel {
        display: none;
    }
    /* Drawer externe visible */
    ::deep .navigation-drawer.mobile > .dxbl-drawer-panel {
        display: flex;
    }
    /* Backdrop visible */
    .navigation-drawer-shading {
        display: block;
    }
    /* Bouton menu toujours visible */
    ::deep .panel-open:not(.mobile) .nav-buttons-container .menu-button {
        display: flex;
    }
}
```

**Classes utilitaires globales** (`site.css`) :

```css
.display-desktop { display: block; }
.display-mobile  { display: none; }

@media (max-width: 768px) {
    .display-desktop { display: none; }
    .display-mobile  { display: block; }
}
```

### 2.4 Shading/Backdrop custom

Le backdrop est géré manuellement (pas via `ApplyBackgroundShading` DevExpress) pour un contrôle total :

```css
.navigation-drawer-shading {
    height: 100%;
    position: absolute;
    transition: ease 300ms;
    transition-property: opacity, visibility;
    visibility: visible;
    width: 100%;
    z-index: 99;
    background-color: var(--dxds-color-surface-backdrop-default-rest);
}

/* Quand le drawer mobile est ferme */
.navigation-drawer.mobile.panel-closed .navigation-drawer-shading {
    opacity: 0;
    visibility: hidden;
}
```

**Pourquoi custom ?**
- DevExpress place le shading dans une `<div>` séparée difficile à styler
- Contrôle total sur z-index, couleur, animation
- Utilise la variable design system DevExpress pour la couleur

### 2.5 CSS variables DevExpress — Panel du drawer

**MainLayout.razor.css** :

```css
/* Suppression padding/bg/bordures par defaut */
::deep .navigation-drawer {
    --dxbl-drawer-panel-body-padding-x: 0;
    --dxbl-drawer-panel-body-padding-y: 1rem;
    --dxbl-drawer-panel-footer-bg: none;
    --dxbl-drawer-panel-header-bg: none;
    --dxbl-drawer-separator-border-width: 0;
}

/* Gradient bleu sur le panel */
::deep .navigation-drawer > .dxbl-drawer-panel {
    background-image: linear-gradient(180deg,
        var(--dxds-color-surface-primary-default-rest) 0%,
        var(--dxds-primary-170) 150%);
}
```

**Variables utilisées** :
- `--dxbl-drawer-*` : variables internes Blazor DevExpress (contrôle layout)
- `--dxds-color-*` : variables Design System DevExpress (palette Fluent Light)
- `--dxds-primary-170` : nuance custom du bleu primaire (dégradé)

### 2.6 Les 3 zones du panel

**HeaderTemplate** (`MainLayout.razor:8-15`) :

```razor
<HeaderTemplate>
    <div class="navigation-drawer-header">
        <NavLink href="@AddDrawerStateToUrl("/")" style="text-decoration:none;color:white;...">
            CrewSizer
        </NavLink>
        <NavLink href="@AddDrawerStateToUrlToggled(LocalPath)">
            <DxButton IconCssClass="@(ToggledDrawer ? "icon icon-close" : "icon icon-menu")" ... />
        </NavLink>
    </div>
</HeaderTemplate>
```

- Lien "CrewSizer" vers `/` (conserve l'état drawer via `AddDrawerStateToUrl`)
- Bouton toggle : alterne entre icône `menu` et `close` selon `ToggledDrawer`
- Navigation vers même page avec état inversé (`AddDrawerStateToUrlToggled(LocalPath)`)

```css
.navigation-drawer-header {
    align-items: center;
    display: flex;
    justify-content: space-between;
    padding: 1.375rem 0.375rem;
    width: 100%;
}
```

**BodyTemplate** (`MainLayout.razor:17-20`) :

```razor
<BodyTemplate>
    <div class="w-100">
        <NavMenu></NavMenu>
    </div>
</BodyTemplate>
```

Simplement le composant `NavMenu` dans un conteneur pleine largeur.

**FooterTemplate** (`MainLayout.razor:22-34`) :

```razor
<FooterTemplate>
    <div class="navigation-drawer-footer">
        <AuthorizeView>
            <Authorized>
                <span>@context.User.Identity?.Name</span>
                <NavLink href="/logout">
                    <DxButton Text="Deconnexion" RenderStyle="ButtonRenderStyle.Light"
                              RenderStyleMode="ButtonRenderStyleMode.Text" />
                </NavLink>
            </Authorized>
        </AuthorizeView>
    </div>
</FooterTemplate>
```

```css
.navigation-drawer-footer {
    display: flex;
    justify-content: space-evenly;
    padding-bottom: 0.875rem;
    width: 100%;
}
```

### 2.7 Toggle du drawer — Via NavLink (pas d'événement click)

Le toggle utilise la navigation Blazor plutôt qu'un event handler C# :

```razor
<NavLink href="@AddDrawerStateToUrlToggled(LocalPath)">
    <DxButton IconCssClass="@(ToggledDrawer ? "icon icon-close" : "icon icon-menu")" />
</NavLink>
```

**Avantages** :
- Compatible SSR (pas besoin de circuit SignalR)
- Pas d'event handler C# (meilleure performance)
- Fonctionne avec le bouton back du navigateur

### 2.8 Zero JavaScript

Le système drawer est 100% CSS + Blazor :
- Pas de fichier `.js` dans le projet
- Media queries CSS pour le responsive
- Navigation Blazor pour le toggle
- Query parameter pour la persistance d'état

---

## 3. Le NavMenu — Navigation

### 3.1 Structure du DxMenu

**NavMenu.razor** — Menu vertical DevExpress avec hiérarchie :

```
+-----------------------------+
|  Dashboard            [/]   |  -> icon-home
|  Scenario     [/scenario]   |  -> icon-settings
|  Programme                  |  -> icon-counter (parent, pas de NavigateUrl)
|    +- Vols                  |  -> /programme/vols
|    +- Blocs                 |  -> /programme/blocs
|    +- Semaines              |  -> /programme/semaines
|    +- Calendrier            |  -> /programme/calendrier
|  Equipage     [/equipage]   |  -> icon-weather
|  Resultats    [/resultats]  |  -> icon-docs
+-----------------------------+
```

```razor
<DxMenu Orientation="@Orientation.Vertical" CssClass="menu">
    <Items>
        <DxMenuItem NavigateUrl="/" Text="Dashboard"
                    CssClass="@MenuItemCssClass("/")" IconCssClass="icon icon-home" />
        <DxMenuItem NavigateUrl="/scenario" Text="Scenario"
                    CssClass="@MenuItemCssClass("/scenario")" IconCssClass="icon icon-settings" />
        <DxMenuItem Text="Programme" IconCssClass="icon icon-counter">
            <Items>
                <DxMenuItem NavigateUrl="/programme/vols" Text="Vols"
                            CssClass="@MenuItemCssClass("/programme/vols")" />
                <DxMenuItem NavigateUrl="/programme/blocs" Text="Blocs"
                            CssClass="@MenuItemCssClass("/programme/blocs")" />
                <DxMenuItem NavigateUrl="/programme/semaines" Text="Semaines"
                            CssClass="@MenuItemCssClass("/programme/semaines")" />
                <DxMenuItem NavigateUrl="/programme/calendrier" Text="Calendrier"
                            CssClass="@MenuItemCssClass("/programme/calendrier")" />
            </Items>
        </DxMenuItem>
        <DxMenuItem NavigateUrl="/equipage" Text="Equipage"
                    CssClass="@MenuItemCssClass("/equipage")" IconCssClass="icon icon-weather" />
        <DxMenuItem NavigateUrl="/resultats" Text="Resultats"
                    CssClass="@MenuItemCssClass("/resultats")" IconCssClass="icon icon-docs" />
    </Items>
</DxMenu>
```

**Points clés** :
- `Programme` est le seul item parent (pas de `NavigateUrl`) — conteneur pour sous-menu
- Les sous-items n'ont pas d'`IconCssClass` — seuls les items racine ont des icônes
- DevExpress gère nativement l'expansion/collapse du sous-menu et le hover

### 3.2 API DxMenu — Propriétés

**Propriétés utilisées** :

| Propriété | Valeur | Rôle |
|---|---|---|
| `Orientation` | `Orientation.Vertical` | Menu vertical (dans le drawer) |
| `CssClass` | `"menu"` | Styling via CSS scoped |

**Propriétés DxMenuItem utilisées** :

| Propriété | Rôle |
|---|---|
| `NavigateUrl` | URL de navigation (utilise `<NavLink>` en interne) |
| `Text` | Texte affiché |
| `CssClass` | Classe CSS dynamique (état actif) |
| `IconCssClass` | Classe(s) CSS pour l'icône |
| `Items` (collection) | Sous-items hiérarchiques |

**Propriétés disponibles mais non utilisées** :

| Propriété | Rôle potentiel |
|---|---|
| `ItemClick` (event) | Handler click (non utilisé car NavigateUrl suffit) |
| `Template` / `ItemTemplate` | Template custom de rendu |
| `BeginGroup` | Séparateur visuel entre items |
| `Enabled` | Activer/désactiver un item |
| `Visible` | Afficher/masquer un item |
| `Target` | Target du lien (`_blank`, etc.) |
| `Name` | Identifiant pour lookup programmatique |

### 3.3 Détection de l'état actif

Le mécanisme est **entièrement manuel** (pas de fonctionnalité DxMenu native) :

```csharp
@inject NavigationManager NavigationManager
@implements IDisposable

@code {
    private string? currentLocalPath;

    protected override void OnInitialized() {
        currentLocalPath = new Uri(NavigationManager.Uri).LocalPath;
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) {
        currentLocalPath = new Uri(NavigationManager.Uri).LocalPath;
        InvokeAsync(StateHasChanged);  // Thread-safe re-render
    }

    private string? MenuItemCssClass(string itemPath) {
        return string.Equals(currentLocalPath, itemPath,
            StringComparison.OrdinalIgnoreCase) ? "menu-item-active" : null;
    }

    public void Dispose() {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
```

**Comportement** :
- Comparaison **exacte** et case-insensitive du `LocalPath`
- `InvokeAsync(StateHasChanged)` car `LocationChanged` peut être déclenché hors thread UI

**Limite connue** : la comparaison est exacte. Si on navigue vers `/scenario/some-guid`, l'item "Scenario" (`/scenario`) ne sera **pas** marqué actif. Idem pour les sous-routes non listées.

### 3.4 Système d'icônes SVG mask-based

**Fichier** : `wwwroot/css/icons.css`

**Mécanisme** :

```css
.icon {
    width: 1.25rem;
    height: 1.25rem;
    background-color: currentcolor;        /* Herite la couleur du texte */
    mask-image: var(--icon-mask-image);
    mask-position: center;
    mask-repeat: no-repeat;
}

.icon-home {
    --icon-mask-image: url("/images/pages/home.svg");
}
```

**Avantage** : les icônes héritent `currentcolor`, donc elles s'adaptent automatiquement aux couleurs du thème DevExpress (repos, hover, active).

**Catalogue complet des icônes disponibles** :

| Classe | Fichier SVG | Usage |
|---|---|---|
| **Navigation** | | |
| `icon-home` | `/images/pages/home.svg` | Dashboard |
| `icon-settings` | `/images/pages/settings.svg` | Scenario |
| `icon-counter` | `/images/pages/counter.svg` | Programme |
| `icon-weather` | `/images/pages/weather.svg` | Equipage |
| `icon-docs` | `/images/pages/docs.svg` | Resultats |
| **UI** | | |
| `icon-back` | `/images/back.svg` | Bouton retour |
| `icon-close` | `/images/close.svg` | Fermer drawer |
| `icon-menu` | `/images/menu.svg` | Ouvrir drawer |
| `icon-demos` | `/images/demos.svg` | (non utilisé) |
| **Compte** | | |
| `icon-email` | `/images/account/email.svg` | Champ email |
| `icon-password` | `/images/account/password.svg` | Champ mot de passe |
| `icon-personal` | `/images/account/personal-information.svg` | Info personnelle |
| `icon-profile` | `/images/account/profile.svg` | Profil |
| `icon-two-factor` | `/images/account/two-factor.svg` | 2FA |
| `icon-log-in` | `/images/account/log-in.svg` | Connexion |
| `icon-log-out` | `/images/account/log-out.svg` | Déconnexion |
| `icon-user` | `/images/account/user.svg` | Utilisateur |
| `icon-external` | `/images/account/external.svg` | Lien externe |
| **Providers** (background-image, pas mask) | | |
| `icon-facebook` | `/images/account/facebook.svg` | OAuth Facebook |
| `icon-google` | `/images/account/google.svg` | OAuth Google |
| `icon-microsoft` | `/images/account/microsoft.svg` | OAuth Microsoft |
| `icon-twitter` | `/images/account/twitter.svg` | OAuth Twitter |

### 3.5 Styles CSS du menu

**NavMenu.razor.css** :

```css
/* Suppression border-radius (items pleine largeur) */
::deep .menu {
    --dxbl-menu-bottom-left-border-radius: 0;
    --dxbl-menu-bottom-right-border-radius: 0;
    --dxbl-menu-top-left-border-radius: 0;
    --dxbl-menu-top-right-border-radius: 0;

    /* Espacement items */
    --dxbl-menu-item-padding-x: 1.125rem;
    --dxbl-menu-item-padding-y: 0.5rem;

    /* Couleurs texte/icones : blanc statique (sur fond bleu du drawer) */
    --dxbl-menu-item-color: var(--dxds-color-content-neutral-default-static-dark-rest);
    --dxbl-menu-item-image-color: var(--dxds-color-content-neutral-default-static-dark-rest);

    /* Hover : fond blanc semi-transparent 15% */
    --dxbl-menu-item-hover-bg: rgb(from var(--dxds-color-surface-neutral-default-static-light-rest) r g b / 0.15);
    --dxbl-menu-item-hover-color: var(--dxds-color-content-neutral-default-static-dark-hovered);
    --dxbl-menu-item-hover-image-color: var(--dxds-color-content-neutral-default-static-dark-hovered);

    background: none;  /* Transparent (le gradient vient du drawer panel) */
}

/* Mobile : marge en bas */
::deep .menu.display-mobile {
    margin-bottom: 2rem;
}

/* Item actif : fond blanc semi-transparent 5% (plus subtil que hover) */
::deep .menu-item-active {
    background-color: rgb(from var(--dxds-color-surface-neutral-default-static-light-rest) r g b / 0.05);
}
```

**Note** : la syntaxe `rgb(from var(...) r g b / 0.15)` est du **CSS Color Level 4** (relative color syntax). Elle prend la couleur de la variable et lui applique une opacité.

### 3.6 Patterns de navigation

L'application utilise 5 méthodes de navigation distinctes :

| Méthode | Composant | Usage |
|---|---|---|
| `DxMenuItem NavigateUrl` | NavMenu | Navigation principale (sidebar) |
| `NavLink href` + `DxButton` | MainLayout | Bouton retour, toggle drawer |
| `DxToolbarItem Click` + `Nav.NavigateTo()` | Pages (toolbar) | Actions contextuelles (Retour, etc.) |
| `DxButton Click` + `Nav.NavigateTo()` | Dashboard | Raccourcis dashboard |
| `Nav.NavigateTo(url, forceLoad: true)` | Pages auth | Post-login/logout (recharge complète) |

**DxToolbar n'est PAS utilisé pour la navigation principale** — il sert uniquement aux actions contextuelles dans les pages (Enregistrer, Calculer, Retour, Nouveau, Supprimer).

---

## 4. Zone de contenu — Patterns viewport-fill

### 4.1 Structure de la zone contenu

**MainLayout.razor — TargetContent** :

```razor
<TargetContent>
    <div class="drawer-content">
        <div class="nav-buttons-container">
            <!-- Bouton menu (mobile/drawer ferme) -->
            <NavLink href="@AddDrawerStateToUrlToggled(LocalPath)" class="menu-button">
                <DxButton IconCssClass="icon icon-menu" ... />
            </NavLink>

            <!-- Bouton Accueil (sauf sur /) -->
            @if (LocalPath != "/") {
                <NavLink href="@AddDrawerStateToUrl("/")" class="button-link">
                    <DxButton Text="Accueil" IconCssClass="icon icon-back" CssClass="back-button" />
                </NavLink>
            }

            <!-- Titre app (desktop, quand drawer ferme) -->
            <span class="menu-button-float-end display-desktop">CrewSizer</span>
        </div>

        <div class="page-content-container">
            @Body
        </div>
    </div>
</TargetContent>
```

**Éléments conditionnels de la barre supérieure** :

| Élément | Visible quand | Rôle |
|---|---|---|
| Bouton menu | Mobile **ou** drawer desktop fermé | Ouvre/toggle le drawer |
| Bouton Accueil | `LocalPath != "/"` | Retour au dashboard |
| Texte "CrewSizer" | Desktop uniquement (`display-desktop`) | Rappel nom app quand drawer fermé |

### 4.2 La chaîne flex complète

Pour que les composants (DxGrid, DxTabs) remplissent exactement l'espace disponible, la chaîne flex doit être **ininterrompue** de `<html>` jusqu'au composant :

```
html (height: 100%)
  +- body (height: 100%, overflow: hidden)
       +- #app
            +- .page (height: 100%)                         <- MainLayout
                 +- DxDrawer
                      +- TargetContent
                           +- .drawer-content               <- flex column, height: 100%
                                +- .nav-buttons-container   <- flex-shrink implicite
                                +- .page-content-container  <- flex-grow: 1, min-height: 0
                                     +- @Body               <- votre page ici
```

**Si un seul maillon de cette chaîne n'est pas flex ou n'a pas `min-height: 0`, le DxGrid déborde ou le scroll casse.**

**CSS correspondant** :

```css
/* site.css */
html, body { height: 100%; }
body { overflow: hidden; }

/* MainLayout.razor.css */
.page { height: 100%; min-height: 0; }

.drawer-content {
    display: flex;
    flex-direction: column;
    height: 100%;
    overflow: hidden;
    padding: 1rem 1.5rem 0.5rem 1.5rem;
}

.page-content-container {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    min-height: 0;    /* Critique pour flex-shrink */
}
```

### 4.3 Les 3 classes utilitaires CSS

#### `.page-fill` — Enveloppe de page

```css
.page-fill {
    display: flex;
    flex-direction: column;
    height: 100%;
    overflow: hidden;        /* Pas de scroll a ce niveau */
}
.page-fill > h3 {
    flex-shrink: 0;          /* Le titre ne retrecit jamais */
    margin-bottom: 0.5rem !important;
}
```

Conteneur racine de chaque page. Prend 100% de la hauteur de `.page-content-container` et établit un nouveau contexte flex column.

#### `.grid-fill` — Zone extensible pour DxGrid

```css
.grid-fill {
    flex: 1;                 /* Prend tout l'espace restant */
    min-height: 0;           /* Permet de retrecir sous la taille intrinseque */
}
```

**`min-height: 0` est critique** : sans cette règle, un enfant flex a un `min-height` implicite égal à la taille de son contenu. Le DxGrid ne pourrait jamais être plus petit que ses données, et déborderait.

#### `.scroll-fill` — Zone scrollable extensible

```css
.scroll-fill {
    flex: 1;
    min-height: 0;
    overflow: auto;          /* Scroll interne quand le contenu deborde */
}
```

Identique à `.grid-fill` mais avec scroll. Utilisé pour les formulaires longs (DxFormLayout) qui doivent scroller dans l'espace restant.

### 4.4 `.dxc-full-height` — Correctif DxTabs

Le DxTabs DevExpress utilise par défaut un layout interne qui ne propage pas la hauteur. Ce correctif force toute la hiérarchie interne en flex column :

```css
/* Force 100% height sur le composant */
.dxc-full-height {
    height: 100% !important;
}

/* DxTabs orientation top : variables CSS internes */
.dxc-full-height.dxbl-tabs.dxbl-tabs-top {
    --dxbl-tabs-display: flex;
    --dxbl-tabs-flex-direction: column;
    --dxbl-tabs-height: 100%;
}

/* Content panel : flex column extensible */
.dxc-full-height.dxbl-tabs > .dxbl-tabs-content-panel {
    display: flex !important;
    flex-direction: column !important;
    flex: 1 1 auto !important;
    min-height: 0 !important;
    overflow: hidden !important;
}

/* Content wrapper */
.dxc-full-height.dxbl-tabs > .dxbl-tabs-content-panel > .dxbl-tabs-content {
    display: flex !important;
    flex-direction: column !important;
    flex: 1 1 auto !important;
    min-height: 0 !important;
}

/* Tab panes individuels */
.dxc-full-height.dxbl-tabs > .dxbl-tabs-content-panel > .dxbl-tabs-content > * {
    flex: 1 1 auto !important;
    min-height: 0 !important;
}
```

**Référence** : Ticket DevExpress T1178238 — CssClass ne se propage pas correctement.

### 4.5 Les 4 patterns d'usage viewport-fill

#### Pattern A : Page avec grille (Vols, Blocs, Semaines)

```
+-- .page-fill -------------------------+
| <h3>Catalogue des vols</h3>           |  <- flex-shrink: 0
|                                       |
| +-- DxGrid CssClass="grid-fill" ----+|
| |                                    ||  <- flex: 1, min-height: 0
| |  (scroll virtuel interne)         ||
| |                                    ||
| +------------------------------------+|
+---------------------------------------+
```

```razor
<div class="page-fill">
    <h3>Catalogue des vols</h3>
    <DxGrid Data="vols" CssClass="grid-fill" VirtualScrollingEnabled="true" ...>
    </DxGrid>
</div>
```

#### Pattern B : Page avec formulaire scrollable (ScenarioEdit)

```
+-- .page-fill -------------------------+
| <DxToolbar>                           |  <- flex-shrink: 0 (inline style)
| [Enregistrer] [Calculer]   [Retour]  |
+---------------------------------------+
| +-- .scroll-fill --------------------+|
| | <DxFormLayout>                     ||  <- flex: 1, overflow: auto
| |   Section 1 (repliable)           ||
| |   Section 2 (repliable)           ||
| |   ...                             ||
| |   Section 13                      ||
| |                         v scroll  ||
| +------------------------------------+|
+---------------------------------------+
```

```razor
<div class="page-fill">
    <DxToolbar CssClass="mb-3" style="flex-shrink:0;">...</DxToolbar>
    <div class="scroll-fill">
        <DxFormLayout>
            <!-- 13 sections repliables -->
        </DxFormLayout>
    </div>
</div>
```

#### Pattern C : Page avec onglets (Résultats, Équipage)

```
+-- DxStackLayout (height: 100%) -------+
| +-- Item Length="auto" --------------+|
| | <h3>Resultats</h3>                ||  <- hauteur auto
| | <DxFormLayout> snapshot info       ||
| +------------------------------------+|
| +-- Item Length="1fr" ---------------+|
| | <DxTabs CssClass="dxc-full-       ||  <- prend le reste
| |         height">                   ||
| |  [Synthese|PNT|PNC|Prog|FTL]      ||
| |  +-- contenu onglet --------------+||
| |  |  (overflow: auto interne)      |||
| |  +--------------------------------+||
| +------------------------------------+|
+---------------------------------------+
```

```razor
<DxStackLayout Orientation="Vertical" style="height:100%;">
    <Items>
        <DxStackLayoutItem Length="auto">
            <h3>Resultats de calcul</h3>
        </DxStackLayoutItem>
        <DxStackLayoutItem Length="1fr">
            <DxTabs CssClass="dxc-full-height">
                <DxTabPage Text="Synthese">
                    <div style="padding:1rem;overflow:auto;">...</div>
                </DxTabPage>
            </DxTabs>
        </DxStackLayoutItem>
    </Items>
</DxStackLayout>
```

**Note** : `DxStackLayout` est utilisé au lieu de `.page-fill` car les DxTabs nécessitent un parent avec hauteur explicite. `Length="1fr"` est l'équivalent DevExpress de `flex: 1`.

#### Pattern D : Dashboard (contenu auto)

```razor
<div class="page-fill">
    <h3>Dashboard</h3>
    <DxFormLayout><!-- tuiles info --></DxFormLayout>
    <DxGridLayout CssClass="mt-3"><!-- grille de cards --></DxGridLayout>
    <div><!-- boutons action --></div>
</div>
```

Le cas le plus simple : pas de scroll ni de grid extensible, tout le contenu a une hauteur naturelle.

### 4.6 Résumé — Quand utiliser quoi

| Besoin | Classe/Pattern | Exemples |
|---|---|---|
| Envelopper toute page | `.page-fill` | Toutes les pages |
| DxGrid plein écran | `CssClass="grid-fill"` | Vols, Blocs, Semaines |
| Formulaire long scrollable | `<div class="scroll-fill">` | ScenarioEdit |
| DxTabs plein écran | `DxStackLayout` + `CssClass="dxc-full-height"` | Résultats, Équipage |
| Élément fixe (titre, toolbar) | `flex-shrink: 0` ou `Length="auto"` | Titres, toolbars |

---

## 5. Catalogue des composants DevExpress layout

### 5.1 DxStackLayout

**Rôle** : Organisation verticale/horizontale avec items dimensionnables.

**Pages** : Equipage, EquipageMatrice, EquipageChecks, Resultats.

```razor
<DxStackLayout Orientation="Orientation.Vertical" CssClass="dxc-full-height" style="height:100%;">
    <Items>
        <DxStackLayoutItem Length="auto">   <!-- Header fixe -->
            <Template>...</Template>
        </DxStackLayoutItem>
        <DxStackLayoutItem Length="1fr">    <!-- Contenu extensible -->
            <Template>...</Template>
        </DxStackLayoutItem>
    </Items>
</DxStackLayout>
```

| Propriété | Valeurs | Rôle |
|---|---|---|
| `Orientation` | `Vertical` / `Horizontal` | Direction de l'empilement |
| `CssClass` | `"dxc-full-height"` | Force 100% hauteur |
| `style` | `"height:100%;"` | Hauteur explicite (nécessaire) |
| `Length` (item) | `"auto"`, `"1fr"`, `"200px"` | Dimensionnement de l'item |

### 5.2 DxGridLayout

**Rôle** : Grilles responsives CSS Grid avec colonnes/rows définies.

**Pages** : Dashboard, EquipageMembres, Equipage (KPI tiles).

```razor
<DxGridLayout ColumnSpacing="1rem" RowSpacing="1rem">
    <Rows>
        <DxGridLayoutRow Height="auto" />
        <DxGridLayoutRow Height="1fr" />
    </Rows>
    <Columns>
        <DxGridLayoutColumn Width="1fr" />
        <DxGridLayoutColumn Width="1fr" />
    </Columns>
    <Items>
        <DxGridLayoutItem Row="0" Column="0" ColumnSpan="2">
            <Template>...</Template>
        </DxGridLayoutItem>
    </Items>
</DxGridLayout>
```

**Usages concrets** :
- **Dashboard** : 4 colonnes `1fr`, 2 rows `auto`, tuiles avec `ColumnSpan="2"`
- **EquipageMembres** : 2 colonnes `45fr`/`55fr`, master-detail
- **Equipage KPI** : 6 colonnes `1fr`, 1 row `auto`, tuiles compactes

### 5.3 DxFormLayout

**Rôle** : Formulaires structurés avec labels/inputs alignés.

**Pages** : ScenarioEdit, Dashboard, Calendrier, pages Auth, tous les PopupEditForm.

```razor
<DxFormLayout>
    <DxFormLayoutGroup Caption="Section" ColSpanMd="12" Expanded="true">
        <DxFormLayoutItem Caption="Label" ColSpanMd="6">
            <DxTextBox @bind-Text="model.Field" />
        </DxFormLayoutItem>
        <DxFormLayoutItem Caption="Autre label" ColSpanMd="6">
            <DxSpinEdit @bind-Value="model.Number" />
        </DxFormLayoutItem>
    </DxFormLayoutGroup>
</DxFormLayout>
```

| Propriété | Rôle |
|---|---|
| `Caption` (Group) | Titre de section (repliable par défaut) |
| `Expanded` | État initial ouvert/fermé du groupe |
| `ColSpanMd` | Nombre de colonnes (sur 12, responsive) |
| `Caption` (Item) | Label du champ |

**ScenarioEdit** : 13 `DxFormLayoutGroup` repliables, layout responsive avec `ColSpanMd`.

### 5.4 DxGrid

**Rôle** : Tables de données avec CRUD intégré.

**3 modes d'édition utilisés** :

| Mode | Pages | Description |
|---|---|---|
| `PopupEditForm` | Vols, Blocs, Semaines | Popup modale avec formulaire complet |
| `EditRow` | Calendrier | Édition inline dans la ligne |
| ReadOnly (pas d'EditMode) | EquipageMembres, EquipageMatrice | Affichage seul avec sélection/filtrage |

**Propriétés communes** :

```razor
<DxGrid @ref="grid"
        Data="@data"
        CssClass="grid-fill"
        VirtualScrollingEnabled="true"
        ShowFilterRow="true"
        EditMode="GridEditMode.PopupEditForm"
        CustomizeEditModel="OnCustomizeEditModel"
        EditModelSaving="OnSaving"
        DataItemDeleting="OnDeleting"
        PopupEditFormHeaderText="@popupTitle">
    <ToolbarTemplate>
        <DxToolbar>
            <DxToolbarItem Text="Nouveau" Click="@(() => grid.StartEditNewRowAsync())"
                           RenderStyle="ButtonRenderStyle.Primary" />
        </DxToolbar>
    </ToolbarTemplate>
    <Columns>
        <DxGridDataColumn FieldName="Code" Caption="Code" />
        <DxGridCommandColumn Width="120px" />
    </Columns>
    <EditFormTemplate Context="ctx">
        <DxFormLayout>...</DxFormLayout>
    </EditFormTemplate>
</DxGrid>
```

### 5.5 DxTabs

**Rôle** : Onglets avec contenu full-height.

**Pages** : Equipage, Resultats.

```razor
<DxTabs CssClass="dxc-full-height" @bind-ActiveTabIndex="activeTabIndex">
    <DxTabPage Text="Onglet 1">
        <ComponentAvecFullHeight />
    </DxTabPage>
    <DxTabPage Text="Onglet 2">
        <AutreComponent />
    </DxTabPage>
</DxTabs>
```

**Critique** : sans `CssClass="dxc-full-height"` et les CSS overrides associés dans `site.css`, les onglets ont une hauteur auto et ne remplissent pas l'espace.

### 5.6 DxPopup

**Rôle** : Modales pour confirmations, détails, édition complémentaire.

**Pages** : ScenarioList (create/clone/delete), EquipageChecks (détail), Blocs/Semaines (implicite via PopupEditForm).

```razor
<DxPopup @bind-Visible="showPopup"
         HeaderText="Titre de la modale"
         CloseOnOutsideClick="false"
         ShowFooter="true"
         Width="400px">
    <BodyTemplate Context="popupCtx">
        <DxFormLayout>...</DxFormLayout>
        @if (popupError is not null) {
            <div style="...">@popupError</div>
        }
    </BodyTemplate>
    <FooterTemplate>
        <DxButton Text="Valider" Click="OnConfirm"
                  RenderStyle="ButtonRenderStyle.Primary" />
        <DxButton Text="Annuler" Click="() => showPopup = false"
                  RenderStyle="ButtonRenderStyle.Secondary" />
    </FooterTemplate>
</DxPopup>
```

**Note** : le `Context="popupCtx"` dans `BodyTemplate` évite le conflit avec la variable `context` implicite de Blazor.

### 5.7 DxToolbar

**Rôle** : Barres d'actions contextuelles.

**3 patterns d'usage** :

| Pattern | Pages | Description |
|---|---|---|
| Actions de page | ScenarioEdit, Calendrier | Enregistrer, Calculer, Retour |
| Actions de grid | ScenarioList, Vols, Blocs, Semaines | Nouveau, Supprimer (via `ToolbarTemplate`) |
| Filtres | EquipageMatrice, EquipageChecks | Tous, Cockpit, Cabine |

```razor
<!-- Pattern actions de page -->
<DxToolbar CssClass="mb-3" style="flex-shrink:0;">
    <DxToolbarItem Text="Enregistrer" Click="Save"
                   RenderStyle="ButtonRenderStyle.Primary" />
    <DxToolbarItem Text="Calculer" Click="RunCalcul"
                   RenderStyle="ButtonRenderStyle.Success" />
    <DxToolbarItem Text="Retour" Click="@(() => Nav.NavigateTo("/scenario"))"
                   RenderStyle="ButtonRenderStyle.Secondary"
                   Alignment="ToolbarItemAlignment.Right" />
</DxToolbar>

<!-- Pattern filtres -->
<DxToolbar>
    <DxToolbarItem Text="Tous" Click="() => SetFiltre(null)"
                   RenderStyle="@(filtreGroupe is null ? ButtonRenderStyle.Primary : ButtonRenderStyle.Light)" />
    <DxToolbarItem Text="Cockpit (PNT)" Click="() => SetFiltre(GroupeCheck.Cockpit)"
                   RenderStyle="@(filtreGroupe == GroupeCheck.Cockpit ? ButtonRenderStyle.Primary : ButtonRenderStyle.Light)" />
</DxToolbar>
```

### 5.8 DxButton

**RenderStyle** (couleur) :

| Style | Usage |
|---|---|
| `Primary` | Action principale (Enregistrer, Nouveau) |
| `Secondary` | Action secondaire (Retour, Annuler) |
| `Success` | Action positive (Calculer) |
| `Danger` | Action destructrice (Supprimer) |
| `Light` | Action discrète (Déconnexion, filtre inactif) |

**RenderStyleMode** (apparence) :

| Mode | Rendu |
|---|---|
| `Contained` | Bouton plein (défaut) |
| `Text` | Texte seul, pas de fond |
| `Outline` | Bordure seule, pas de fond |

---

## 6. Patterns CRUD DxGrid PopupEditForm

### 6.1 Workflow complet

Le pattern CRUD avec `PopupEditForm` suit un workflow en 5 étapes :

```
1. Clic "Nouveau" / icone edit
       |
       v
2. CustomizeEditModel -> creation/mapping du modele edit
       |
       v
3. Popup s'ouvre avec EditFormTemplate (DxFormLayout)
       |
       v
4. Clic "Enregistrer" dans la popup
       |
       v
5. EditModelSaving -> envoi commande MediatR
       |
       +-> Succes : popup se ferme, grid rafraichie
       +-> Erreur : e.Cancel = true, message inline
```

### 6.2 Modèle d'édition (classe interne)

Chaque page CRUD définit une classe interne pour le binding du formulaire :

```csharp
class BlocEdit
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public List<EtapeEdit> Etapes { get; set; } = [];
}

class EtapeEdit
{
    public Guid VolId { get; set; }
    public string VolCode { get; set; } = "";
    public int Ordre { get; set; }
}
```

### 6.3 CustomizeEditModel

Initialise le modèle d'édition selon le mode (création/modification) :

```csharp
void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e)
{
    editError = null;  // Reset erreur
    if (e.IsNew)
    {
        popupTitle = "Nouveau bloc";
        e.EditModel = new BlocEdit();
    }
    else
    {
        popupTitle = "Modifier le bloc";
        var item = (BlocVolDto)e.DataItem;
        e.EditModel = new BlocEdit
        {
            Id = item.Id,
            Code = item.Code,
            Description = item.Description,
            Etapes = item.Etapes.Select(et => new EtapeEdit
            {
                VolId = et.VolId,
                VolCode = et.VolCode,
                Ordre = et.Ordre
            }).ToList()
        };
    }
}
```

### 6.4 EditFormTemplate avec gestion d'erreurs

```razor
<EditFormTemplate Context="ctx">
    @{ var edit = (BlocEdit)ctx.EditModel; }

    @if (editError is not null)
    {
        <div style="background:#fce4ec;color:#c62828;padding:0.5rem 0.75rem;
                    border-radius:4px;margin-bottom:0.75rem;font-size:0.9rem;">
            @editError
        </div>
    }

    <DxFormLayout>
        <DxFormLayoutItem Caption="Code" ColSpanMd="12">
            <DxTextBox @bind-Text="edit.Code" />
        </DxFormLayoutItem>

        <!-- Collections imbriquees -->
        <DxFormLayoutGroup Caption="Etapes du bloc">
            <DxFormLayoutItem>
                <Template>
                    @if (edit.Etapes.Count > 0)
                    {
                        <table class="table table-sm">
                            @for (int i = 0; i < edit.Etapes.Count; i++)
                            {
                                var idx = i;
                                <tr>
                                    <td>@edit.Etapes[idx].VolCode</td>
                                    <td>
                                        <DxButton Text="Retirer"
                                                  Click="() => RemoveEtape(edit, idx)"
                                                  RenderStyle="ButtonRenderStyle.Danger"
                                                  RenderStyleMode="ButtonRenderStyleMode.Text" />
                                    </td>
                                </tr>
                            }
                        </table>
                    }
                    <div style="display:flex;gap:0.5rem;">
                        <DxComboBox Data="vols" @bind-Value="newEtapeVolId"
                                    TextFieldName="Code" ValueFieldName="Id" />
                        <DxButton Text="Ajouter" Click="() => AddEtape(edit)" />
                    </div>
                </Template>
            </DxFormLayoutItem>
        </DxFormLayoutGroup>
    </DxFormLayout>
</EditFormTemplate>
```

### 6.5 EditModelSaving — Envoi commande MediatR

```csharp
async Task OnSaving(GridEditModelSavingEventArgs e)
{
    try
    {
        var edit = (BlocEdit)e.EditModel;
        if (e.IsNew)
        {
            await Mediator.Send(new CreateBlocVolCommand
            {
                Code = edit.Code,
                Description = edit.Description,
                EtapeVolIds = edit.Etapes.Select(et => et.VolId).ToList()
            });
        }
        else
        {
            await Mediator.Send(new UpdateBlocVolCommand
            {
                Id = edit.Id,
                Code = edit.Code,
                Description = edit.Description,
                EtapeVolIds = edit.Etapes.Select(et => et.VolId).ToList()
            });
        }

        // Rafraichir la grille
        blocs = await Mediator.Send(new GetAllBlocsVolQuery());
    }
    catch (Application.Common.ValidationException ex)
    {
        e.Cancel = true;  // CRITIQUE : empeche la fermeture de la popup
        editError = string.Join(" -- ", ex.Errors.SelectMany(kv => kv.Value));
    }
}
```

**`e.Cancel = true`** est essentiel : sans cela, la popup se ferme même en cas d'erreur de validation, et l'utilisateur perd sa saisie.

### 6.6 DataItemDeleting

```csharp
async Task OnDeleting(GridDataItemDeletingEventArgs e)
{
    var item = (BlocVolDto)e.DataItem;
    await Mediator.Send(new DeleteBlocVolCommand(item.Id));
    blocs = await Mediator.Send(new GetAllBlocsVolQuery());
}
```

### 6.7 Collections imbriquées

Gestion de l'ajout/suppression d'éléments dans le formulaire popup (sans toucher la base) :

```csharp
private Guid? newEtapeVolId;

void AddEtape(BlocEdit edit)
{
    if (newEtapeVolId is null) return;
    var vol = vols.FirstOrDefault(v => v.Id == newEtapeVolId);
    if (vol is null) return;

    edit.Etapes.Add(new EtapeEdit
    {
        VolId = vol.Id,
        VolCode = vol.Code,
        Ordre = edit.Etapes.Count + 1
    });
    newEtapeVolId = null;
    StateHasChanged();  // Force re-render de la table
}

void RemoveEtape(BlocEdit edit, int idx)
{
    edit.Etapes.RemoveAt(idx);
    StateHasChanged();
}
```

---

## 7. Patterns de rendu conditionnel

### 7.1 Loading states

**Pattern simple** — Boolean flag (ScenarioEdit) :

```razor
@if (loading)
{
    <p>Chargement...</p>
}
else if (model is null)
{
    <p>Scenario introuvable.</p>
}
else
{
    <!-- Contenu principal -->
}
```

**Pattern deferred render** — Évite accès concurrent DbContext (Equipage) :

```razor
@if (loaded)
{
    <DxTabs>...</DxTabs>   @* Rendu apres chargement complet *@
}
```

**Contexte** : en Blazor Server, le `DbContext` est Scoped (un par circuit). Si plusieurs composants enfants effectuent des requêtes parallèles lors du premier rendu, cela cause des erreurs de concurrence. La solution : charger les données dans le parent, puis rendre les enfants.

### 7.2 Empty states

**Pattern avec action** (Dashboard) :

```razor
@if (ActiveScenario.HasScenario)
{
    <!-- Tuiles + actions -->
}
else
{
    <div style="margin-top:2rem;text-align:center;color:#888;">
        <p>Selectionnez un scenario pour commencer.</p>
        <DxButton Text="Creer un scenario"
                  Click="() => Nav.NavigateTo("/scenario")"
                  RenderStyle="ButtonRenderStyle.Primary" />
    </div>
}
```

**Pattern avec données nullables** (Résultats) :

```razor
@if (resultat is not null && snapshot is not null)
{
    <!-- Affichage resultats -->
}
else if (snapshots.Count == 0)
{
    <p>Aucun calcul disponible.</p>
}
```

### 7.3 Error states

**Pattern inline coloré** :

```razor
@if (editError is not null)
{
    <div style="background:#fce4ec;color:#c62828;padding:0.5rem 0.75rem;
                border-radius:4px;margin-bottom:0.75rem;font-size:0.9rem;">
        @editError
    </div>
}
```

**Pattern avec success/error** (ScenarioEdit) :

```razor
@if (message is not null)
{
    <div style="@(isError
        ? "background:#fce4ec;color:#c62828;"
        : "background:#e8f5e9;color:#2e7d32;")
        padding:0.5rem 0.75rem;border-radius:4px;flex-shrink:0;">
        @message
    </div>
}
```

### 7.4 Alertes collapsibles

**Pattern toggle + grid** (Equipage) :

```razor
@if (alertes.Count > 0)
{
    <DxStackLayoutItem Length="auto">
        <Template>
            <DxButton Text="@($"Alertes ({alertes.Count})")"
                      Click="() => alertesVisibles = !alertesVisibles"
                      IconCssClass="@(alertesVisibles ? "oi oi-chevron-top" : "oi oi-chevron-bottom")" />
            @if (alertesVisibles)
            {
                <DxGrid Data="alertes" ShowFilterRow="true">
                    <Columns>
                        <DxGridDataColumn FieldName="Membre" />
                        <DxGridDataColumn FieldName="Message" />
                    </Columns>
                </DxGrid>
            }
        </Template>
    </DxStackLayoutItem>
}
```

---

## 8. Patterns de styling

### 8.1 CSS scoped (composants layout)

Les composants layout utilisent le CSS scoped Blazor (`.razor.css`) avec `::deep` pour cibler les éléments internes DevExpress :

```css
/* MainLayout.razor.css */
::deep .navigation-drawer {
    --dxbl-drawer-panel-body-padding-x: 0;
}

/* NavMenu.razor.css */
::deep .menu {
    --dxbl-menu-item-padding-x: 1.125rem;
}

/* Drawer.razor.css */
::deep .navigation-drawer > .dxbl-drawer-panel {
    display: flex;
}
```

**Convention** : le sélecteur `::deep` est nécessaire car les composants DevExpress génèrent leur propre DOM interne, hors de la portée du CSS scoped Blazor.

### 8.2 CSS inline dans .razor (pages)

Les pages utilisent des blocs `<style>` directement dans le fichier `.razor` pour les styles spécifiques :

```razor
<!-- Dashboard.razor -->
<style>
    .dashboard-tile { ... }
    .badge-green { background: #e8f5e9; color: #2e7d32; }
    .badge-orange { background: #fff3e0; color: #e65100; }
    .badge-red { background: #fce4ec; color: #c62828; }
</style>
```

**Avantages** : colocation du style avec le composant, évite conflits globaux.

**Pages utilisant ce pattern** :
- Dashboard : `.dashboard-tile`, `.badge-*`
- Equipage : `.kpi-tile`, `.matrice-cell`, `.cell-valide/proche/expire`
- Resultats : `.result-card`, `.result-label/value`, `.badge`
- AuthLayout : tout le styling auth (gradient, form, inputs)

### 8.3 Classes utilitaires globales (site.css)

**Bootstrap-like** :
- `.w-100`, `.h-100`, `.h-auto`
- `.overflow-hidden`
- `.text-danger`

**Viewport-fill** :
- `.page-fill`, `.grid-fill`, `.scroll-fill`
- `.dxc-full-height`

**Responsive** :
- `.display-desktop` / `.display-mobile` (breakpoint 768px)

**Alertes DevExpress** :
- `.alert`, `.alert-danger`, `.alert-warning`, `.alert-success`

**Typographie** :
- `.title`, `.title-secondary`, `.title-header-text`, `.title-content-text`
- Utilise variables DevExpress (`--dxds-font-size-headline-lg`)

### 8.4 Badges statut

Pattern récurrent pour afficher un indicateur coloré basé sur un seuil :

```csharp
static string BadgeCss(double taux)
{
    if (taux < 0.85) return "badge-green";
    if (taux < 0.95) return "badge-orange";
    return "badge-red";
}
```

```razor
<span class="badge @BadgeCss(resultat.TauxEngagementGlobal)">
    @(resultat.TauxEngagementGlobal.ToString("P1"))
</span>
```

```css
.badge-green  { background: #e8f5e9; color: #2e7d32; }
.badge-orange { background: #fff3e0; color: #e65100; }
.badge-red    { background: #fce4ec; color: #c62828; }
```

### 8.5 Système de couleurs DevExpress Design System

Les variables CSS DevExpress suivent la convention `--dxds-*` (Design System) :

| Catégorie | Exemple | Usage |
|---|---|---|
| Surface | `--dxds-color-surface-primary-default-rest` | Fond du drawer |
| Content | `--dxds-color-content-neutral-default-static-dark-rest` | Texte menu |
| Backdrop | `--dxds-color-surface-backdrop-default-rest` | Overlay shading |
| Primary | `--dxds-primary-170` | Nuance primaire dégradée |

Les composants DevExpress utilisent aussi des variables internes `--dxbl-*` (Blazor) pour le contrôle fin du layout (padding, border, background).

---

## 9. Patterns spécifiques

### 9.1 Master-detail (EquipageMembres)

**Layout** : `DxGridLayout` 2 colonnes (45fr/55fr)

```
+------- 45fr -------+------- 55fr -------+
|                     |                     |
|  DxGrid membres     |  DxFormLayout       |
|  FocusedRowEnabled  |  detail membre      |
|  ShowFilterRow      |  + sous-grid        |
|                     |  qualifications     |
+---------------------+---------------------+
```

```csharp
async Task OnFocusedRowChanged(GridFocusedRowChangedEventArgs e)
{
    if (e.DataItem is MembreEquipageDto dto)
        detail = await Mediator.Send(new GetMembreDetailQuery(dto.Id));
}
```

### 9.2 Matrice dynamique (EquipageMatrice)

Colonnes générées dynamiquement via `foreach` :

```razor
@foreach (var checkCode in matrice.CodesChecks)
{
    var cc = checkCode;  // Capture pour le lambda
    <DxGridDataColumn FieldName="Code" Caption="@cc"
                      UnboundType="GridUnboundColumnType.String">
        <CellDisplayTemplate>
            @{
                var ligne = (MatriceLigneDto)context.DataItem;
                var cell = ligne.Checks.GetValueOrDefault(cc);
                if (cell is not null)
                {
                    <span class="matrice-cell @CellCss(cell.Statut)">
                        @CellIcon(cell.Statut)
                    </span>
                }
            }
        </CellDisplayTemplate>
    </DxGridDataColumn>
}
```

**Styling conditionnel** basé sur l'enum `StatutCheck` :

```css
.cell-valide  { background: #e8f5e9; }
.cell-proche  { background: #fff3e0; }
.cell-expire  { background: #fce4ec; }
```

### 9.3 Import fichiers (EquipageImport)

**Pattern** : `InputFile` Blazor + conversion `MemoryStream` + `EventCallback` pour refresh parent :

```razor
<InputFile OnChange="e => filePnt = e.File" accept=".xml" />

<DxButton Text="Importer" Click="DoImport"
          Enabled="@(filePnt is not null && filePnc is not null && ...)" />
```

```csharp
async Task DoImport()
{
    importing = true;
    try
    {
        var streamPnt = await CopyToMemoryStream(filePnt);
        var streamPnc = await CopyToMemoryStream(filePnc);
        // ...
        importResult = await Mediator.Send(new ImportEquipageCommand(streamPnt, streamPnc, ...));
        await OnImportComplete.InvokeAsync();  // Callback pour refresh parent
    }
    finally { importing = false; }
}

static async Task<MemoryStream> CopyToMemoryStream(IBrowserFile? file)
{
    if (file is null) throw new InvalidOperationException();
    var ms = new MemoryStream();
    await file.OpenReadStream().CopyToAsync(ms);
    ms.Position = 0;
    return ms;
}
```

### 9.4 Authentification SSR (pages Auth)

**Spécificités** :
- `@layout AuthLayout` — layout minimaliste sans drawer
- `@attribute [AllowAnonymous]` — accès sans authentification
- Rendu SSR statique (pas de circuit SignalR) — configuré dans `App.razor`
- `EditForm` avec `method="post"` (formulaire HTML classique)
- `forceLoad: true` pour la redirection post-login (nécessaire pour établir le circuit SignalR)

```razor
@page "/login"
@layout AuthLayout
@attribute [AllowAnonymous]

<EditForm Model="Input" FormName="login" OnValidSubmit="LoginUser"
          class="auth-form" method="post">
    <DataAnnotationsValidator />
    <AntiforgeryToken />

    <InputText @bind-Value="Input.UserName" placeholder="Nom d'utilisateur" />
    <InputText @bind-Value="Input.Password" type="password" placeholder="Mot de passe" />

    <button type="submit">Se connecter</button>
</EditForm>
```

```csharp
[CascadingParameter] HttpContext HttpContext { get; set; } = null!;

protected override Task OnInitializedAsync()
{
    // Rediriger si deja authentifie (GET uniquement)
    if (HttpMethods.IsGet(HttpContext.Request.Method))
    {
        if (HttpContext.User.Identity?.IsAuthenticated == true)
            NavigationManager.NavigateTo("/", forceLoad: true);
    }
    return Task.CompletedTask;
}

async Task LoginUser()
{
    var result = await SignInManager.PasswordSignInAsync(Input.UserName, Input.Password, false, false);
    if (result.Succeeded)
        NavigationManager.NavigateTo("/", forceLoad: true);
    else
        errorMessage = "Identifiants invalides.";
}
```

---

## 10. Anti-patterns et workarounds

### 10.1 DxTextBox ReadOnly ne se met pas à jour

**Problème** : `<DxTextBox Value="@detail.Code" ReadOnly="true" />` ne se met pas à jour au re-rendu, même avec `ValueChanged` no-op.

**Solution** : utiliser un `<input>` HTML standard :

```razor
<!-- Mauvais -->
<DxTextBox Value="@detail.Code" ReadOnly="true" />

<!-- Bon -->
<input type="text" class="form-control" readonly value="@detail.Code" />
```

### 10.2 Razor switch pattern avec opérateurs de comparaison

**Problème** : `< 0.85` dans un switch pattern Razor est interprété comme du HTML (balise ouvrante).

**Solution** : utiliser if/else au lieu de switch :

```csharp
// Mauvais — Razor parse erreur
var css = taux switch {
    < 0.85 => "badge-green",    // < interprete comme HTML
    < 0.95 => "badge-orange",
    _ => "badge-red"
};

// Bon
string css;
if (taux < 0.85) css = "badge-green";
else if (taux < 0.95) css = "badge-orange";
else css = "badge-red";
```

### 10.3 DxSpinEdit Increment decimal vs double

**Problème** : `Increment="0.5m"` (literal decimal) est incompatible avec `DxSpinEdit<double>`.

**Solution** : utiliser le literal double :

```razor
<!-- Mauvais -->
<DxSpinEdit @bind-Value="model.ValeurDouble" Increment="0.5m" />

<!-- Bon -->
<DxSpinEdit @bind-Value="model.ValeurDouble" Increment="0.5" />
```

### 10.4 DxPopup BodyTemplate — Conflit de `context`

**Problème** : `<DxPopup><BodyTemplate>` et `<DxFormLayoutItem><Template>` utilisent tous deux la variable implicite `context`, créant un conflit de compilation.

**Solution** : nommer explicitement le `Context` :

```razor
<DxPopup>
    <BodyTemplate Context="popupCtx">
        <DxFormLayout>
            <DxFormLayoutItem>
                <Template Context="itemCtx">
                    <!-- Pas de conflit -->
                </Template>
            </DxFormLayoutItem>
        </DxFormLayout>
    </BodyTemplate>
</DxPopup>
```

### 10.5 DbContext Scoped — Queries séquentielles

**Problème** : en Blazor Server, le `DbContext` est Scoped (un par circuit SignalR). Deux queries EF Core parallèles sur le même `DbContext` causent une exception.

**Solution** : exécuter les queries séquentiellement, et rendre les enfants après le chargement du parent :

```csharp
// Mauvais — erreur de concurrence
await Task.WhenAll(
    Mediator.Send(new GetAllVolsQuery()),
    Mediator.Send(new GetAllBlocsQuery())
);

// Bon — sequentiel
vols = await Mediator.Send(new GetAllVolsQuery());
blocs = await Mediator.Send(new GetAllBlocsQuery());
loaded = true;  // Maintenant les enfants peuvent se rendre
```

### 10.6 ResultatMarge — Tuples et JSON

**Problème** : `ResultatMarge` contient des tuples C# `(double, bool)` qui ne sont pas sérialisés par défaut par `System.Text.Json`.

**Solution** : activer `IncludeFields` :

```csharp
var options = new JsonSerializerOptions { IncludeFields = true };
var json = JsonSerializer.Serialize(resultat, options);
```

### 10.7 UseXminAsConcurrencyToken retiré dans Npgsql 9.x

**Problème** : `UseXminAsConcurrencyToken()` n'existe plus dans Npgsql 9.x.

**Solution** : configurer manuellement la propriété `xmin` :

```csharp
// Dans la configuration EF Core (Fluent API)
entity.Property<uint>("xmin")
    .HasColumnName("xmin")
    .IsRowVersion();
```

---

## 11. Checklist pour nouvelles pages

### 11.1 Template par type de page

#### Page avec grille CRUD

```razor
@page "/monentite"
@inject IMediator Mediator

<div class="page-fill">
    <h3>Mon entite</h3>

    <DxGrid @ref="grid" Data="@items" CssClass="grid-fill"
            VirtualScrollingEnabled="true"
            EditMode="GridEditMode.PopupEditForm"
            CustomizeEditModel="OnCustomizeEditModel"
            EditModelSaving="OnSaving"
            DataItemDeleting="OnDeleting"
            PopupEditFormHeaderText="@popupTitle">
        <ToolbarTemplate>
            <DxToolbar>
                <DxToolbarItem Text="Nouveau"
                               Click="@(() => grid.StartEditNewRowAsync())"
                               RenderStyle="ButtonRenderStyle.Primary" />
            </DxToolbar>
        </ToolbarTemplate>
        <Columns>
            <DxGridDataColumn FieldName="Code" Caption="Code" />
            <DxGridCommandColumn Width="120px" />
        </Columns>
        <EditFormTemplate Context="ctx">
            @{ var edit = (MonEdit)ctx.EditModel; }
            @if (editError is not null) {
                <div style="background:#fce4ec;color:#c62828;padding:0.5rem;border-radius:4px;">
                    @editError
                </div>
            }
            <DxFormLayout>
                <DxFormLayoutItem Caption="Code" ColSpanMd="12">
                    <DxTextBox @bind-Text="edit.Code" />
                </DxFormLayoutItem>
            </DxFormLayout>
        </EditFormTemplate>
    </DxGrid>
</div>
```

#### Page avec formulaire scrollable

```razor
@page "/monentite/{Id:guid}"
@inject IMediator Mediator
@inject NavigationManager Nav

<div class="page-fill">
    @if (loading) {
        <p>Chargement...</p>
    }
    else if (model is null) {
        <p>Entite introuvable.</p>
    }
    else {
        <DxToolbar style="flex-shrink:0;">
            <DxToolbarItem Text="Enregistrer" Click="Save"
                           RenderStyle="ButtonRenderStyle.Primary" />
            <DxToolbarItem Text="Retour" Click="() => Nav.NavigateTo("/monentite")"
                           Alignment="ToolbarItemAlignment.Right" />
        </DxToolbar>

        @if (message is not null) {
            <div style="...flex-shrink:0;">@message</div>
        }

        <div class="scroll-fill">
            <DxFormLayout>
                <DxFormLayoutGroup Caption="Section 1" ColSpanMd="12">
                    <DxFormLayoutItem Caption="Champ" ColSpanMd="6">
                        <DxTextBox @bind-Text="model.Champ" />
                    </DxFormLayoutItem>
                </DxFormLayoutGroup>
            </DxFormLayout>
        </div>
    }
</div>
```

#### Page avec onglets

```razor
@page "/monentite"

<DxStackLayout Orientation="Orientation.Vertical" style="height:100%;">
    <Items>
        <DxStackLayoutItem Length="auto">
            <Template>
                <h3>Mon entite</h3>
            </Template>
        </DxStackLayoutItem>
        <DxStackLayoutItem Length="1fr">
            <Template>
                <DxTabs CssClass="dxc-full-height" @bind-ActiveTabIndex="activeTab">
                    <DxTabPage Text="Onglet 1">
                        <div style="padding:1rem;overflow:auto;">
                            <!-- Contenu onglet -->
                        </div>
                    </DxTabPage>
                    <DxTabPage Text="Onglet 2">
                        <ComponentEnfant />
                    </DxTabPage>
                </DxTabs>
            </Template>
        </DxStackLayoutItem>
    </Items>
</DxStackLayout>
```

### 11.2 Tableau récapitulatif — Toutes les pages existantes

| Page | Route | Layout | Pattern CRUD | Rendu conditionnel | CSS |
|---|---|---|---|---|---|
| Dashboard | `/` | `.page-fill` + DxGridLayout | Aucun | `ActiveScenario.HasScenario` | Inline tiles, badges |
| ScenarioList | `/scenario` | `.page-fill` + DxGrid | DxPopup manual (Create/Clone/Delete) | Empty state | Inline error |
| ScenarioEdit | `/scenario/{Id:guid}` | `.page-fill` + `.scroll-fill` + DxFormLayout | Manual Save/RunCalcul | Loading/null model | Inline tables |
| Vols | `/programme/vols` | `.page-fill` + DxGrid | PopupEditForm | EditFormTemplate error | Inline popup |
| Blocs | `/programme/blocs` | `.page-fill` + DxGrid | PopupEditForm + collections | EditFormTemplate error | Inline popup |
| Semaines | `/programme/semaines` | `.page-fill` + DxGrid | PopupEditForm + collections | EditFormTemplate error | Inline popup |
| Calendrier | `/programme/calendrier` | `.page-fill` + DxGrid | EditRow + ComboBox | ActiveScenario check | Standard |
| Equipage | `/equipage` | DxStackLayout + DxTabs | Import modal | KPI null, alertes toggle, loaded | Inline KPI, matrice |
| Resultats | `/resultats` | DxStackLayout + DxTabs | Aucun | Resultat null, snapshots empty | Inline cards, badges |
| Login | `/login` | AuthLayout (SSR) | EditForm post | Error message | AuthLayout inline |
| Register | `/register` | AuthLayout (SSR) | EditForm post | Error message | AuthLayout inline |
| Logout | `/logout` | AuthLayout (SSR) | SignOutAsync | - | - |

### 11.3 Pattern recommandé par use case

| Use case | Pattern recommandé |
|---|---|
| Liste simple readonly | DxGrid + `ShowFilterRow` + `grid-fill` |
| Liste CRUD simple | DxGrid + `PopupEditForm` (modèle plat) |
| Liste CRUD avec relations | DxGrid + `PopupEditForm` (modèle + collections, tables imbriquées) |
| Formulaire multi-sections | DxFormLayout + `DxFormLayoutGroup` repliables + `.scroll-fill` |
| Dashboard / KPIs | DxGridLayout (colonnes égales auto-responsive) |
| Master-detail | DxGridLayout 2-col (45/55) + `FocusedRow` |
| Onglets verticaux | DxStackLayout + DxTabs + `.dxc-full-height` |
| Données tabulaires lourdes | DxGrid + `VirtualScrollingEnabled="true"` |
| Import fichiers | `InputFile` + `MemoryStream` + `EventCallback` |
| Page auth SSR | `AuthLayout` + `EditForm` + `method="post"` |

---

> *Document généré à partir de l'analyse du code source CrewSizer.Web — Phase 3 (plan-008).*
> *Dernière mise à jour : février 2026.*
