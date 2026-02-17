using CrewSizer.Commands;
using CrewSizer.IO;
using CrewSizer.Models;

namespace CrewSizer.Tui;

public class TuiApp
{
    private readonly ITheme _theme;
    private readonly ScreenRenderer _renderer;
    private readonly OutputBuffer _buffer = new();
    private readonly CommandHandler _handler;
    private readonly AppSettings _settings;

    private string _inputBuffer = "";
    private int _cursorPos;
    private readonly List<string> _history = [];
    private int _historyIndex = -1;

    private HelpOverlay? _helpOverlay;
    private FormOverlay? _formOverlay;
    private ProgrammeOverlay? _programmeOverlay;
    private FileMenuOverlay? _fileMenuOverlay;
    private CrewOverlay? _crewOverlay;

    public TuiApp(Configuration config, ITheme theme,
        string? paramPath, string? progPath, string? volsPath, string? equipagePath = null,
        AppSettings? settings = null)
    {
        _theme = theme;
        _settings = settings ?? new AppSettings();
        _renderer = new ScreenRenderer(theme);
        _handler = new CommandHandler(config, _buffer, theme, paramPath, progPath, volsPath, equipagePath, _settings);

        _buffer.WriteLine("CrewSizer -- Marge d'engagement equipage");
        if (paramPath != null || progPath != null || volsPath != null || equipagePath != null)
        {
            if (paramPath != null) _buffer.WriteLine($"Parametres : {Path.GetFileName(paramPath)}");
            if (progPath != null) _buffer.WriteLine($"Programme  : {Path.GetFileName(progPath)}");
            if (volsPath != null) _buffer.WriteLine($"Catalogue  : {Path.GetFileName(volsPath)}");
            if (equipagePath != null) _buffer.WriteLine($"Equipage   : {Path.GetFileName(equipagePath)}");
            var p = config.Periode;
            if (!string.IsNullOrEmpty(p.Mois))
                _buffer.WriteLine($"Periode : {p.Mois} {p.Annee} ({p.NbJours} jours)");
        }
        else
        {
            _buffer.WriteLine("Nouvelle configuration.");
            _buffer.WriteLine("  F2  Editer les parametres");
            _buffer.WriteLine("  F3  Editer le programme (semaines types, blocs, vols)");
            _buffer.WriteLine("  F4  Menu fichier (ouvrir, nouveau, enregistrer)");
            _buffer.WriteLine("  calc   Lancer le calcul");
        }
        _buffer.WriteLine();
        _buffer.WriteLine("Tapez une commande ou appuyez sur F1 pour l'aide.");
    }

    public void Run()
    {
        Console.CursorVisible = false;
        Console.Clear();

        try
        {
            Render();

            while (true)
            {
                var key = Console.ReadKey(true);

                if (!HandleKey(key))
                    break;

                Render();
            }
        }
        finally
        {
            Console.CursorVisible = true;
            Console.ResetColor();
            Console.Write(_theme.Reset);
            Console.Clear();
        }
    }

