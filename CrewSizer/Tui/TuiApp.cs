using CrewSizer.Commands;
using CrewSizer.Models;

namespace CrewSizer.Tui;

public class TuiApp
{
    private readonly ITheme _theme;
    private readonly ScreenRenderer _renderer;
    private readonly OutputBuffer _buffer = new();
    private readonly CommandHandler _handler;

    private string _inputBuffer = "";
    private int _cursorPos;
    private readonly List<string> _history = [];
    private int _historyIndex = -1;

    private HelpOverlay? _helpOverlay;
    private FormOverlay? _formOverlay;
    private ProgrammeOverlay? _programmeOverlay;

    public TuiApp(Configuration config, ITheme theme, string? configPath)
    {
        _theme = theme;
        _renderer = new ScreenRenderer(theme);
        _handler = new CommandHandler(config, _buffer, theme, configPath);

        _buffer.WriteLine("CrewSizer -- Marge d'engagement equipage");
        if (configPath != null)
        {
            _buffer.WriteLine($"Configuration : {Path.GetFileName(configPath)}");
            var p = config.Periode;
            if (!string.IsNullOrEmpty(p.Mois))
                _buffer.WriteLine($"Periode : {p.Mois} {p.Annee} ({p.NbJours} jours)");
        }
        else
        {
            _buffer.WriteLine("Nouvelle configuration.");
            _buffer.WriteLine("  set mois <nom> <annee> <nbJours>   Definir la periode");
            _buffer.WriteLine("  set cdb/opl/cc/pnc <N>             Definir l'effectif");
            _buffer.WriteLine("  add ligne <nom> <bl/j> <j/s> <s/m> <hdv>  Ajouter une ligne");
            _buffer.WriteLine("  calc                               Lancer le calcul");
            _buffer.WriteLine("  save <fichier.xml>                 Sauvegarder");
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
                _programmeOverlay = ProgrammeOverlay.FromConfig(_handler.Config);
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
            _programmeOverlay = ProgrammeOverlay.FromConfig(_handler.Config);
            return;
        }

        _handler.ExecuteCommand(command);
        _buffer.WriteLine();
    }

    private void Render()
    {
        _renderer.UpdateSize();
        _renderer.BeginFrame();

        var configName = GetConfigName();
        var period = GetPeriodString();
        var statusExtra = _handler.IsDirty ? "[modifie]" : "";

        _renderer.RenderFrame(configName, period, statusExtra);

        if (_programmeOverlay is { IsVisible: true })
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

        var showCursor = _programmeOverlay is { IsVisible: true, IsEditing: true }
                      || _formOverlay is { IsVisible: true, IsEditing: true }
                      || (_programmeOverlay is not { IsVisible: true }
                          && _formOverlay is not { IsVisible: true }
                          && _helpOverlay is not { IsVisible: true });
        _renderer.SetCursorVisible(showCursor);

        _renderer.FlushFrame();
    }

    private string GetConfigName()
    {
        if (_handler.ConfigPath == null)
            return "(non sauvegarde)";
        return Path.GetFileName(_handler.ConfigPath);
    }

    private string GetPeriodString()
    {
        var p = _handler.Config.Periode;
        if (string.IsNullOrEmpty(p.Mois))
            return "";
        return $"{p.Mois} {p.Annee} ({p.NbJours} j)";
    }
}
