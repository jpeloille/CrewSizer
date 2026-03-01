using System.Text.RegularExpressions;
using CrewSizer.IO;
using CrewSizer.Models;

namespace CrewSizer.Tui;

public enum CrewView
{
    ListeMembres,
    DetailMembre,
    QualificationsMembre,
    QualifEdit,
    TableauStatuts,
    DefinitionsChecks,
    ImportMenu,
    Stats
}

public enum ImportTab { PNT, PNC, CheckStatus, CheckDesc }

public enum CrewFilter { Tous, PNT, PNC }

public class CrewOverlay
{
    private readonly Configuration _config;
    private readonly Action? _onModified;
    private CrewView _view = CrewView.ListeMembres;

    // ── ListeMembres state ──
    private int _listIndex;
    private int _listScroll;
    private CrewFilter _filter = CrewFilter.Tous;
    private bool _confirmDelete;

    // ── DetailMembre state ──
    private int _fieldIndex;
    private int _fieldScroll;
    private bool _fieldEditing;
    private string _fieldEditBuffer = "";
    private int _fieldEditCursorPos;
    private bool _isNewMembre;
    private MembreEquipage? _editMembre;

    // Champs du formulaire détail
    private static readonly string[] DetailFields =
    [
        "Code", "Nom", "Matricule", "Grade", "Contrat",
        "Actif", "Date entree", "Date fin", "Roles",
        "Bases", "Type avion", "Categorie"
    ];

    private static readonly string[] GradeOptions = ["CDB", "OPL", "CC", "PNC"];
    private static readonly string[] ContratOptions = ["PNT", "PNC"];
    private static readonly string[] ActifOptions = ["Oui", "Non"];

    // ── QualificationsMembre state ──
    private int _qualifIndex;
    private int _qualifScroll;

    // ── QualifEdit state ──
    private int _qualifFieldIndex;
    private bool _qualifFieldEditing;
    private string _qualifFieldEditBuffer = "";
    private int _qualifFieldEditCursorPos;
    private bool _isNewQualif;
    private StatutQualification? _editQualif;

    private static readonly string[] QualifFields = ["Code check", "Date expiration", "Statut"];
    private static readonly string[] StatutOptions = ["Valide", "ExpirationProche", "Avertissement", "Expire", "NonApplicable"];

    // ── TableauStatuts state ──
    private int _matrixRow;
    private int _matrixCol;
    private int _matrixScrollRow;
    private int _matrixScrollCol;

    // ── DefinitionsChecks state ──
    private int _checkDefIndex;
    private int _checkDefScroll;

    // ── ImportMenu state ──
    private ImportTab _importTab = ImportTab.PNT;

    private readonly Dictionary<ImportTab, List<string>> _filesByTab = new()
    {
        [ImportTab.PNT] = [],
        [ImportTab.PNC] = [],
        [ImportTab.CheckStatus] = [],
        [ImportTab.CheckDesc] = [],
    };

    private readonly Dictionary<ImportTab, int> _selectedByTab = new()
    {
        [ImportTab.PNT] = 0,
        [ImportTab.PNC] = 0,
        [ImportTab.CheckStatus] = 0,
        [ImportTab.CheckDesc] = 0,
    };

    private readonly Dictionary<ImportTab, int> _scrollByTab = new()
    {
        [ImportTab.PNT] = 0,
        [ImportTab.PNC] = 0,
        [ImportTab.CheckStatus] = 0,
        [ImportTab.CheckDesc] = 0,
    };

    // ── Stats state ──
    private int _statsScroll;

    // ── Public state for renderer ──
    public bool IsVisible { get; private set; } = true;
    public bool Modified { get; private set; }
    public CrewView View => _view;
    public Configuration Config => _config;

    public bool IsEditing => _fieldEditing || _qualifFieldEditing;

    // ListeMembres
    public int ListIndex => _listIndex;
    public int ListScroll => _listScroll;
    public CrewFilter Filter => _filter;
    public bool ConfirmDelete => _confirmDelete;

    // DetailMembre
    public int FieldIndex => _fieldIndex;
    public int FieldScroll => _fieldScroll;
    public bool FieldEditing => _fieldEditing;
    public string FieldEditBuffer => _fieldEditBuffer;
    public int FieldEditCursorPos => _fieldEditCursorPos;
    public bool IsNewMembre => _isNewMembre;
    public MembreEquipage? EditMembre => _editMembre;

