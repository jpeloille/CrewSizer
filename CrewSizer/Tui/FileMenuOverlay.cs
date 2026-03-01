using CrewSizer.IO;

namespace CrewSizer.Tui;

public enum FileAction { New, Open, SaveAs, Delete, SetDirectory }
public enum FileTab { Parametres, Programme, CatalogueVols, Equipage }

public class FileMenuOverlay
{
    private FileTab _activeTab = FileTab.Parametres;

    private readonly Dictionary<FileTab, List<string>> _filesByTab = new()
    {
        [FileTab.Parametres] = [],
        [FileTab.Programme] = [],
        [FileTab.CatalogueVols] = [],
        [FileTab.Equipage] = [],
    };

    private readonly Dictionary<FileTab, int> _selectedByTab = new()
    {
        [FileTab.Parametres] = 0,
        [FileTab.Programme] = 0,
        [FileTab.CatalogueVols] = 0,
        [FileTab.Equipage] = 0,
    };

    private readonly Dictionary<FileTab, int> _scrollByTab = new()
    {
        [FileTab.Parametres] = 0,
        [FileTab.Programme] = 0,
        [FileTab.CatalogueVols] = 0,
        [FileTab.Equipage] = 0,
    };

    private readonly string? _paramPath;
    private readonly string? _progPath;
    private readonly string? _volsPath;
    private readonly string? _equipagePath;
    private readonly string _sourceDirectory;

    private bool _inputMode;
    private string _inputBuffer = "";
    private int _inputCursorPos;
    private FileAction _inputAction;

    public bool IsVisible { get; private set; } = true;
    public FileAction? ResultAction { get; private set; }
    public string? ResultPath { get; private set; }

    // Public state for renderer
    public FileTab ActiveTab => _activeTab;
    public IReadOnlyList<string> Files => _filesByTab[_activeTab];
    public int SelectedIndex => _selectedByTab[_activeTab];
    public int ScrollOffset => _scrollByTab[_activeTab];
    public bool InputMode => _inputMode;
    public string InputBuffer => _inputBuffer;
    public int InputCursorPos => _inputCursorPos;
    public FileAction InputAction => _inputAction;
    public bool IsEditing => _inputMode;
    public string SourceDirectory => _sourceDirectory;

    public string? CurrentPath => _activeTab switch
    {
        FileTab.Parametres => _paramPath,
        FileTab.Programme => _progPath,
        FileTab.CatalogueVols => _volsPath,
        FileTab.Equipage => _equipagePath,
        _ => null
    };

    public ConfigFileType ActiveTabType => _activeTab switch
    {
        FileTab.Parametres => ConfigFileType.Parametres,
        FileTab.Programme => ConfigFileType.Programme,
        FileTab.CatalogueVols => ConfigFileType.CatalogueVols,
        FileTab.Equipage => ConfigFileType.Equipage,
        _ => ConfigFileType.Unknown
    };

    private FileMenuOverlay(string? paramPath, string? progPath, string? volsPath,
        string? equipagePath, string sourceDirectory)
    {
        _paramPath = paramPath != null ? Path.GetFullPath(paramPath) : null;
        _progPath = progPath != null ? Path.GetFullPath(progPath) : null;
        _volsPath = volsPath != null ? Path.GetFullPath(volsPath) : null;
        _equipagePath = equipagePath != null ? Path.GetFullPath(equipagePath) : null;
        _sourceDirectory = sourceDirectory;
        RefreshFileList();
    }

    public static FileMenuOverlay FromCurrentState(string? paramPath, string? progPath,
        string? volsPath, string? equipagePath = null, string? sourceDirectory = null)
    {
        return new FileMenuOverlay(paramPath, progPath, volsPath, equipagePath,
            sourceDirectory ?? Environment.CurrentDirectory);
    }

    public void RefreshFileList()
    {
        foreach (var tab in _filesByTab.Keys)
            _filesByTab[tab].Clear();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dirs = new List<string>();

        void AddDir(string d)
        {
            if (seen.Add(Path.GetFullPath(d))) dirs.Add(d);
            var sub = Path.Combine(d, "Config");
            if (Directory.Exists(sub) && seen.Add(Path.GetFullPath(sub))) dirs.Add(sub);
        }

        AddDir(_sourceDirectory);
        if (!string.Equals(Path.GetFullPath(_sourceDirectory),
            Path.GetFullPath(Environment.CurrentDirectory), StringComparison.OrdinalIgnoreCase))
            AddDir(Environment.CurrentDirectory);

        foreach (var dir in dirs)
        {
            try
            {
                foreach (var f in Directory.GetFiles(dir, "*.xml"))
                {
                    var type = XmlConfigLoader.DetecterType(f);
                    switch (type)
                    {
                        case ConfigFileType.Parametres:
                            _filesByTab[FileTab.Parametres].Add(f);
                            break;
                        case ConfigFileType.Programme:
                            _filesByTab[FileTab.Programme].Add(f);
                            break;
                        case ConfigFileType.CatalogueVols:
                            _filesByTab[FileTab.CatalogueVols].Add(f);
                            break;
                        case ConfigFileType.Equipage:
                            _filesByTab[FileTab.Equipage].Add(f);
                            break;
                        case ConfigFileType.Legacy:
                            // Legacy files appear in all tabs
                            _filesByTab[FileTab.Parametres].Add(f);
                            _filesByTab[FileTab.Programme].Add(f);
                            _filesByTab[FileTab.CatalogueVols].Add(f);
                            break;
                    }
                }
            }
            catch { /* ignore permission errors */ }
        }

        foreach (var tab in _filesByTab.Keys)
        {
            _filesByTab[tab].Sort(StringComparer.OrdinalIgnoreCase);
            if (_selectedByTab[tab] >= _filesByTab[tab].Count && _filesByTab[tab].Count > 0)
                _selectedByTab[tab] = _filesByTab[tab].Count - 1;
        }

        PositionOnCurrentFile(FileTab.Parametres, _paramPath);
        PositionOnCurrentFile(FileTab.Programme, _progPath);
        PositionOnCurrentFile(FileTab.CatalogueVols, _volsPath);
        PositionOnCurrentFile(FileTab.Equipage, _equipagePath);
    }