    private bool HandleKey(ConsoleKeyInfo key)
    {
        // Modal: file menu overlay intercepts all keys
        if (_fileMenuOverlay is { IsVisible: true })
        {
            if (!_fileMenuOverlay.HandleKey(key))
            {
                HandleFileMenuResult();
                _fileMenuOverlay = null;
            }
            return true;
        }

        // Modal: crew overlay intercepts all keys
        if (_crewOverlay is { IsVisible: true })
        {
            if (!_crewOverlay.HandleKey(key))
            {
                HandleCrewImportResult();
                if (_crewOverlay.Modified)
                    _handler.MarkDirty();
                _crewOverlay = null;
            }
            return true;
        }

        // Modal: programme overlay intercepts all keys
        if (_programmeOverlay is { IsVisible: true })
        {
            if (!_programmeOverlay.HandleKey(key))
            {
                if (_programmeOverlay.Modified)
                    _handler.MarkDirty();
                _programmeOverlay = null;
            }
            return true;
        }

        // Modal: form overlay intercepts all keys
        if (_formOverlay is { IsVisible: true })
        {
            if (!_formOverlay.HandleKey(key))
            {
                if (_formOverlay.Applied)
                {
                    _formOverlay.ApplyTo(_handler.Config);
                    _handler.MarkDirty();
                    _buffer.WriteLine($"{_theme.AlertOk}Configuration mise a jour.{_theme.Reset}");
                    _buffer.WriteLine();
                }
                _formOverlay = null;
            }
            return true;
        }

        // Modal: help overlay intercepts all keys
        if (_helpOverlay is { IsVisible: true })
        {
            if (!_helpOverlay.HandleKey(key))
                _helpOverlay = null;
            return true;
        }

        switch (key.Key)
        {
            case ConsoleKey.F10:
            case ConsoleKey.Escape:
                return false;

            case ConsoleKey.F1:
                _helpOverlay = new HelpOverlay();
                break;

            case ConsoleKey.F2:
                _formOverlay = FormOverlay.FromConfig(_handler.Config);
                break;

            case ConsoleKey.F3:
                _programmeOverlay = ProgrammeOverlay.FromConfig(_handler.Config, _handler.MarkDirty);
                break;

            case ConsoleKey.F4:
                _fileMenuOverlay = FileMenuOverlay.FromCurrentState(
                    _handler.ParamPath, _handler.ProgPath, _handler.VolsPath,
                    _handler.EquipagePath, _settings.RepertoireEffectif);
                break;

            case ConsoleKey.F5:
                _crewOverlay = CrewOverlay.FromConfig(_handler.Config, _handler.MarkDirty,
                    _settings.RepertoireEffectif);
                break;

            case ConsoleKey.Enter:
                if (!string.IsNullOrWhiteSpace(_inputBuffer))
                {
                    var cmd = _inputBuffer.Trim();
                    _history.Add(cmd);
                    _historyIndex = _history.Count;
                    _inputBuffer = "";
                    _cursorPos = 0;

                    if (cmd is "quit" or "exit")
                        return false;

                    ExecuteCommand(cmd);
                }
                break;

            case ConsoleKey.Backspace:
                if (_cursorPos > 0)
                {
                    _inputBuffer = _inputBuffer.Remove(_cursorPos - 1, 1);
                    _cursorPos--;
                }
                break;

            case ConsoleKey.Delete:
                if (_cursorPos < _inputBuffer.Length)
                    _inputBuffer = _inputBuffer.Remove(_cursorPos, 1);
                break;

            case ConsoleKey.LeftArrow:
                if (_cursorPos > 0) _cursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (_cursorPos < _inputBuffer.Length) _cursorPos++;
                break;

            case ConsoleKey.Home:
                _cursorPos = 0;
                break;

            case ConsoleKey.End:
                _cursorPos = _inputBuffer.Length;
                break;

            case ConsoleKey.UpArrow:
                if (_history.Count > 0 && _historyIndex > 0)
                {
                    _historyIndex--;
                    _inputBuffer = _history[_historyIndex];
                    _cursorPos = _inputBuffer.Length;
                }
                break;

            case ConsoleKey.DownArrow:
                if (_historyIndex < _history.Count - 1)
                {
                    _historyIndex++;
                    _inputBuffer = _history[_historyIndex];
                    _cursorPos = _inputBuffer.Length;
                }
                else
                {
                    _historyIndex = _history.Count;
                    _inputBuffer = "";
                    _cursorPos = 0;
                }
                break;

            case ConsoleKey.PageUp:
                _buffer.PageUp(_renderer.ViewportHeight);
                break;

            case ConsoleKey.PageDown:
                _buffer.PageDown(_renderer.ViewportHeight);
                break;

            default:
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    _inputBuffer = _inputBuffer.Insert(_cursorPos, key.KeyChar.ToString());
                    _cursorPos++;
                }
                break;
        }