    // QualificationsMembre
    public int QualifIndex => _qualifIndex;
    public int QualifScroll => _qualifScroll;

    // QualifEdit
    public int QualifFieldIndex => _qualifFieldIndex;
    public bool QualifFieldEditing => _qualifFieldEditing;
    public string QualifFieldEditBuffer => _qualifFieldEditBuffer;
    public int QualifFieldEditCursorPos => _qualifFieldEditCursorPos;
    public bool IsNewQualif => _isNewQualif;
    public StatutQualification? EditQualif => _editQualif;

    // TableauStatuts
    public int MatrixRow => _matrixRow;
    public int MatrixCol => _matrixCol;
    public int MatrixScrollRow => _matrixScrollRow;
    public int MatrixScrollCol => _matrixScrollCol;

    // DefinitionsChecks
    public int CheckDefIndex => _checkDefIndex;
    public int CheckDefScroll => _checkDefScroll;

    // ImportMenu
    public ImportTab ActiveImportTab => _importTab;
    public IReadOnlyList<string> ImportFiles => _filesByTab[_importTab];
    public int ImportSelectedIndex => _selectedByTab[_importTab];
    public int ImportScrollOffset => _scrollByTab[_importTab];

    // Stats
    public int StatsScroll => _statsScroll;

    // Import result
    public ImportTab? ImportResultTab { get; private set; }
    public string? ImportResultPath { get; private set; }

    // ── Computed ──

    public List<MembreEquipage> FilteredMembres
    {
        get
        {
            var membres = _config.Equipage?.Membres ?? [];
            return _filter switch
            {
                CrewFilter.PNT => membres.Where(m => m.Contrat == TypeContrat.PNT).ToList(),
                CrewFilter.PNC => membres.Where(m => m.Contrat == TypeContrat.PNC).ToList(),
                _ => membres.ToList()
            };
        }
    }

    public MembreEquipage? CurrentMembre
    {
        get
        {
            var list = FilteredMembres;
            return _listIndex >= 0 && _listIndex < list.Count ? list[_listIndex] : null;
        }
    }

    public List<string> AllCheckCodes
    {
        get
        {
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var eq = _config.Equipage;
            if (eq == null) return [];
            foreach (var c in eq.Checks)
                codes.Add(c.Code);
            foreach (var m in eq.Membres)
            foreach (var q in m.Qualifications)
                codes.Add(q.CodeCheck);
            return codes.OrderBy(c => c).ToList();
        }
    }

    // ── Construction ──

    private string _sourceDirectory = Environment.CurrentDirectory;

    private CrewOverlay(Configuration config, Action? onModified)
    {
        _config = config;
        _onModified = onModified;
    }

    public static CrewOverlay FromConfig(Configuration config, Action? onModified = null,
        string? sourceDirectory = null)
    {
        var overlay = new CrewOverlay(config, onModified);
        if (sourceDirectory != null)
            overlay._sourceDirectory = sourceDirectory;
        overlay.RefreshImportFileList();
        return overlay;
    }

    // ── HandleKey dispatcher ──

    public bool HandleKey(ConsoleKeyInfo key) => _view switch
    {
        CrewView.ListeMembres => HandleKeyListeMembres(key),
        CrewView.DetailMembre => HandleKeyDetailMembre(key),
        CrewView.QualificationsMembre => HandleKeyQualifications(key),
        CrewView.QualifEdit => HandleKeyQualifEdit(key),
        CrewView.TableauStatuts => HandleKeyTableauStatuts(key),
        CrewView.DefinitionsChecks => HandleKeyDefinitionsChecks(key),
        CrewView.ImportMenu => HandleKeyImportMenu(key),
        CrewView.Stats => HandleKeyStats(key),
        _ => true
    };

    // ══════════════════════════════════════════
    // ── ListeMembres ──
    // ══════════════════════════════════════════

    private bool HandleKeyListeMembres(ConsoleKeyInfo key)
    {
        if (_confirmDelete)
        {
            if (key.KeyChar is 'o' or 'O')
            {
                var membre = CurrentMembre;
                if (membre != null)
                {
                    _config.Equipage?.Membres.Remove(membre);
                    SetModified();
                    if (_listIndex >= FilteredMembres.Count && _listIndex > 0)
                        _listIndex--;
                }
                _confirmDelete = false;
            }
            else
            {
                _confirmDelete = false;
            }
            return true;
        }

        var list = FilteredMembres;

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                IsVisible = false;
                return false;

            case ConsoleKey.UpArrow:
                if (_listIndex > 0) _listIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_listIndex < list.Count - 1) _listIndex++;
                break;