    private void PositionOnCurrentFile(FileTab tab, string? currentPath)
    {
        if (currentPath == null) return;
        var idx = _filesByTab[tab].FindIndex(f =>
            string.Equals(Path.GetFullPath(f), currentPath, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) _selectedByTab[tab] = idx;
    }

    public bool HandleKey(ConsoleKeyInfo key)
    {
        if (_inputMode)
            return HandleInputKey(key);

        var files = _filesByTab[_activeTab];

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                IsVisible = false;
                return false;

            case ConsoleKey.Tab:
                _activeTab = _activeTab switch
                {
                    FileTab.Parametres => FileTab.Programme,
                    FileTab.Programme => FileTab.CatalogueVols,
                    FileTab.CatalogueVols => FileTab.Equipage,
                    FileTab.Equipage => FileTab.Parametres,
                    _ => FileTab.Parametres
                };
                break;

            case ConsoleKey.UpArrow:
                if (_selectedByTab[_activeTab] > 0) _selectedByTab[_activeTab]--;
                break;

            case ConsoleKey.DownArrow:
                if (_selectedByTab[_activeTab] < files.Count - 1) _selectedByTab[_activeTab]++;
                break;

            case ConsoleKey.Enter:
                if (files.Count > 0)
                {
                    ResultAction = FileAction.Open;
                    ResultPath = files[_selectedByTab[_activeTab]];
                    IsVisible = false;
                    return false;
                }
                break;

            case ConsoleKey.Delete:
                if (files.Count > 0)
                {
                    ResultAction = FileAction.Delete;
                    ResultPath = files[_selectedByTab[_activeTab]];
                    IsVisible = false;
                    return false;
                }
                break;

            default:
                switch (key.KeyChar)
                {
                    case 'n' or 'N':
                        _inputMode = true;
                        _inputAction = FileAction.New;
                        _inputBuffer = "";
                        _inputCursorPos = 0;
                        break;

                    case 's' or 'S':
                        _inputMode = true;
                        _inputAction = FileAction.SaveAs;
                        _inputBuffer = "";
                        _inputCursorPos = 0;
                        break;

                    case 'd' or 'D':
                        _inputMode = true;
                        _inputAction = FileAction.SetDirectory;
                        _inputBuffer = _sourceDirectory;
                        _inputCursorPos = _inputBuffer.Length;
                        break;
                }
                break;
        }

        return true;
    }

    private bool HandleInputKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _inputMode = false;
                break;

            case ConsoleKey.Enter:
                if (!string.IsNullOrWhiteSpace(_inputBuffer))
                {
                    if (_inputAction == FileAction.SetDirectory)
                    {
                        ResultAction = FileAction.SetDirectory;
                        ResultPath = _inputBuffer.Trim();
                        IsVisible = false;
                        return false;
                    }

                    var name = _inputBuffer.Trim();
                    if (!name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        name += ".xml";

                    ResultAction = _inputAction;
                    ResultPath = Path.Combine(_sourceDirectory, name);
                    IsVisible = false;
                    return false;
                }
                break;

            case ConsoleKey.Backspace:
                if (_inputCursorPos > 0)
                {
                    _inputBuffer = _inputBuffer.Remove(_inputCursorPos - 1, 1);
                    _inputCursorPos--;
                }
                break;

            case ConsoleKey.Delete:
                if (_inputCursorPos < _inputBuffer.Length)
                    _inputBuffer = _inputBuffer.Remove(_inputCursorPos, 1);
                break;

            case ConsoleKey.LeftArrow:
                if (_inputCursorPos > 0) _inputCursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (_inputCursorPos < _inputBuffer.Length) _inputCursorPos++;
                break;

            case ConsoleKey.Home:
                _inputCursorPos = 0;
                break;

            case ConsoleKey.End:
                _inputCursorPos = _inputBuffer.Length;
                break;

            default:
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    _inputBuffer = _inputBuffer.Insert(_inputCursorPos, key.KeyChar.ToString());
                    _inputCursorPos++;
                }
                break;
        }

        return true;
    }

    public void EnsureVisible(int contentHeight)
    {
        var sel = _selectedByTab[_activeTab];
        var scroll = _scrollByTab[_activeTab];

        if (sel < scroll)
            scroll = sel;
        else if (sel >= scroll + contentHeight)
            scroll = sel - contentHeight + 1;

        _scrollByTab[_activeTab] = scroll;
    }

    public bool IsLegacyFile(string path)
    {
        return XmlConfigLoader.DetecterType(path) == ConfigFileType.Legacy;
    }
}
