using System.Globalization;
using CrewSizer.Models;

namespace CrewSizer.Tui;

public enum ProgrammeView { SemainesTypes, Blocs, BlocEdit, Calendrier, VolsList, VolEdit, Catalogue }

public class ProgrammeOverlay
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private readonly Configuration _config;
    private ProgrammeView _view = ProgrammeView.SemainesTypes;

    // SemainesTypes view
    private int _stIndex;
    private bool _stEditing;       // editing reference inline
    private string _stEditBuffer = "";
    private int _stEditCursorPos;

    // Blocs view
    private int _blocIndex;
    private int _blocScroll;

    // BlocEdit view
    private int _fieldIndex;
    private bool _fieldEditing;
    private string _fieldEditBuffer = "";
    private int _fieldEditCursorPos;
    private string _fieldEditOriginal = "";

    // Calendrier view
    private int _calIndex;
    private int _calScroll;

    // VolsList view
    private int _volIndex;
    private int _volScroll;

    // VolEdit view
    private int _volFieldIndex;
    private bool _volFieldEditing;
    private string _volFieldEditBuffer = "";
    private int _volFieldEditCursorPos;
    private string _volFieldEditOriginal = "";

    // Catalogue view
    private int _catIndex;
    private int _catScroll;
    private bool _catPickMode;

    // Catalogue edit popup
    private bool _catEditPopup;
    private int _catEditFieldIndex;
    private bool _catEditFieldEditing;
    private string _catEditFieldBuffer = "";
    private int _catEditFieldCursorPos;
    private string _catEditFieldOriginal = "";
    private Vol? _catEditVol;

    public bool IsVisible { get; private set; } = true;
    public bool Modified { get; private set; }

    // Public state for renderer
    public ProgrammeView View => _view;
    public Configuration Config => _config;
    public int StIndex => _stIndex;
    public bool StEditing => _stEditing;
    public string StEditBuffer => _stEditBuffer;
    public int StEditCursorPos => _stEditCursorPos;
    public int BlocIndex => _blocIndex;
    public int BlocScroll => _blocScroll;
    public int FieldIndex => _fieldIndex;
    public bool FieldEditing => _fieldEditing;
    public string FieldEditBuffer => _fieldEditBuffer;
    public int FieldEditCursorPos => _fieldEditCursorPos;
    public int CalIndex => _calIndex;
    public int CalScroll => _calScroll;
    public int VolIndex => _volIndex;
    public int VolScroll => _volScroll;
    public int VolFieldIndex => _volFieldIndex;
    public bool VolFieldEditing => _volFieldEditing;
    public string VolFieldEditBuffer => _volFieldEditBuffer;
    public int VolFieldEditCursorPos => _volFieldEditCursorPos;
    public int CatIndex => _catIndex;
    public int CatScroll => _catScroll;
    public bool CatPickMode => _catPickMode;
    public bool CatEditPopup => _catEditPopup;
    public int CatEditFieldIndex => _catEditFieldIndex;
    public bool CatEditFieldEditing => _catEditFieldEditing;
    public string CatEditFieldBuffer => _catEditFieldBuffer;
    public int CatEditFieldCursorPos => _catEditFieldCursorPos;
    public Vol? CatEditVol => _catEditVol;

    public bool IsEditing => _stEditing || _fieldEditing || _volFieldEditing || _catEditFieldEditing;

    // Current semaine type (for Blocs/BlocEdit views)
    public SemaineType? CurrentST =>
        _stIndex >= 0 && _stIndex < _config.SemainesTypes.Count
            ? _config.SemainesTypes[_stIndex]
            : null;

    // Current bloc (for BlocEdit view)
    public BlocVol? CurrentBloc
    {
        get
        {
            var st = CurrentST;
            if (st == null || _blocIndex < 0 || _blocIndex >= st.Blocs.Count) return null;
            return st.Blocs[_blocIndex];
        }
    }

    // Current vol (for VolEdit view)
    public Vol? CurrentVol
    {
        get
        {
            var bloc = CurrentBloc;
            if (bloc == null || _volIndex < 0 || _volIndex >= bloc.Vols.Count) return null;
            return bloc.Vols[_volIndex];
        }
    }

    private ProgrammeOverlay(Configuration config)
    {
        _config = config;
    }

    public static ProgrammeOverlay FromConfig(Configuration config)
    {
        return new ProgrammeOverlay(config);
    }

    // ── Key dispatch ──

    public bool HandleKey(ConsoleKeyInfo key)
    {
        return _view switch
        {
            ProgrammeView.SemainesTypes => HandleKeyST(key),
            ProgrammeView.Blocs => HandleKeyBlocs(key),
            ProgrammeView.BlocEdit => HandleKeyBlocEdit(key),
            ProgrammeView.Calendrier => HandleKeyCalendrier(key),
            ProgrammeView.VolsList => HandleKeyVolsList(key),
            ProgrammeView.VolEdit => HandleKeyVolEdit(key),
            ProgrammeView.Catalogue => HandleKeyCatalogue(key),
            _ => true
        };
    }

    // ══════════════════════════════════════════════
    //  Vue 1 : SEMAINES TYPES
    // ══════════════════════════════════════════════

    private bool HandleKeyST(ConsoleKeyInfo key)
    {
        if (_stEditing)
            return HandleSTEditKey(key);

        var stList = _config.SemainesTypes;

        switch (key.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.F3:
                IsVisible = false;
                return false;

            case ConsoleKey.UpArrow:
                if (_stIndex > 0) _stIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_stIndex < stList.Count - 1) _stIndex++;
                break;

            case ConsoleKey.LeftArrow:
                if (stList.Count > 0)
                    CycleSaison(stList[_stIndex], -1);
                break;

            case ConsoleKey.RightArrow:
                if (stList.Count > 0)
                    CycleSaison(stList[_stIndex], +1);
                break;

            case ConsoleKey.Enter:
                if (stList.Count > 0)
                {
                    _view = ProgrammeView.Blocs;
                    _blocIndex = 0;
                    _blocScroll = 0;
                }
                break;

            case ConsoleKey.Insert:
                var newSt = new SemaineType { Reference = "", Saison = "BASSE" };
                stList.Add(newSt);
                _stIndex = stList.Count - 1;
                Modified = true;
                // Start editing reference
                _stEditing = true;
                _stEditBuffer = "";
                _stEditCursorPos = 0;
                break;

            case ConsoleKey.Delete:
                if (stList.Count > 0)
                {
                    var refToRemove = stList[_stIndex].Reference;
                    stList.RemoveAt(_stIndex);
                    _config.Calendrier.RemoveAll(a => a.SemaineTypeRef == refToRemove);
                    Modified = true;
                    if (_stIndex >= stList.Count && stList.Count > 0)
                        _stIndex = stList.Count - 1;
                }
                break;

            case ConsoleKey.Tab:
                _view = ProgrammeView.Calendrier;
                _calIndex = 0;
                _calScroll = 0;
                break;

            default:
                if (key.KeyChar is 'c' or 'C')
                {
                    _view = ProgrammeView.Catalogue;
                    _catIndex = 0;
                    _catScroll = 0;
                    _catPickMode = false;
                    _catEditPopup = false;
                }
                break;
        }

        return true;
    }

    private bool HandleSTEditKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                // Cancel: remove the ST if ref is empty
                if (string.IsNullOrWhiteSpace(_stEditBuffer))
                {
                    _config.SemainesTypes.RemoveAt(_stIndex);
                    if (_stIndex >= _config.SemainesTypes.Count && _config.SemainesTypes.Count > 0)
                        _stIndex = _config.SemainesTypes.Count - 1;
                }
                _stEditing = false;
                break;

            case ConsoleKey.Enter:
                if (!string.IsNullOrWhiteSpace(_stEditBuffer))
                {
                    _config.SemainesTypes[_stIndex].Reference = _stEditBuffer.Trim();
                    Modified = true;
                }
                else
                {
                    // Empty ref: remove
                    _config.SemainesTypes.RemoveAt(_stIndex);
                    if (_stIndex >= _config.SemainesTypes.Count && _config.SemainesTypes.Count > 0)
                        _stIndex = _config.SemainesTypes.Count - 1;
                }
                _stEditing = false;
                break;

            case ConsoleKey.Backspace:
                if (_stEditCursorPos > 0)
                {
                    _stEditBuffer = _stEditBuffer.Remove(_stEditCursorPos - 1, 1);
                    _stEditCursorPos--;
                }
                break;

            case ConsoleKey.Delete:
                if (_stEditCursorPos < _stEditBuffer.Length)
                    _stEditBuffer = _stEditBuffer.Remove(_stEditCursorPos, 1);
                break;

            case ConsoleKey.LeftArrow:
                if (_stEditCursorPos > 0) _stEditCursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (_stEditCursorPos < _stEditBuffer.Length) _stEditCursorPos++;
                break;

            case ConsoleKey.Home:
                _stEditCursorPos = 0;
                break;

            case ConsoleKey.End:
                _stEditCursorPos = _stEditBuffer.Length;
                break;

            default:
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    _stEditBuffer = _stEditBuffer.Insert(_stEditCursorPos, key.KeyChar.ToString());
                    _stEditCursorPos++;
                }
                break;
        }

        return true;
    }

    private static void CycleSaison(SemaineType st, int direction)
    {
        var options = FormOverlay.SaisonOptions;
        int idx = Array.IndexOf(options, st.Saison);
        if (idx < 0) idx = 0;
        else idx = (idx + direction + options.Length) % options.Length;
        st.Saison = options[idx];
    }

    // ══════════════════════════════════════════════
    //  Vue 2 : BLOCS (table)
    // ══════════════════════════════════════════════

    private bool HandleKeyBlocs(ConsoleKeyInfo key)
    {
        var st = CurrentST;
        if (st == null) { _view = ProgrammeView.SemainesTypes; return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = ProgrammeView.SemainesTypes;
                break;

            case ConsoleKey.UpArrow:
                if (_blocIndex > 0) _blocIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_blocIndex < st.Blocs.Count - 1) _blocIndex++;
                break;

            case ConsoleKey.Enter:
                if (st.Blocs.Count > 0)
                {
                    _view = ProgrammeView.BlocEdit;
                    _fieldIndex = 0;
                    _fieldEditing = false;
                }
                break;

            case ConsoleKey.Insert:
                var newBloc = new BlocVol
                {
                    Sequence = 1,
                    Jour = "Lundi",
                    Periode = "AM",
                    DebutDP = "06:00",
                    FinDP = "09:00",
                    DebutFDP = "06:30",
                    FinFDP = "08:30",
                    Vols = [new Vol { Numero = "101", Depart = "NOU", Arrivee = "LIF", HeureDepart = "07:00", HeureArrivee = "07:30" }]
                };
                st.Blocs.Add(newBloc);
                _blocIndex = st.Blocs.Count - 1;
                Modified = true;
                // Go straight to edit
                _view = ProgrammeView.BlocEdit;
                _fieldIndex = 0;
                _fieldEditing = false;
                break;

            case ConsoleKey.Delete:
                if (st.Blocs.Count > 0)
                {
                    st.Blocs.RemoveAt(_blocIndex);
                    Modified = true;
                    if (_blocIndex >= st.Blocs.Count && st.Blocs.Count > 0)
                        _blocIndex = st.Blocs.Count - 1;
                }
                break;
        }

        return true;
    }

    // ══════════════════════════════════════════════
    //  Vue 3 : BLOC EDIT (champ par champ)
    // ══════════════════════════════════════════════

    // Field indices: 0=Sequence, 1=Jour, 2=Periode, 3=DebutDP, 4=FinDP, 5=DebutFDP, 6=FinFDP, 7=Vols
    private static readonly string[] FieldLabels =
        ["Sequence", "Jour", "Periode", "Debut DP", "Fin DP", "Debut FDP", "Fin FDP", "Vols"];

    public static string[] GetFieldLabels() => FieldLabels;

    private bool HandleKeyBlocEdit(ConsoleKeyInfo key)
    {
        if (_fieldEditing)
            return HandleFieldEditKey(key);

        var bloc = CurrentBloc;
        if (bloc == null) { _view = ProgrammeView.Blocs; return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = ProgrammeView.Blocs;
                break;

            case ConsoleKey.UpArrow:
                if (_fieldIndex > 0) _fieldIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_fieldIndex < FieldLabels.Length - 1) _fieldIndex++;
                break;

            case ConsoleKey.LeftArrow:
                CycleBlocField(bloc, _fieldIndex, -1);
                break;

            case ConsoleKey.RightArrow:
                CycleBlocField(bloc, _fieldIndex, +1);
                break;

            case ConsoleKey.Enter:
                if (_fieldIndex is 1 or 2)
                {
                    // Jour/Periode are selectors — Enter cycles forward
                    CycleBlocField(bloc, _fieldIndex, +1);
                }
                else if (_fieldIndex == 7)
                {
                    // Vols — navigate to VolsList
                    _view = ProgrammeView.VolsList;
                    _volIndex = 0;
                    _volScroll = 0;
                }
                else
                {
                    var val = GetBlocFieldValue(bloc, _fieldIndex);
                    _fieldEditing = true;
                    _fieldEditOriginal = val;
                    _fieldEditBuffer = IsBlocTimeField(_fieldIndex) ? StripColon(val) : val;
                    _fieldEditCursorPos = _fieldEditBuffer.Length;
                }
                break;
        }

        return true;
    }

    private bool HandleFieldEditKey(ConsoleKeyInfo key)
    {
        var bloc = CurrentBloc;
        if (bloc == null) { _fieldEditing = false; return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _fieldEditing = false;
                break;

            case ConsoleKey.Enter:
                if (IsBlocTimeField(_fieldIndex))
                {
                    var formatted = FormatHHMM(_fieldEditBuffer);
                    if (formatted == null) break;
                    SetBlocFieldValue(bloc, _fieldIndex, formatted);
                }
                else
                {
                    SetBlocFieldValue(bloc, _fieldIndex, _fieldEditBuffer);
                }
                Modified = true;
                _fieldEditing = false;
                if (_fieldIndex < FieldLabels.Length - 1) _fieldIndex++;
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
                    if (IsBlocTimeField(_fieldIndex))
                    {
                        if (!char.IsDigit(key.KeyChar) || _fieldEditBuffer.Length >= 4) break;
                    }
                    _fieldEditBuffer = _fieldEditBuffer.Insert(_fieldEditCursorPos, key.KeyChar.ToString());
                    _fieldEditCursorPos++;
                }
                break;
        }

        return true;
    }

    private static void CycleBlocField(BlocVol bloc, int fieldIndex, int direction)
    {
        switch (fieldIndex)
        {
            case 1: // Jour
                var jOpts = FormOverlay.JourOptions;
                int ji = Array.IndexOf(jOpts, bloc.Jour);
                if (ji < 0) ji = 0;
                else ji = (ji + direction + jOpts.Length) % jOpts.Length;
                bloc.Jour = jOpts[ji];
                break;
            case 2: // Periode
                var pOpts = FormOverlay.PeriodeOptions;
                int pi = Array.IndexOf(pOpts, bloc.Periode);
                if (pi < 0) pi = 0;
                else pi = (pi + direction + pOpts.Length) % pOpts.Length;
                bloc.Periode = pOpts[pi];
                break;
        }
    }

    public static string GetBlocFieldValue(BlocVol bloc, int fieldIndex) => fieldIndex switch
    {
        0 => bloc.Sequence.ToString(Inv),
        1 => bloc.Jour,
        2 => bloc.Periode,
        3 => bloc.DebutDP,
        4 => bloc.FinDP,
        5 => bloc.DebutFDP,
        6 => bloc.FinFDP,
        7 => $"{bloc.Vols.Count} vols  (Enter \u2192)",
        _ => ""
    };

    private static void SetBlocFieldValue(BlocVol bloc, int fieldIndex, string value)
    {
        switch (fieldIndex)
        {
            case 0:
                if (int.TryParse(value, NumberStyles.Integer, Inv, out var seq))
                    bloc.Sequence = seq;
                break;
            case 1: bloc.Jour = value; break;
            case 2: bloc.Periode = value; break;
            case 3: bloc.DebutDP = value; break;
            case 4: bloc.FinDP = value; break;
            case 5: bloc.DebutFDP = value; break;
            case 6: bloc.FinFDP = value; break;
        }
    }

    public static bool IsFieldSelector(int fieldIndex) => fieldIndex is 1 or 2;
    public static bool IsFieldLink(int fieldIndex) => fieldIndex == 7;

    // ══════════════════════════════════════════════
    //  Vue 4 : CALENDRIER
    // ══════════════════════════════════════════════

    public int CalendrierCount => _config.Calendrier.Count;

    private bool HandleKeyCalendrier(ConsoleKeyInfo key)
    {
        var cal = _config.Calendrier;
        if (cal.Count == 0) { _view = ProgrammeView.SemainesTypes; return true; }

        var refs = _config.SemainesTypes.Select(st => st.Reference).ToList();
        if (refs.Count == 0) { _view = ProgrammeView.SemainesTypes; return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                IsVisible = false;
                return false;

            case ConsoleKey.Tab:
                _view = ProgrammeView.SemainesTypes;
                break;

            case ConsoleKey.UpArrow:
                if (_calIndex > 0) _calIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_calIndex < cal.Count - 1) _calIndex++;
                break;

            case ConsoleKey.LeftArrow:
                CycleCalRef(cal[_calIndex], refs, -1);
                Modified = true;
                break;

            case ConsoleKey.RightArrow:
                CycleCalRef(cal[_calIndex], refs, +1);
                Modified = true;
                break;
        }

        return true;
    }

    private static void CycleCalRef(AffectationSemaine aff, List<string> refs, int direction)
    {
        int idx = refs.IndexOf(aff.SemaineTypeRef);
        if (idx < 0) idx = 0;
        else idx = (idx + direction + refs.Count) % refs.Count;
        aff.SemaineTypeRef = refs[idx];
    }

    // ══════════════════════════════════════════════
    //  Vue 5 : VOLS D'UN BLOC (VolsList)
    // ══════════════════════════════════════════════

    private static readonly string[] VolFieldLabels =
        ["Numero", "Depart", "Arrivee", "Heure depart", "Heure arrivee"];

    public static string[] GetVolFieldLabels() => VolFieldLabels;

    private bool HandleKeyVolsList(ConsoleKeyInfo key)
    {
        var bloc = CurrentBloc;
        if (bloc == null) { _view = ProgrammeView.BlocEdit; return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = ProgrammeView.BlocEdit;
                break;

            case ConsoleKey.UpArrow:
                if (_volIndex > 0) _volIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_volIndex < bloc.Vols.Count - 1) _volIndex++;
                break;

            case ConsoleKey.Enter:
                if (bloc.Vols.Count > 0)
                {
                    _view = ProgrammeView.VolEdit;
                    _volFieldIndex = 0;
                    _volFieldEditing = false;
                }
                break;

            case ConsoleKey.Insert:
                // Open catalogue in pick mode
                _view = ProgrammeView.Catalogue;
                _catIndex = 0;
                _catScroll = 0;
                _catPickMode = true;
                _catEditPopup = false;
                break;

            case ConsoleKey.Delete:
                if (bloc.Vols.Count > 0)
                {
                    bloc.Vols.RemoveAt(_volIndex);
                    Modified = true;
                    if (_volIndex >= bloc.Vols.Count && bloc.Vols.Count > 0)
                        _volIndex = bloc.Vols.Count - 1;
                }
                break;

            default:
                if (key.KeyChar is 'm' or 'M' && bloc.Vols.Count > 0)
                {
                    bloc.Vols[_volIndex].MH = !bloc.Vols[_volIndex].MH;
                    Modified = true;
                }
                break;
        }

        return true;
    }

    // ══════════════════════════════════════════════
    //  Vue 6 : EDITION D'UN VOL (VolEdit)
    // ══════════════════════════════════════════════

    private bool HandleKeyVolEdit(ConsoleKeyInfo key)
    {
        if (_volFieldEditing)
            return HandleVolFieldEditKey(key);

        var vol = CurrentVol;
        if (vol == null) { _view = ProgrammeView.VolsList; return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _view = ProgrammeView.VolsList;
                break;

            case ConsoleKey.UpArrow:
                if (_volFieldIndex > 0) _volFieldIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_volFieldIndex < VolFieldLabels.Length - 1) _volFieldIndex++;
                break;

            case ConsoleKey.Enter:
                var val = GetVolFieldValue(vol, _volFieldIndex);
                _volFieldEditing = true;
                _volFieldEditOriginal = val;
                _volFieldEditBuffer = IsVolTimeField(_volFieldIndex) ? StripColon(val) : val;
                _volFieldEditCursorPos = _volFieldEditBuffer.Length;
                break;
        }

        return true;
    }

    private static bool IsCodeField(int fieldIndex) => fieldIndex is 1 or 2;
    private static bool IsVolTimeField(int fieldIndex) => fieldIndex is 3 or 4;
    private static bool IsBlocTimeField(int fieldIndex) => fieldIndex is >= 3 and <= 6;

    private static string? FormatHHMM(string input)
    {
        if (input.Length != 4) return null;
        if (!int.TryParse(input[..2], out var hh) || !int.TryParse(input[2..], out var mm))
            return null;
        if (hh < 0 || hh > 24 || mm < 0 || mm > 59) return null;
        return $"{hh:D2}:{mm:D2}";
    }

    private static string StripColon(string val) => val.Replace(":", "");

    private bool HandleVolFieldEditKey(ConsoleKeyInfo key)
    {
        var vol = CurrentVol;
        if (vol == null) { _volFieldEditing = false; return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _volFieldEditing = false;
                break;

            case ConsoleKey.Enter:
                if (IsCodeField(_volFieldIndex) && _volFieldEditBuffer.Length != 3)
                    break;
                if (IsVolTimeField(_volFieldIndex))
                {
                    var formatted = FormatHHMM(_volFieldEditBuffer);
                    if (formatted == null) break;
                    SetVolFieldValue(vol, _volFieldIndex, formatted);
                }
                else
                {
                    SetVolFieldValue(vol, _volFieldIndex, _volFieldEditBuffer);
                }
                Modified = true;
                _volFieldEditing = false;
                if (_volFieldIndex < VolFieldLabels.Length - 1) _volFieldIndex++;
                break;

            case ConsoleKey.Backspace:
                if (_volFieldEditCursorPos > 0)
                {
                    _volFieldEditBuffer = _volFieldEditBuffer.Remove(_volFieldEditCursorPos - 1, 1);
                    _volFieldEditCursorPos--;
                }
                break;

            case ConsoleKey.Delete:
                if (_volFieldEditCursorPos < _volFieldEditBuffer.Length)
                    _volFieldEditBuffer = _volFieldEditBuffer.Remove(_volFieldEditCursorPos, 1);
                break;

            case ConsoleKey.LeftArrow:
                if (_volFieldEditCursorPos > 0) _volFieldEditCursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (_volFieldEditCursorPos < _volFieldEditBuffer.Length) _volFieldEditCursorPos++;
                break;

            case ConsoleKey.Home:
                _volFieldEditCursorPos = 0;
                break;

            case ConsoleKey.End:
                _volFieldEditCursorPos = _volFieldEditBuffer.Length;
                break;

            default:
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    var ch = key.KeyChar;
                    if (IsCodeField(_volFieldIndex))
                    {
                        if (!char.IsLetter(ch) || _volFieldEditBuffer.Length >= 3) break;
                        ch = char.ToUpper(ch);
                    }
                    else if (IsVolTimeField(_volFieldIndex))
                    {
                        if (!char.IsDigit(ch) || _volFieldEditBuffer.Length >= 4) break;
                    }
                    _volFieldEditBuffer = _volFieldEditBuffer.Insert(_volFieldEditCursorPos, ch.ToString());
                    _volFieldEditCursorPos++;
                }
                break;
        }

        return true;
    }

    public static string GetVolFieldValue(Vol vol, int fieldIndex) => fieldIndex switch
    {
        0 => vol.Numero,
        1 => vol.Depart,
        2 => vol.Arrivee,
        3 => vol.HeureDepart,
        4 => vol.HeureArrivee,
        _ => ""
    };

    private static void SetVolFieldValue(Vol vol, int fieldIndex, string value)
    {
        switch (fieldIndex)
        {
            case 0: vol.Numero = value; break;
            case 1: vol.Depart = value; break;
            case 2: vol.Arrivee = value; break;
            case 3: vol.HeureDepart = value; break;
            case 4: vol.HeureArrivee = value; break;
        }
    }

    // ══════════════════════════════════════════════
    //  Vue 7 : CATALOGUE VOLS TYPES
    // ══════════════════════════════════════════════

    private bool HandleKeyCatalogue(ConsoleKeyInfo key)
    {
        if (_catEditPopup)
            return HandleKeyCatEditPopup(key);

        var catalogue = _config.CatalogueVols;

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                if (_catPickMode)
                    _view = ProgrammeView.VolsList;
                else
                    _view = ProgrammeView.SemainesTypes;
                break;

            case ConsoleKey.UpArrow:
                if (_catIndex > 0) _catIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_catIndex < catalogue.Count - 1) _catIndex++;
                break;

            case ConsoleKey.Enter:
                if (catalogue.Count > 0)
                {
                    if (_catPickMode)
                    {
                        // Copy vol into current bloc
                        var source = catalogue[_catIndex];
                        var copy = new Vol
                        {
                            Numero = source.Numero,
                            Depart = source.Depart,
                            Arrivee = source.Arrivee,
                            HeureDepart = source.HeureDepart,
                            HeureArrivee = source.HeureArrivee,
                            MH = false
                        };
                        var bloc = CurrentBloc;
                        if (bloc != null)
                        {
                            bloc.Vols.Add(copy);
                            _volIndex = bloc.Vols.Count - 1;
                            Modified = true;
                        }
                        _view = ProgrammeView.VolsList;
                    }
                    else
                    {
                        // Open edit popup
                        _catEditVol = catalogue[_catIndex];
                        _catEditPopup = true;
                        _catEditFieldIndex = 0;
                        _catEditFieldEditing = false;
                    }
                }
                break;

            case ConsoleKey.Insert:
                if (!_catPickMode)
                {
                    var newVol = new Vol();
                    catalogue.Add(newVol);
                    _catIndex = catalogue.Count - 1;
                    Modified = true;
                    // Open edit popup
                    _catEditVol = newVol;
                    _catEditPopup = true;
                    _catEditFieldIndex = 0;
                    _catEditFieldEditing = false;
                }
                break;

            case ConsoleKey.Delete:
                if (!_catPickMode && catalogue.Count > 0)
                {
                    catalogue.RemoveAt(_catIndex);
                    Modified = true;
                    if (_catIndex >= catalogue.Count && catalogue.Count > 0)
                        _catIndex = catalogue.Count - 1;
                }
                break;
        }

        return true;
    }

    private bool HandleKeyCatEditPopup(ConsoleKeyInfo key)
    {
        if (_catEditFieldEditing)
            return HandleCatEditFieldKey(key);

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _catEditPopup = false;
                _catEditVol = null;
                break;

            case ConsoleKey.UpArrow:
                if (_catEditFieldIndex > 0) _catEditFieldIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (_catEditFieldIndex < VolFieldLabels.Length - 1) _catEditFieldIndex++;
                break;

            case ConsoleKey.Enter:
                if (_catEditVol != null)
                {
                    var val = GetVolFieldValue(_catEditVol, _catEditFieldIndex);
                    _catEditFieldEditing = true;
                    _catEditFieldOriginal = val;
                    _catEditFieldBuffer = IsVolTimeField(_catEditFieldIndex) ? StripColon(val) : val;
                    _catEditFieldCursorPos = _catEditFieldBuffer.Length;
                }
                break;
        }

        return true;
    }

    private bool HandleCatEditFieldKey(ConsoleKeyInfo key)
    {
        if (_catEditVol == null) { _catEditFieldEditing = false; return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                _catEditFieldEditing = false;
                break;

            case ConsoleKey.Enter:
                if (IsCodeField(_catEditFieldIndex) && _catEditFieldBuffer.Length != 3)
                    break;
                if (IsVolTimeField(_catEditFieldIndex))
                {
                    var formatted = FormatHHMM(_catEditFieldBuffer);
                    if (formatted == null) break;
                    SetVolFieldValue(_catEditVol, _catEditFieldIndex, formatted);
                }
                else
                {
                    SetVolFieldValue(_catEditVol, _catEditFieldIndex, _catEditFieldBuffer);
                }
                Modified = true;
                _catEditFieldEditing = false;
                if (_catEditFieldIndex < VolFieldLabels.Length - 1) _catEditFieldIndex++;
                break;

            case ConsoleKey.Backspace:
                if (_catEditFieldCursorPos > 0)
                {
                    _catEditFieldBuffer = _catEditFieldBuffer.Remove(_catEditFieldCursorPos - 1, 1);
                    _catEditFieldCursorPos--;
                }
                break;

            case ConsoleKey.Delete:
                if (_catEditFieldCursorPos < _catEditFieldBuffer.Length)
                    _catEditFieldBuffer = _catEditFieldBuffer.Remove(_catEditFieldCursorPos, 1);
                break;

            case ConsoleKey.LeftArrow:
                if (_catEditFieldCursorPos > 0) _catEditFieldCursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (_catEditFieldCursorPos < _catEditFieldBuffer.Length) _catEditFieldCursorPos++;
                break;

            case ConsoleKey.Home:
                _catEditFieldCursorPos = 0;
                break;

            case ConsoleKey.End:
                _catEditFieldCursorPos = _catEditFieldBuffer.Length;
                break;

            default:
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    var ch = key.KeyChar;
                    if (IsCodeField(_catEditFieldIndex))
                    {
                        if (!char.IsLetter(ch) || _catEditFieldBuffer.Length >= 3) break;
                        ch = char.ToUpper(ch);
                    }
                    else if (IsVolTimeField(_catEditFieldIndex))
                    {
                        if (!char.IsDigit(ch) || _catEditFieldBuffer.Length >= 4) break;
                    }
                    _catEditFieldBuffer = _catEditFieldBuffer.Insert(_catEditFieldCursorPos, ch.ToString());
                    _catEditFieldCursorPos++;
                }
                break;
        }

        return true;
    }

    // ── Scroll helpers ──

    public void EnsureBlocVisible(int contentHeight)
    {
        if (_blocIndex < _blocScroll)
            _blocScroll = _blocIndex;
        else if (_blocIndex >= _blocScroll + contentHeight)
            _blocScroll = _blocIndex - contentHeight + 1;
    }

    public void EnsureCalVisible(int contentHeight)
    {
        if (_calIndex < _calScroll)
            _calScroll = _calIndex;
        else if (_calIndex >= _calScroll + contentHeight)
            _calScroll = _calIndex - contentHeight + 1;
    }

    public void EnsureVolVisible(int contentHeight)
    {
        if (_volIndex < _volScroll)
            _volScroll = _volIndex;
        else if (_volIndex >= _volScroll + contentHeight)
            _volScroll = _volIndex - contentHeight + 1;
    }

    public void EnsureCatVisible(int contentHeight)
    {
        if (_catIndex < _catScroll)
            _catScroll = _catIndex;
        else if (_catIndex >= _catScroll + contentHeight)
            _catScroll = _catIndex - contentHeight + 1;
    }
}