        return true;
    }

    private void ExecuteCommand(string command)
    {
        _buffer.WriteLine($"{_theme.CommandEcho}> {command}{_theme.Reset}");

        if (command.Trim().Equals("edit", StringComparison.OrdinalIgnoreCase))
        {
            _formOverlay = FormOverlay.FromConfig(_handler.Config);
            return;
        }

        if (command.Trim().Equals("prog", StringComparison.OrdinalIgnoreCase))
        {
            _programmeOverlay = ProgrammeOverlay.FromConfig(_handler.Config, _handler.MarkDirty);
            return;
        }

        if (command.Trim() is "crew" or "equipage")
        {
            _crewOverlay = CrewOverlay.FromConfig(_handler.Config, _handler.MarkDirty);
            return;
        }

        _handler.ExecuteCommand(command);
        _buffer.WriteLine();
    }

    private void HandleFileMenuResult()
    {
        if (_fileMenuOverlay?.ResultAction == null || _fileMenuOverlay.ResultPath == null)
            return;

        var action = _fileMenuOverlay.ResultAction.Value;
        var path = _fileMenuOverlay.ResultPath;
        var fileType = _fileMenuOverlay.ActiveTabType;

        // For legacy files, detect actual type
        if (_fileMenuOverlay.IsLegacyFile(path))
            fileType = ConfigFileType.Legacy;

        switch (action)
        {
            case FileAction.New:
                _handler.SaveConfigAs(path, fileType);
                _buffer.WriteLine($"{_theme.AlertOk}Nouveau fichier : {Path.GetFileName(path)}{_theme.Reset}");
                _buffer.WriteLine();
                break;

            case FileAction.Open:
                try
                {
                    _handler.LoadConfig(path, fileType);
                    var typeLabel = fileType switch
                    {
                        ConfigFileType.Parametres => "Parametres",
                        ConfigFileType.Programme => "Programme",
                        ConfigFileType.CatalogueVols => "Catalogue vols",
                        ConfigFileType.Equipage => "Equipage",
                        ConfigFileType.Legacy => "Configuration legacy",
                        _ => "Fichier"
                    };
                    _buffer.WriteLine($"{_theme.AlertOk}{typeLabel} charge : {Path.GetFileName(path)}{_theme.Reset}");
                    if (fileType is ConfigFileType.Parametres or ConfigFileType.Programme or ConfigFileType.Legacy)
                    {
                        var p = _handler.Config.Periode;
                        if (!string.IsNullOrEmpty(p.Mois))
                            _buffer.WriteLine($"Periode : {p.Mois} {p.Annee} ({p.NbJours} jours)");
                    }
                    _buffer.WriteLine();
                }
                catch (Exception ex)
                {
                    _buffer.WriteLine($"{_theme.AlertExceeded}Erreur chargement : {ex.Message}{_theme.Reset}");
                    _buffer.WriteLine();
                }
                break;

            case FileAction.SaveAs:
                try
                {
                    _handler.SaveConfigAs(path, fileType);
                    _buffer.WriteLine($"{_theme.AlertOk}Enregistre : {Path.GetFileName(path)}{_theme.Reset}");
                    _buffer.WriteLine();
                }
                catch (Exception ex)
                {
                    _buffer.WriteLine($"{_theme.AlertExceeded}Erreur sauvegarde : {ex.Message}{_theme.Reset}");
                    _buffer.WriteLine();
                }
                break;

            case FileAction.Delete:
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    var isParam = _handler.ParamPath != null &&
                        string.Equals(fullPath, Path.GetFullPath(_handler.ParamPath), StringComparison.OrdinalIgnoreCase);
                    var isProg = _handler.ProgPath != null &&
                        string.Equals(fullPath, Path.GetFullPath(_handler.ProgPath), StringComparison.OrdinalIgnoreCase);
                    var isVols = _handler.VolsPath != null &&
                        string.Equals(fullPath, Path.GetFullPath(_handler.VolsPath), StringComparison.OrdinalIgnoreCase);
                    var isEquip = _handler.EquipagePath != null &&
                        string.Equals(fullPath, Path.GetFullPath(_handler.EquipagePath), StringComparison.OrdinalIgnoreCase);

                    File.Delete(path);
                    _buffer.WriteLine($"{_theme.AlertOk}Fichier supprime : {Path.GetFileName(path)}{_theme.Reset}");

                    if (isParam || isProg || isVols || isEquip)
                    {
                        _handler.NewConfig();
                        _buffer.WriteLine("Configuration reinitialise (fichier actif supprime).");
                    }
                    _buffer.WriteLine();
                }
                catch (Exception ex)
                {
                    _buffer.WriteLine($"{_theme.AlertExceeded}Erreur suppression : {ex.Message}{_theme.Reset}");
                    _buffer.WriteLine();
                }
                break;

            case FileAction.SetDirectory:
                if (Directory.Exists(path))
                {
                    _settings.RepertoireSources = Path.GetFullPath(path);
                    _settings.Sauvegarder();
                    _buffer.WriteLine($"{_theme.AlertOk}Repertoire de sources : {_settings.RepertoireSources}{_theme.Reset}");
                }
                else
                {
                    _buffer.WriteLine($"{_theme.AlertExceeded}Repertoire introuvable : {path}{_theme.Reset}");
                }
                _buffer.WriteLine();
                break;
        }
    }

    private void HandleCrewImportResult()
    {
        if (_crewOverlay?.ImportResultTab == null || _crewOverlay.ImportResultPath == null)
            return;

        var tab = _crewOverlay.ImportResultTab.Value;
        var path = _crewOverlay.ImportResultPath;

        try
        {
            var tabLabel = tab switch
            {
                ImportTab.PNT => "PNT Crew List",
                ImportTab.PNC => "PNC Crew List",
                ImportTab.CheckStatus => "Check Status",
                ImportTab.CheckDesc => "Check Descriptions",
                _ => "Fichier"
            };

            // CheckStatus requires existing members - apply directly
            if (tab == ImportTab.CheckStatus)
            {
                var eq = _handler.Config.Equipage;
                if (eq == null || eq.Membres.Count == 0)
                {
                    _buffer.WriteLine($"{_theme.AlertExceeded}Importez d'abord les crew lists (PNT/PNC) avant le Check Status.{_theme.Reset}");
                    _buffer.WriteLine();
                    return;
                }

                var updated = EquipageLoader.AppliquerCheckStatuses(path, eq.Membres);
                _handler.MarkDirty();
                _buffer.WriteLine($"{_theme.AlertOk}Import {tabLabel} : {Path.GetFileName(path)}{_theme.Reset}");
                _buffer.WriteLine($"  {updated} membres mis a jour avec leurs qualifications");
                _buffer.WriteLine();
                return;
            }

            // CheckDesc can be imported independently
            if (tab == ImportTab.CheckDesc)
            {
                var newEquipage = EquipageLoader.Charger(cheminCheckDesc: path);
                _handler.Config.Equipage ??= new DonneesEquipage { DateExtraction = DateTime.Today };
                // Replace check definitions
                _handler.Config.Equipage.Checks = newEquipage.Checks;
                _handler.MarkDirty();
                _buffer.WriteLine($"{_theme.AlertOk}Import {tabLabel} : {Path.GetFileName(path)}{_theme.Reset}");
                _buffer.WriteLine($"  {newEquipage.Checks.Count} definitions de checks");
                _buffer.WriteLine();
                return;
            }

            // PNT/PNC crew lists
            var equipage = tab switch
            {
                ImportTab.PNT => EquipageLoader.Charger(cheminPnt: path),
                ImportTab.PNC => EquipageLoader.Charger(cheminPnc: path),
                _ => new DonneesEquipage()
            };

            if (_handler.Config.Equipage == null)
            {
                _handler.Config.Equipage = equipage;
            }
            else
            {
                MergeEquipage(_handler.Config.Equipage, equipage);
            }

            _handler.MarkDirty();
            _buffer.WriteLine($"{_theme.AlertOk}Import {tabLabel} : {Path.GetFileName(path)}{_theme.Reset}");
            _buffer.WriteLine($"  {equipage.Membres.Count} membres");
            _buffer.WriteLine();
        }
        catch (Exception ex)
        {
            _buffer.WriteLine($"{_theme.AlertExceeded}Erreur import : {ex.Message}{_theme.Reset}");
            _buffer.WriteLine();
        }
    }

    private static void MergeEquipage(DonneesEquipage cible, DonneesEquipage source)
    {
        // Merge membres by Code
        var membresDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < cible.Membres.Count; i++)
            membresDict[cible.Membres[i].Code] = i;

        foreach (var src in source.Membres)
        {
            if (membresDict.TryGetValue(src.Code, out var idx))
                cible.Membres[idx] = src; // replace
            else
                cible.Membres.Add(src);
        }

        // Merge checks by Code
        var checksDict = new HashSet<string>(cible.Checks.Select(c => c.Code), StringComparer.OrdinalIgnoreCase);
        foreach (var src in source.Checks)
        {
            if (!checksDict.Contains(src.Code))
                cible.Checks.Add(src);
        }

        // Keep most recent extraction date
        if (source.DateExtraction > cible.DateExtraction)
            cible.DateExtraction = source.DateExtraction;
    }

    private void Render()
    {
        _renderer.UpdateSize();
        _renderer.BeginFrame();

        var configName = GetConfigName();
        var period = GetPeriodString();
        var statusExtra = _handler.IsDirty ? "[modifie]" : "";

        _renderer.RenderFrame(configName, period, statusExtra);

        if (_fileMenuOverlay is { IsVisible: true })
        {
            _renderer.RenderBlankViewport();
            _renderer.RenderFileMenuOverlay(_fileMenuOverlay);
        }
        else if (_crewOverlay is { IsVisible: true })
        {
            _renderer.RenderBlankViewport();
            _renderer.RenderCrewOverlay(_crewOverlay);
        }
        else if (_programmeOverlay is { IsVisible: true })
        {
            _renderer.RenderBlankViewport();
            _renderer.RenderProgrammeOverlay(_programmeOverlay);
        }
        else if (_formOverlay is { IsVisible: true })
        {
            _renderer.RenderBlankViewport();
            _renderer.RenderFormOverlay(_formOverlay);
        }
        else if (_helpOverlay is { IsVisible: true })
        {
            _renderer.RenderBlankViewport();
            _renderer.RenderHelpOverlay(_helpOverlay);
        }
        else
        {
            _renderer.RenderContent(_buffer);
            _renderer.RenderInputLine(_inputBuffer, _cursorPos);
        }

        var showCursor = _fileMenuOverlay is { IsVisible: true, IsEditing: true }
                      || _crewOverlay is { IsVisible: true, IsEditing: true }
                      || _programmeOverlay is { IsVisible: true, IsEditing: true }
                      || _formOverlay is { IsVisible: true, IsEditing: true }
                      || (_fileMenuOverlay is not { IsVisible: true }
                          && _crewOverlay is not { IsVisible: true }
                          && _programmeOverlay is not { IsVisible: true }
                          && _formOverlay is not { IsVisible: true }
                          && _helpOverlay is not { IsVisible: true });
        _renderer.SetCursorVisible(showCursor);

        _renderer.FlushFrame();
    }

    private string GetConfigName()
    {
        var parts = new List<string>();
        if (_handler.ParamPath != null)
            parts.Add($"P:{Path.GetFileName(_handler.ParamPath)}");
        if (_handler.ProgPath != null)
            parts.Add($"Pr:{Path.GetFileName(_handler.ProgPath)}");
        if (_handler.VolsPath != null)
            parts.Add($"V:{Path.GetFileName(_handler.VolsPath)}");
        return parts.Count > 0 ? string.Join("  ", parts) : "(non sauvegarde)";
    }

    private string GetPeriodString()
    {
        var p = _handler.Config.Periode;
        if (string.IsNullOrEmpty(p.Mois))
            return "";
        return $"{p.Mois} {p.Annee} ({p.NbJours} j)";
    }
}