            case ConsoleKey.PageUp:
                _listIndex = Math.Max(0, _listIndex - 10);
                break;

            case ConsoleKey.PageDown:
                _listIndex = Math.Min(list.Count - 1, _listIndex + 10);
                break;

            case ConsoleKey.Home:
                _listIndex = 0;
                break;

            case ConsoleKey.End:
                _listIndex = Math.Max(0, list.Count - 1);
                break;

            case ConsoleKey.Enter:
                if (list.Count > 0)
                {
                    _editMembre = CurrentMembre;
                    _isNewMembre = false;
                    _fieldIndex = 0;
                    _fieldScroll = 0;
                    _fieldEditing = false;
                    _view = CrewView.DetailMembre;
                }
                break;

            case ConsoleKey.Insert:
                EnsureEquipage();
                _editMembre = new MembreEquipage();
                _isNewMembre = true;
                _fieldIndex = 0;
                _fieldScroll = 0;
                _fieldEditing = false;
                _view = CrewView.DetailMembre;
                break;

            case ConsoleKey.Delete:
                if (list.Count > 0 && CurrentMembre != null)
                    _confirmDelete = true;
                break;

            default:
                switch (key.KeyChar)
                {
                    case 'p' or 'P':
                        _filter = CrewFilter.PNT;
                        _listIndex = 0;
                        _listScroll = 0;
                        break;
                    case 'n' or 'N':
                        _filter = CrewFilter.PNC;
                        _listIndex = 0;
                        _listScroll = 0;
                        break;
                    case 'a' or 'A':
                        _filter = CrewFilter.Tous;
                        _listIndex = 0;
                        _listScroll = 0;
                        break;
                    case 'q' or 'Q':
                        if (list.Count > 0 && CurrentMembre != null)
                        {
                            _editMembre = CurrentMembre;
                            _qualifIndex = 0;
                            _qualifScroll = 0;
                            _view = CrewView.QualificationsMembre;
                        }
                        break;
                    case 'i' or 'I':
                        RefreshImportFileList();
                        _view = CrewView.ImportMenu;
                        break;
                    case 't' or 'T':
                        _matrixRow = 0;
                        _matrixCol = 0;
                        _matrixScrollRow = 0;
                        _matrixScrollCol = 0;
                        _view = CrewView.TableauStatuts;
                        break;
                    case 'c' or 'C':
                        _checkDefIndex = 0;
                        _checkDefScroll = 0;
                        _view = CrewView.DefinitionsChecks;
                        break;
                    case 's' or 'S':
                        _statsScroll = 0;
                        _view = CrewView.Stats;
                        break;
                }
                break;
        }

        return true;
    }

    // ══════════════════════════════════════════
    // ── DetailMembre ──
    // ══════════════════════════════════════════

    private bool HandleKeyDetailMembre(ConsoleKeyInfo key)
    {
        if (_fieldEditing)
            return HandleFieldEditKey(key);

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                if (_isNewMembre && _editMembre != null &&
                    !string.IsNullOrWhiteSpace(_editMembre.Code))
                {
                    EnsureEquipage();
                    _config.Equipage!.Membres.Add(_editMembre);
                    SetModified();
                }
                _editMembre = null;
                _isNewMembre = false;
                _view = CrewView.ListeMembres;
                break;

            case ConsoleKey.UpArrow:
                if (_fieldIndex > 0) _fieldIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_fieldIndex < DetailFields.Length - 1) _fieldIndex++;
                break;

            case ConsoleKey.Enter:
                StartFieldEdit();
                break;

            case ConsoleKey.Delete:
                if (_editMembre != null && !_isNewMembre)
                {
                    _config.Equipage?.Membres.Remove(_editMembre);
                    SetModified();
                    _editMembre = null;
                    _view = CrewView.ListeMembres;
                    if (_listIndex >= FilteredMembres.Count && _listIndex > 0)
                        _listIndex--;
                }
                break;

            default:
                if (key.KeyChar is 'q' or 'Q')
                {
                    if (_editMembre != null)
                    {
                        if (_isNewMembre && !string.IsNullOrWhiteSpace(_editMembre.Code))
                        {
                            EnsureEquipage();
                            _config.Equipage!.Membres.Add(_editMembre);
                            SetModified();
                            _isNewMembre = false;
                        }
                        _qualifIndex = 0;
                        _qualifScroll = 0;
                        _view = CrewView.QualificationsMembre;
                    }
                }
                else if (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.RightArrow)
                {
                    // Cycle sélecteurs
                    CycleFieldSelector(key.Key == ConsoleKey.RightArrow ? 1 : -1);
                }
                break;
        }

        return true;
    }

    private void StartFieldEdit()
    {
        if (_editMembre == null) return;

        var field = DetailFields[_fieldIndex];

        // Sélecteurs : cycle au lieu d'édition texte
        if (field is "Grade" or "Contrat" or "Actif")
        {
            CycleFieldSelector(1);
            return;
        }

        _fieldEditing = true;
        _fieldEditBuffer = GetFieldValue(_editMembre, _fieldIndex);
        _fieldEditCursorPos = _fieldEditBuffer.Length;
    }

    private void CycleFieldSelector(int direction)
    {
        if (_editMembre == null) return;
        var field = DetailFields[_fieldIndex];

        switch (field)
        {
            case "Grade":
                var gi = Array.IndexOf(GradeOptions, _editMembre.Grade.ToString());
                gi = (gi + direction + GradeOptions.Length) % GradeOptions.Length;
                _editMembre.Grade = Enum.Parse<Grade>(GradeOptions[gi]);
                SetModified();
                break;
            case "Contrat":
                var ci = Array.IndexOf(ContratOptions, _editMembre.Contrat.ToString());
                ci = (ci + direction + ContratOptions.Length) % ContratOptions.Length;
                _editMembre.Contrat = Enum.Parse<TypeContrat>(ContratOptions[ci]);
                SetModified();
                break;
            case "Actif":
                _editMembre.Actif = !_editMembre.Actif;
                SetModified();
                break;
        }
    }

    private bool HandleFieldEditKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _fieldEditing = false;
                break;

            case ConsoleKey.Enter:
            case ConsoleKey.Tab:
                ApplyFieldEdit();
                _fieldEditing = false;
                if (key.Key == ConsoleKey.Tab && _fieldIndex < DetailFields.Length - 1)
                    _fieldIndex++;
                break;

            case ConsoleKey.Backspace:
                if (_fieldEditCursorPos > 0)
                {
                    _fieldEditBuffer = _fieldEditBuffer.Remove(_fieldEditCursorPos - 1, 1);
                    _fieldEditCursorPos--;
                }
                break;

            case ConsoleKey.Delete:
                if (_fieldEditCursorPos < _fieldEditBuffer.Length)
                    _fieldEditBuffer = _fieldEditBuffer.Remove(_fieldEditCursorPos, 1);
                break;

            case ConsoleKey.LeftArrow:
                if (_fieldEditCursorPos > 0) _fieldEditCursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (_fieldEditCursorPos < _fieldEditBuffer.Length) _fieldEditCursorPos++;
                break;

            case ConsoleKey.Home:
                _fieldEditCursorPos = 0;
                break;

            case ConsoleKey.End:
                _fieldEditCursorPos = _fieldEditBuffer.Length;
                break;

            default:
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    _fieldEditBuffer = _fieldEditBuffer.Insert(_fieldEditCursorPos, key.KeyChar.ToString());
                    _fieldEditCursorPos++;
                }
                break;
        }

        return true;
    }

    private void ApplyFieldEdit()
    {
        if (_editMembre == null) return;
        SetFieldValue(_editMembre, _fieldIndex, _fieldEditBuffer.Trim());
        SetModified();
    }

    public static string GetFieldValue(MembreEquipage m, int fieldIndex) => fieldIndex switch
    {
        0 => m.Code,
        1 => m.Nom,
        2 => m.Matricule,
        3 => m.Grade.ToString(),
        4 => m.Contrat.ToString(),
        5 => m.Actif ? "Oui" : "Non",
        6 => m.DateEntree?.ToString("dd/MM/yyyy") ?? "",
        7 => m.DateFin?.ToString("dd/MM/yyyy") ?? "",
        8 => string.Join("+", m.Roles),
        9 => string.Join(" ", m.Bases),
        10 => m.TypeAvion,
        11 => m.Categorie,
        _ => ""
    };

    public static string GetFieldLabel(int fieldIndex) =>
        fieldIndex >= 0 && fieldIndex < DetailFields.Length ? DetailFields[fieldIndex] : "";

    public static bool IsFieldSelector(int fieldIndex) =>
        fieldIndex >= 0 && fieldIndex < DetailFields.Length &&
        DetailFields[fieldIndex] is "Grade" or "Contrat" or "Actif";

    private static void SetFieldValue(MembreEquipage m, int fieldIndex, string value)
    {
        switch (fieldIndex)
        {
            case 0: m.Code = value; break;
            case 1: m.Nom = value; break;
            case 2: m.Matricule = value; break;
            case 3:
                if (Enum.TryParse<Grade>(value, true, out var g)) m.Grade = g;
                break;
            case 4:
                if (Enum.TryParse<TypeContrat>(value, true, out var tc)) m.Contrat = tc;
                break;
            case 5: m.Actif = value.Equals("Oui", StringComparison.OrdinalIgnoreCase) || value == "1"; break;
            case 6:
                m.DateEntree = DateTime.TryParseExact(value, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var d1) ? d1 : m.DateEntree;
                break;
            case 7:
                m.DateFin = string.IsNullOrEmpty(value) ? null :
                    DateTime.TryParseExact(value, "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var d2) ? d2 : m.DateFin;
                break;
            case 8:
                m.Roles = value.Split('+', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim()).Where(r => r.Length > 0).ToList();
                break;
            case 9:
                m.Bases = value.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => b.Trim()).Where(b => b.Length > 0).Distinct().ToList();
                break;
            case 10: m.TypeAvion = value; break;
            case 11: m.Categorie = value; break;
        }
    }

    // ══════════════════════════════════════════
    // ── QualificationsMembre ──
    // ══════════════════════════════════════════

    private bool HandleKeyQualifications(ConsoleKeyInfo key)
    {
        var qualifs = _editMembre?.Qualifications ?? [];

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = CrewView.DetailMembre;
                break;

            case ConsoleKey.UpArrow:
                if (_qualifIndex > 0) _qualifIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_qualifIndex < qualifs.Count - 1) _qualifIndex++;
                break;

            case ConsoleKey.PageUp:
                _qualifIndex = Math.Max(0, _qualifIndex - 10);
                break;

            case ConsoleKey.PageDown:
                _qualifIndex = Math.Min(qualifs.Count - 1, _qualifIndex + 10);
                break;

            case ConsoleKey.Enter:
                if (qualifs.Count > 0 && _qualifIndex < qualifs.Count)
                {
                    _editQualif = qualifs[_qualifIndex];
                    _isNewQualif = false;
                    _qualifFieldIndex = 0;
                    _qualifFieldEditing = false;
                    _view = CrewView.QualifEdit;
                }
                break;

            case ConsoleKey.Insert:
                _editQualif = new StatutQualification();
                _isNewQualif = true;
                _qualifFieldIndex = 0;
                _qualifFieldEditing = false;
                _view = CrewView.QualifEdit;
                break;

            case ConsoleKey.Delete:
                if (qualifs.Count > 0 && _qualifIndex < qualifs.Count)
                {
                    qualifs.RemoveAt(_qualifIndex);
                    SetModified();
                    if (_qualifIndex >= qualifs.Count && _qualifIndex > 0)
                        _qualifIndex--;
                }
                break;
        }

        return true;
    }

    // ══════════════════════════════════════════
    // ── QualifEdit ──
    // ══════════════════════════════════════════

    private bool HandleKeyQualifEdit(ConsoleKeyInfo key)
    {
        if (_qualifFieldEditing)
            return HandleQualifFieldEditKey(key);

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                if (_isNewQualif && _editQualif != null &&
                    !string.IsNullOrWhiteSpace(_editQualif.CodeCheck))
                {
                    _editMembre?.Qualifications.Add(_editQualif);
                    SetModified();
                }
                _editQualif = null;
                _isNewQualif = false;
                _view = CrewView.QualificationsMembre;
                break;

            case ConsoleKey.UpArrow:
                if (_qualifFieldIndex > 0) _qualifFieldIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_qualifFieldIndex < QualifFields.Length - 1) _qualifFieldIndex++;
                break;

            case ConsoleKey.Enter:
                StartQualifFieldEdit();
                break;

            case ConsoleKey.LeftArrow:
            case ConsoleKey.RightArrow:
                if (_qualifFieldIndex == 2) // Statut = sélecteur
                    CycleQualifStatut(key.Key == ConsoleKey.RightArrow ? 1 : -1);
                break;
        }

        return true;
    }

    private void StartQualifFieldEdit()
    {
        if (_editQualif == null) return;

        // Statut = sélecteur
        if (_qualifFieldIndex == 2)
        {
            CycleQualifStatut(1);
            return;
        }

        _qualifFieldEditing = true;
        _qualifFieldEditBuffer = GetQualifFieldValue(_editQualif, _qualifFieldIndex);
        _qualifFieldEditCursorPos = _qualifFieldEditBuffer.Length;
    }

    private void CycleQualifStatut(int direction)
    {
        if (_editQualif == null) return;
        var si = Array.IndexOf(StatutOptions, _editQualif.Statut.ToString());
        si = (si + direction + StatutOptions.Length) % StatutOptions.Length;
        _editQualif.Statut = Enum.Parse<StatutCheck>(StatutOptions[si]);
        SetModified();
    }

    private bool HandleQualifFieldEditKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _qualifFieldEditing = false;
                break;

            case ConsoleKey.Enter:
            case ConsoleKey.Tab:
                ApplyQualifFieldEdit();
                _qualifFieldEditing = false;
                if (key.Key == ConsoleKey.Tab && _qualifFieldIndex < QualifFields.Length - 1)
                    _qualifFieldIndex++;
                break;

            case ConsoleKey.Backspace:
                if (_qualifFieldEditCursorPos > 0)
                {
                    _qualifFieldEditBuffer = _qualifFieldEditBuffer.Remove(_qualifFieldEditCursorPos - 1, 1);
                    _qualifFieldEditCursorPos--;
                }
                break;

            case ConsoleKey.Delete:
                if (_qualifFieldEditCursorPos < _qualifFieldEditBuffer.Length)
                    _qualifFieldEditBuffer = _qualifFieldEditBuffer.Remove(_qualifFieldEditCursorPos, 1);
                break;

            case ConsoleKey.LeftArrow:
                if (_qualifFieldEditCursorPos > 0) _qualifFieldEditCursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (_qualifFieldEditCursorPos < _qualifFieldEditBuffer.Length) _qualifFieldEditCursorPos++;
                break;

            case ConsoleKey.Home:
                _qualifFieldEditCursorPos = 0;
                break;

            case ConsoleKey.End:
                _qualifFieldEditCursorPos = _qualifFieldEditBuffer.Length;
                break;

            default:
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    _qualifFieldEditBuffer = _qualifFieldEditBuffer.Insert(_qualifFieldEditCursorPos, key.KeyChar.ToString());
                    _qualifFieldEditCursorPos++;
                }
                break;
        }

        return true;
    }

    private void ApplyQualifFieldEdit()
    {
        if (_editQualif == null) return;
        var value = _qualifFieldEditBuffer.Trim();

        switch (_qualifFieldIndex)
        {
            case 0: // Code check
                _editQualif.CodeCheck = value;
                break;
            case 1: // Date expiration
                if (string.IsNullOrEmpty(value))
                    _editQualif.DateExpiration = null;
                else if (DateTime.TryParseExact(value, "dd/MM/yyyy",
                             System.Globalization.CultureInfo.InvariantCulture,
                             System.Globalization.DateTimeStyles.None, out var d))
                    _editQualif.DateExpiration = d;
                break;
        }
        SetModified();
    }

    public static string GetQualifFieldValue(StatutQualification q, int fieldIndex) => fieldIndex switch
    {
        0 => q.CodeCheck,
        1 => q.DateExpiration?.ToString("dd/MM/yyyy") ?? "",
        2 => q.Statut.ToString(),
        _ => ""
    };

    public static string GetQualifFieldLabel(int fieldIndex) =>
        fieldIndex >= 0 && fieldIndex < QualifFields.Length ? QualifFields[fieldIndex] : "";

    public static bool IsQualifFieldSelector(int fieldIndex) => fieldIndex == 2;

    // ══════════════════════════════════════════
    // ── TableauStatuts ──
    // ══════════════════════════════════════════

    private bool HandleKeyTableauStatuts(ConsoleKeyInfo key)
    {
        var membres = _config.Equipage?.Membres ?? [];
        var codes = AllCheckCodes;

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = CrewView.ListeMembres;
                break;

            case ConsoleKey.UpArrow:
                if (_matrixRow > 0) _matrixRow--;
                break;

            case ConsoleKey.DownArrow:
                if (_matrixRow < membres.Count - 1) _matrixRow++;
                break;

            case ConsoleKey.LeftArrow:
                if (_matrixCol > 0) _matrixCol--;
                break;

            case ConsoleKey.RightArrow:
                if (_matrixCol < codes.Count - 1) _matrixCol++;
                break;

            case ConsoleKey.PageUp:
                _matrixRow = Math.Max(0, _matrixRow - 10);
                break;

            case ConsoleKey.PageDown:
                _matrixRow = Math.Min(membres.Count - 1, _matrixRow + 10);
                break;

            case ConsoleKey.Home:
                _matrixCol = 0;
                break;

            case ConsoleKey.End:
                _matrixCol = Math.Max(0, codes.Count - 1);
                break;
        }

        return true;
    }

    // ══════════════════════════════════════════
    // ── DefinitionsChecks ──
    // ══════════════════════════════════════════

    private bool HandleKeyDefinitionsChecks(ConsoleKeyInfo key)
    {
        var checks = _config.Equipage?.Checks ?? [];

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = CrewView.ListeMembres;
                break;

            case ConsoleKey.UpArrow:
                if (_checkDefIndex > 0) _checkDefIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_checkDefIndex < checks.Count - 1) _checkDefIndex++;
                break;

            case ConsoleKey.PageUp:
                _checkDefIndex = Math.Max(0, _checkDefIndex - 10);
                break;

            case ConsoleKey.PageDown:
                _checkDefIndex = Math.Min(checks.Count - 1, _checkDefIndex + 10);
                break;
        }

        return true;
    }

    // ══════════════════════════════════════════
    // ── ImportMenu ──
    // ══════════════════════════════════════════

    private bool HandleKeyImportMenu(ConsoleKeyInfo key)
    {
        var files = _filesByTab[_importTab];

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = CrewView.ListeMembres;
                break;

            case ConsoleKey.Tab:
                _importTab = _importTab switch
                {
                    ImportTab.PNT => ImportTab.PNC,
                    ImportTab.PNC => ImportTab.CheckStatus,
                    ImportTab.CheckStatus => ImportTab.CheckDesc,
                    ImportTab.CheckDesc => ImportTab.PNT,
                    _ => ImportTab.PNT
                };
                break;

            case ConsoleKey.UpArrow:
                if (_selectedByTab[_importTab] > 0) _selectedByTab[_importTab]--;
                break;

            case ConsoleKey.DownArrow:
                if (_selectedByTab[_importTab] < files.Count - 1) _selectedByTab[_importTab]++;
                break;

            case ConsoleKey.Enter:
                if (files.Count > 0)
                {
                    ImportResultTab = _importTab;
                    ImportResultPath = files[_selectedByTab[_importTab]];
                    // Stay in overlay, don't close - just signal result
                    // TuiApp will handle the import and we'll refresh
                    IsVisible = false;
                    return false;
                }
                break;
        }

        return true;
    }

    // ══════════════════════════════════════════
    // ── Stats ──
    // ══════════════════════════════════════════

    private bool HandleKeyStats(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = CrewView.ListeMembres;
                break;

            case ConsoleKey.UpArrow:
            case ConsoleKey.PageUp:
                if (_statsScroll > 0) _statsScroll--;
                break;

            case ConsoleKey.DownArrow:
            case ConsoleKey.PageDown:
                _statsScroll++;
                break;
        }

        return true;
    }

    // ══════════════════════════════════════════
    // ── Import file scanning ──
    // ══════════════════════════════════════════

    private void RefreshImportFileList()
    {
        foreach (var tab in _filesByTab.Keys)
            _filesByTab[tab].Clear();

        var dirs = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

        seen.Clear(); // réutiliser pour la déduplication des fichiers

        foreach (var dir in dirs)
        {
            try
            {
                foreach (var f in Directory.GetFiles(dir, "*.xml"))
                {
                    var fullPath = Path.GetFullPath(f);
                    if (!seen.Add(fullPath)) continue; // skip duplicates

                    var name = Path.GetFileName(f);
                    if (Regex.IsMatch(name, @"Pnt.*Crew|CrewList.*Pilot", RegexOptions.IgnoreCase))
                        _filesByTab[ImportTab.PNT].Add(f);
                    else if (Regex.IsMatch(name, @"Pnc.*Crew|CrewList.*Cabin", RegexOptions.IgnoreCase))
                        _filesByTab[ImportTab.PNC].Add(f);
                    else if (Regex.IsMatch(name, @"CheckStatus|CrewCheck", RegexOptions.IgnoreCase))
                        _filesByTab[ImportTab.CheckStatus].Add(f);
                    else if (Regex.IsMatch(name, @"Check.*Desc", RegexOptions.IgnoreCase))
                        _filesByTab[ImportTab.CheckDesc].Add(f);
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
    }

    // ══════════════════════════════════════════
    // ── Scroll helpers (called by renderer) ──
    // ══════════════════════════════════════════

    public void EnsureListVisible(int contentHeight)
    {
        if (_listIndex < _listScroll)
            _listScroll = _listIndex;
        else if (_listIndex >= _listScroll + contentHeight)
            _listScroll = _listIndex - contentHeight + 1;
    }

    public void EnsureQualifVisible(int contentHeight)
    {
        if (_qualifIndex < _qualifScroll)
            _qualifScroll = _qualifIndex;
        else if (_qualifIndex >= _qualifScroll + contentHeight)
            _qualifScroll = _qualifIndex - contentHeight + 1;
    }

    public void EnsureCheckDefVisible(int contentHeight)
    {
        if (_checkDefIndex < _checkDefScroll)
            _checkDefScroll = _checkDefIndex;
        else if (_checkDefIndex >= _checkDefScroll + contentHeight)
            _checkDefScroll = _checkDefIndex - contentHeight + 1;
    }

    public void EnsureImportVisible(int contentHeight)
    {
        var sel = _selectedByTab[_importTab];
        var scroll = _scrollByTab[_importTab];

        if (sel < scroll)
            scroll = sel;
        else if (sel >= scroll + contentHeight)
            scroll = sel - contentHeight + 1;

        _scrollByTab[_importTab] = scroll;
    }

    public void EnsureMatrixVisible(int visibleRows, int visibleCols)
    {
        if (_matrixRow < _matrixScrollRow)
            _matrixScrollRow = _matrixRow;
        else if (_matrixRow >= _matrixScrollRow + visibleRows)
            _matrixScrollRow = _matrixRow - visibleRows + 1;

        if (_matrixCol < _matrixScrollCol)
            _matrixScrollCol = _matrixCol;
        else if (_matrixCol >= _matrixScrollCol + visibleCols)
            _matrixScrollCol = _matrixCol - visibleCols + 1;
    }

    public void EnsureFieldVisible(int contentHeight)
    {
        if (_fieldIndex < _fieldScroll)
            _fieldScroll = _fieldIndex;
        else if (_fieldIndex >= _fieldScroll + contentHeight)
            _fieldScroll = _fieldIndex - contentHeight + 1;
    }

    // ══════════════════════════════════════════
    // ── Stats computation (for renderer) ──
    // ══════════════════════════════════════════

    public (int valides, int proches, int avertissements, int expires) ComputeQualifStats()
    {
        int v = 0, p = 0, a = 0, e = 0;
        foreach (var m in _config.Equipage?.Membres ?? [])
        {
            foreach (var q in m.Qualifications)
            {
                switch (q.Statut)
                {
                    case StatutCheck.Valide: v++; break;
                    case StatutCheck.ExpirationProche: p++; break;
                    case StatutCheck.Avertissement: a++; break;
                    case StatutCheck.Expire: e++; break;
                }
            }
        }
        return (v, p, a, e);
    }

    public StatutCheck GetMembreWorstStatut(MembreEquipage membre)
    {
        var worst = StatutCheck.NonApplicable;
        foreach (var q in membre.Qualifications)
        {
            if (q.Statut == StatutCheck.Expire) return StatutCheck.Expire;
            if (q.Statut == StatutCheck.Avertissement && worst != StatutCheck.Expire) worst = StatutCheck.Avertissement;
            if (q.Statut == StatutCheck.ExpirationProche && worst is StatutCheck.NonApplicable or StatutCheck.Valide) worst = StatutCheck.ExpirationProche;
            if (q.Statut == StatutCheck.Valide && worst == StatutCheck.NonApplicable) worst = StatutCheck.Valide;
        }
        return worst;
    }

    // ── Helpers ──

    private void SetModified()
    {
        Modified = true;
        _onModified?.Invoke();
    }

    private void EnsureEquipage()
    {
        _config.Equipage ??= new DonneesEquipage { DateExtraction = DateTime.Today };
    }
}
