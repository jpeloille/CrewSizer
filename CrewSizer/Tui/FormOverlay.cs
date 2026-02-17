using System.Globalization;
using CrewSizer.Models;

namespace CrewSizer.Tui;

public enum FormRowKind { SectionHeader, Field, AddButton, Selector }

public class FormRow
{
    public FormRowKind Kind { get; init; }
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Key { get; set; } = "";
    public string SectionId { get; init; } = "";
    public int ListIndex { get; set; } = -1;
    public string[]? SelectorOptions { get; init; }
}

public class FormOverlay
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    internal static readonly string[] JourOptions = ["Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche"];
    internal static readonly string[] PeriodeOptions = ["AM", "PM", "MIXTE", "INT"];
    internal static readonly string[] SaisonOptions = ["BASSE", "HAUTE"];

    private readonly List<FormRow> _rows = [];
    private int _selectedIndex;
    private int _scrollOffset;
    private bool _editing;
    private string _editBuffer = "";
    private int _editCursorPos;
    private string _editOriginal = "";

    public bool IsVisible { get; private set; } = true;
    public bool Applied { get; private set; }
    public IReadOnlyList<FormRow> Rows => _rows;
    public int SelectedIndex => _selectedIndex;
    public int ScrollOffset => _scrollOffset;
    public bool IsEditing => _editing;
    public string EditBuffer => _editBuffer;
    public int EditCursorPos => _editCursorPos;

    private FormOverlay() { }

    public static FormOverlay FromConfig(Configuration config)
    {
        var form = new FormOverlay();
        form.BuildRows(config);
        form.MoveToFirstSelectable();
        return form;
    }

    private void BuildRows(Configuration config)
    {
        _rows.Clear();
        var c = config;

        // PERIODE
        AddSection("periode", "PERIODE");
        AddField("periode", "Mois", c.Periode.Mois, "periode.mois");
        AddField("periode", "Annee", c.Periode.Annee.ToString(Inv), "periode.annee");
        AddField("periode", "Nb jours", c.Periode.NbJours.ToString(Inv), "periode.nbJours");

        // EFFECTIF
        AddSection("effectif", "EFFECTIF");
        AddField("effectif", "CDB", c.Effectif.Cdb.ToString(Inv), "effectif.cdb");
        AddField("effectif", "OPL", c.Effectif.Opl.ToString(Inv), "effectif.opl");
        AddField("effectif", "CC", c.Effectif.Cc.ToString(Inv), "effectif.cc");
        AddField("effectif", "PNC", c.Effectif.Pnc.ToString(Inv), "effectif.pnc");

        // LIMITES FTL
        AddSection("ftl", "LIMITES FTL");
        AddField("ftl", "TSV max journalier", c.LimitesFTL.TsvMaxJournalier.ToString("F1", Inv), "ftl.tsvMax");
        AddField("ftl", "TSV moyen retenu", c.LimitesFTL.TsvMoyenRetenu.ToString("F1", Inv), "ftl.tsv");
        AddField("ftl", "Repos minimum", c.LimitesFTL.ReposMinimum.ToString("F1", Inv), "ftl.repos");

        // JOURS OFF
        AddSection("off", "JOURS OFF");
        AddField("off", "Reglementaire", c.JoursOff.Reglementaire.ToString(Inv), "off.regl");
        AddField("off", "Accord entreprise", c.JoursOff.AccordEntreprise.ToString(Inv), "off.accord");

        // LIMITES CUMULATIVES
        AddSection("cumul", "LIMITES CUMULATIVES");
        AddField("cumul", "H28 max", c.LimitesCumulatives.H28Max.ToString("F0", Inv), "cumul.h28");
        AddField("cumul", "H90 max", c.LimitesCumulatives.H90Max.ToString("F0", Inv), "cumul.h90");
        AddField("cumul", "H12 max", c.LimitesCumulatives.H12Max.ToString("F0", Inv), "cumul.h12");

        // COMPTEURS PNT
        AddSection("cumulPnt", "COMPTEURS ENTRANTS PNT");
        AddField("cumulPnt", "Cumul 28j", c.LimitesCumulatives.CumulPNT.Cumul28Entrant.ToString("F1", Inv), "cumul.pnt.28");
        AddField("cumulPnt", "Cumul 90j", c.LimitesCumulatives.CumulPNT.Cumul90Entrant.ToString("F1", Inv), "cumul.pnt.90");
        AddField("cumulPnt", "Cumul 12m", c.LimitesCumulatives.CumulPNT.Cumul12Entrant.ToString("F1", Inv), "cumul.pnt.12");

        // COMPTEURS PNC
        AddSection("cumulPnc", "COMPTEURS ENTRANTS PNC");
        AddField("cumulPnc", "Cumul 28j", c.LimitesCumulatives.CumulPNC.Cumul28Entrant.ToString("F1", Inv), "cumul.pnc.28");
        AddField("cumulPnc", "Cumul 90j", c.LimitesCumulatives.CumulPNC.Cumul90Entrant.ToString("F1", Inv), "cumul.pnc.90");
        AddField("cumulPnc", "Cumul 12m", c.LimitesCumulatives.CumulPNC.Cumul12Entrant.ToString("F1", Inv), "cumul.pnc.12");

        // SEMAINES TYPES (Reference + Saison uniquement — F3 pour editer les placements)
        AddSection("semtypes", "SEMAINES TYPES (F3 pour placements)");
        for (int stIdx = 0; stIdx < c.SemainesTypes.Count; stIdx++)
        {
            var st = c.SemainesTypes[stIdx];
            AddField("semtypes", $"  {stIdx + 1}. Reference", st.Reference, $"semtypes.{stIdx}.ref", stIdx);
            AddSelector("semtypes", $"  {stIdx + 1}. Saison", st.Saison, $"semtypes.{stIdx}.saison", SaisonOptions, stIdx);
        }
        AddAdd("semtypes", "Ajouter une semaine type");

        // ABATTEMENTS PNT
        AddSection("abatPnt", "ABATTEMENTS PNT");
        for (int i = 0; i < c.AbattementsPNT.Count; i++)
            AddAbattementFields("abatPnt", c.AbattementsPNT[i], i);
        AddAdd("abatPnt", "Ajouter un abattement PNT");

        // ABATTEMENTS PNC
        AddSection("abatPnc", "ABATTEMENTS PNC");
        for (int i = 0; i < c.AbattementsPNC.Count; i++)
            AddAbattementFields("abatPnc", c.AbattementsPNC[i], i);
        AddAdd("abatPnc", "Ajouter un abattement PNC");

        // FONCTIONS SOL PNT
        AddSection("solPnt", "FONCTIONS SOL PNT");
        for (int i = 0; i < c.FonctionsSolPNT.Count; i++)
            AddFonctionSolFields("solPnt", c.FonctionsSolPNT[i], i);
        AddAdd("solPnt", "Ajouter une fonction sol PNT");

        // FONCTIONS SOL PNC
        AddSection("solPnc", "FONCTIONS SOL PNC");
        for (int i = 0; i < c.FonctionsSolPNC.Count; i++)
            AddFonctionSolFields("solPnc", c.FonctionsSolPNC[i], i);
        AddAdd("solPnc", "Ajouter une fonction sol PNC");
    }

    private void AddSection(string sectionId, string label) =>
        _rows.Add(new FormRow { Kind = FormRowKind.SectionHeader, Label = label, SectionId = sectionId });

    private void AddField(string sectionId, string label, string value, string key, int listIndex = -1) =>
        _rows.Add(new FormRow { Kind = FormRowKind.Field, Label = label, Value = value, Key = key, SectionId = sectionId, ListIndex = listIndex });

    private void AddAdd(string sectionId, string label) =>
        _rows.Add(new FormRow { Kind = FormRowKind.AddButton, Label = label, SectionId = sectionId });

    private void AddSelector(string sectionId, string label, string value, string key, string[] options, int listIndex = -1) =>
        _rows.Add(new FormRow { Kind = FormRowKind.Selector, Label = label, Value = value,
            Key = key, SectionId = sectionId, ListIndex = listIndex, SelectorOptions = options });

    private void AddBlocVolFields(string section, BlocVol b, int idx)
    {
        AddField(section, $"  Bloc {idx + 1} - Sequence", b.Sequence.ToString(Inv), $"{section}.{idx}.seq", idx);
        AddSelector(section, $"  Bloc {idx + 1} - Jour", b.Jour, $"{section}.{idx}.jour", JourOptions, idx);
        AddSelector(section, $"  Bloc {idx + 1} - Periode", b.Periode, $"{section}.{idx}.periode", PeriodeOptions, idx);
        AddField(section, $"  Bloc {idx + 1} - Debut DP", b.DebutDP, $"{section}.{idx}.debutDP", idx);
        AddField(section, $"  Bloc {idx + 1} - Fin DP", b.FinDP, $"{section}.{idx}.finDP", idx);
        AddField(section, $"  Bloc {idx + 1} - Debut FDP", b.DebutFDP, $"{section}.{idx}.debutFDP", idx);
        AddField(section, $"  Bloc {idx + 1} - Fin FDP", b.FinFDP, $"{section}.{idx}.finFDP", idx);
        var volsStr = string.Join(";", b.Vols.Select(v => $"{v.Numero}-{v.Depart}-{v.Arrivee}-{v.HeureDepart}-{v.HeureArrivee}"));
        AddField(section, $"  Bloc {idx + 1} - Vols", volsStr, $"{section}.{idx}.vols", idx);
    }

    private void AddAbattementFields(string section, Abattement a, int idx)
    {
        AddField(section, $"  {idx + 1}. Libelle", a.Libelle, $"{section}.{idx}.libelle", idx);
        AddField(section, $"  {idx + 1}. Jours", a.JoursPersonnel.ToString(Inv), $"{section}.{idx}.jours", idx);
    }

    private void AddFonctionSolFields(string section, FonctionSol f, int idx)
    {
        AddField(section, $"  {idx + 1}. Nom", f.Nom, $"{section}.{idx}.nom", idx);
        AddField(section, $"  {idx + 1}. Nb personnes", f.NbPersonnes.ToString(Inv), $"{section}.{idx}.nbPers", idx);
        AddField(section, $"  {idx + 1}. Jours/mois", f.JoursSolMois.ToString(Inv), $"{section}.{idx}.joursMois", idx);
    }

    // ── Apply form values back to config ──

    public void ApplyTo(Configuration config)
    {
        // Scalar fields
        foreach (var row in _rows.Where(r => r.Kind == FormRowKind.Field && r.ListIndex < 0))
        {
            switch (row.Key)
            {
                case "periode.mois": config.Periode.Mois = row.Value; break;
                case "periode.annee": config.Periode.Annee = PInt(row.Value); break;
                case "periode.nbJours": config.Periode.NbJours = PInt(row.Value); break;
                case "effectif.cdb": config.Effectif.Cdb = PInt(row.Value); break;
                case "effectif.opl": config.Effectif.Opl = PInt(row.Value); break;
                case "effectif.cc": config.Effectif.Cc = PInt(row.Value); break;
                case "effectif.pnc": config.Effectif.Pnc = PInt(row.Value); break;
                case "ftl.tsvMax": config.LimitesFTL.TsvMaxJournalier = PDbl(row.Value); break;
                case "ftl.tsv": config.LimitesFTL.TsvMoyenRetenu = PDbl(row.Value); break;
                case "ftl.repos": config.LimitesFTL.ReposMinimum = PDbl(row.Value); break;
                case "off.regl": config.JoursOff.Reglementaire = PInt(row.Value); break;
                case "off.accord": config.JoursOff.AccordEntreprise = PInt(row.Value); break;
                case "cumul.h28": config.LimitesCumulatives.H28Max = PDbl(row.Value); break;
                case "cumul.h90": config.LimitesCumulatives.H90Max = PDbl(row.Value); break;
                case "cumul.h12": config.LimitesCumulatives.H12Max = PDbl(row.Value); break;
                case "cumul.pnt.28": config.LimitesCumulatives.CumulPNT.Cumul28Entrant = PDbl(row.Value); break;
                case "cumul.pnt.90": config.LimitesCumulatives.CumulPNT.Cumul90Entrant = PDbl(row.Value); break;
                case "cumul.pnt.12": config.LimitesCumulatives.CumulPNT.Cumul12Entrant = PDbl(row.Value); break;
                case "cumul.pnc.28": config.LimitesCumulatives.CumulPNC.Cumul28Entrant = PDbl(row.Value); break;
                case "cumul.pnc.90": config.LimitesCumulatives.CumulPNC.Cumul90Entrant = PDbl(row.Value); break;
                case "cumul.pnc.12": config.LimitesCumulatives.CumulPNC.Cumul12Entrant = PDbl(row.Value); break;
            }
        }

        // Rebuild semaines types from form rows
        RebuildSemainesTypes(config);

        config.AbattementsPNT = BuildList<Abattement>("abatPnt", (a, key, val) =>
        {
            if (key.EndsWith(".libelle")) a.Libelle = val;
            else if (key.EndsWith(".jours")) a.JoursPersonnel = PInt(val);
        });

        config.AbattementsPNC = BuildList<Abattement>("abatPnc", (a, key, val) =>
        {
            if (key.EndsWith(".libelle")) a.Libelle = val;
            else if (key.EndsWith(".jours")) a.JoursPersonnel = PInt(val);
        });

        config.FonctionsSolPNT = BuildList<FonctionSol>("solPnt", (f, key, val) =>
        {
            if (key.EndsWith(".nom")) f.Nom = val;
            else if (key.EndsWith(".nbPers")) f.NbPersonnes = PInt(val);
            else if (key.EndsWith(".joursMois")) f.JoursSolMois = PInt(val);
        });

        config.FonctionsSolPNC = BuildList<FonctionSol>("solPnc", (f, key, val) =>
        {
            if (key.EndsWith(".nom")) f.Nom = val;
            else if (key.EndsWith(".nbPers")) f.NbPersonnes = PInt(val);
            else if (key.EndsWith(".joursMois")) f.JoursSolMois = PInt(val);
        });
    }

    private void RebuildSemainesTypes(Configuration config)
    {
        // Collect form values by listIndex
        var items = new Dictionary<int, (string Reference, string Saison)>();
        foreach (var row in _rows.Where(r => r.SectionId == "semtypes" && r.ListIndex >= 0))
        {
            if (!items.ContainsKey(row.ListIndex))
                items[row.ListIndex] = ("", "BASSE");

            var item = items[row.ListIndex];
            if (row.Key.EndsWith(".ref"))
                items[row.ListIndex] = (row.Value, item.Saison);
            else if (row.Key.EndsWith(".saison"))
                items[row.ListIndex] = (item.Reference, row.Value);
        }

        var ordered = items.OrderBy(kv => kv.Key).ToList();

        // Update existing SemaineTypes (preserving Id, Placements, Blocs)
        for (int i = 0; i < ordered.Count; i++)
        {
            var (reference, saison) = ordered[i].Value;
            if (i < config.SemainesTypes.Count)
            {
                config.SemainesTypes[i].Reference = reference;
                config.SemainesTypes[i].Saison = saison;
            }
            else
            {
                config.SemainesTypes.Add(new SemaineType { Reference = reference, Saison = saison });
            }
        }

        // Remove excess
        while (config.SemainesTypes.Count > ordered.Count)
            config.SemainesTypes.RemoveAt(config.SemainesTypes.Count - 1);
    }

    private List<T> BuildList<T>(string sectionId, Action<T, string, string> setter) where T : new()
    {
        var items = new Dictionary<int, T>();
        foreach (var row in _rows.Where(r => r.Kind == FormRowKind.Field && r.SectionId == sectionId && r.ListIndex >= 0))
        {
            if (!items.ContainsKey(row.ListIndex))
                items[row.ListIndex] = new T();
            setter(items[row.ListIndex], row.Key, row.Value);
        }
        return items.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
    }

    // ── Key handling ──

    public bool HandleKey(ConsoleKeyInfo key)
    {
        if (_editing)
            return HandleEditKey(key);

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                IsVisible = false;
                Applied = false;
                return false;

            case ConsoleKey.F2:
                IsVisible = false;
                Applied = true;
                return false;

            case ConsoleKey.UpArrow:
                MovePrev();
                break;

            case ConsoleKey.DownArrow:
                MoveNext();
                break;

            case ConsoleKey.PageUp:
                JumpPrevSection();
                break;

            case ConsoleKey.PageDown:
                JumpNextSection();
                break;

            case ConsoleKey.LeftArrow:
                if (CurrentRowIsSelector()) CycleSelectorValue(_rows[_selectedIndex], -1);
                break;

            case ConsoleKey.RightArrow:
                if (CurrentRowIsSelector()) CycleSelectorValue(_rows[_selectedIndex], +1);
                break;

            case ConsoleKey.Enter:
                HandleEnter();
                break;

            case ConsoleKey.Delete:
                HandleDelete();
                break;
        }

        return true;
    }

    private bool HandleEditKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                // Cancel edit, restore original value
                _rows[_selectedIndex].Value = _editOriginal;
                _editing = false;
                break;

            case ConsoleKey.Enter:
            case ConsoleKey.Tab:
                // Confirm edit and move to next field
                _rows[_selectedIndex].Value = _editBuffer;
                _editing = false;
                MoveNext();
                break;

            case ConsoleKey.Backspace:
                if (_editCursorPos > 0)
                {
                    _editBuffer = _editBuffer.Remove(_editCursorPos - 1, 1);
                    _editCursorPos--;
                }
                break;

            case ConsoleKey.Delete:
                if (_editCursorPos < _editBuffer.Length)
                    _editBuffer = _editBuffer.Remove(_editCursorPos, 1);
                break;

            case ConsoleKey.LeftArrow:
                if (_editCursorPos > 0) _editCursorPos--;
                break;

            case ConsoleKey.RightArrow:
                if (_editCursorPos < _editBuffer.Length) _editCursorPos++;
                break;

            case ConsoleKey.Home:
                _editCursorPos = 0;
                break;

            case ConsoleKey.End:
                _editCursorPos = _editBuffer.Length;
                break;

            default:
                if (key.KeyChar >= 32 && key.KeyChar < 127)
                {
                    _editBuffer = _editBuffer.Insert(_editCursorPos, key.KeyChar.ToString());
                    _editCursorPos++;
                }
                break;
        }

        return true;
    }

    private void HandleEnter()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;
        var row = _rows[_selectedIndex];

        switch (row.Kind)
        {
            case FormRowKind.Field:
                _editing = true;
                _editOriginal = row.Value;
                _editBuffer = row.Value;
                _editCursorPos = _editBuffer.Length;
                break;

            case FormRowKind.Selector:
                CycleSelectorValue(row, +1);
                break;

            case FormRowKind.AddButton:
                AddNewListItem(row.SectionId);
                break;
        }
    }

    private void HandleDelete()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;
        var row = _rows[_selectedIndex];

        if (row.Kind != FormRowKind.Field || row.ListIndex < 0) return;

        var sectionId = row.SectionId;
        var listIndex = row.ListIndex;

        // Remove all rows with this section + listIndex
        _rows.RemoveAll(r => r.SectionId == sectionId && r.ListIndex == listIndex);

        // Renumber remaining items in this section
        int newIdx = 0;
        int? lastIdx = null;
        foreach (var r in _rows.Where(r => r.SectionId == sectionId && r.ListIndex >= 0))
        {
            if (r.ListIndex != lastIdx)
            {
                lastIdx = r.ListIndex;
                newIdx = lastIdx == null ? 0 : newIdx;
            }
            r.ListIndex = newIdx;
        }

        // Full renumber pass
        RenumberSection(sectionId);

        // Clamp selection
        if (_selectedIndex >= _rows.Count)
            _selectedIndex = _rows.Count - 1;
        MoveToNearestSelectable();
    }

    private void RenumberSection(string sectionId)
    {
        int idx = -1;
        int? prevOrigIdx = null;
        foreach (var r in _rows.Where(r => r.SectionId == sectionId && r.ListIndex >= 0))
        {
            if (r.ListIndex != prevOrigIdx)
            {
                idx++;
                prevOrigIdx = r.ListIndex;
            }
            r.ListIndex = idx;

            // Rebuild key: replace index in "section.OLD.field" → "section.NEW.field"
            var keyParts = r.Key.Split('.');
            if (keyParts.Length >= 3)
            {
                keyParts[1] = idx.ToString();
                r.Key = string.Join('.', keyParts);
            }

            // Update label number
            if (sectionId == "prog")
            {
                var field = r.Key.Split('.').Last();
                r.Label = field switch
                {
                    "nom" => $"  Bloc {idx + 1} - Nom",
                    "jour" => $"  Bloc {idx + 1} - Jour (1-7)",
                    "debutTS" => $"  Bloc {idx + 1} - Debut TS",
                    "finTS" => $"  Bloc {idx + 1} - Fin TS",
                    "debutTSV" => $"  Bloc {idx + 1} - Debut TSV",
                    "finTSV" => $"  Bloc {idx + 1} - Fin TSV",
                    "etapes" => $"  Bloc {idx + 1} - Etapes",
                    _ => r.Label
                };
            }
            else
            {
                var trimmed = r.Label.TrimStart();
                var dotPos = trimmed.IndexOf('.');
                if (dotPos > 0 && int.TryParse(trimmed[..dotPos], out _))
                {
                    var prefix = new string(' ', r.Label.Length - trimmed.Length);
                    r.Label = $"{prefix}{idx + 1}{trimmed[dotPos..]}";
                }
            }
        }
    }

    private void AddNewListItem(string sectionId)
    {
        // Find the AddButton for this section
        int addBtnIdx = _rows.FindIndex(r => r.Kind == FormRowKind.AddButton && r.SectionId == sectionId);
        if (addBtnIdx < 0) return;

        // Determine new list index
        int maxIdx = _rows.Where(r => r.SectionId == sectionId && r.ListIndex >= 0)
            .Select(r => r.ListIndex).DefaultIfEmpty(-1).Max();
        int newIdx = maxIdx + 1;

        // Create rows based on section type
        var newRows = new List<FormRow>();
        if (sectionId == "semtypes")
        {
            newRows.Add(MakeField(sectionId, $"  {newIdx + 1}. Reference", "", $"{sectionId}.{newIdx}.ref", newIdx));
            newRows.Add(MakeSelector(sectionId, $"  {newIdx + 1}. Saison", "BASSE", $"{sectionId}.{newIdx}.saison", SaisonOptions, newIdx));
        }
        else switch (sectionId)
        {
            case "abatPnt" or "abatPnc":
                newRows.Add(MakeField(sectionId, $"  {newIdx + 1}. Libelle", "", $"{sectionId}.{newIdx}.libelle", newIdx));
                newRows.Add(MakeField(sectionId, $"  {newIdx + 1}. Jours", "0", $"{sectionId}.{newIdx}.jours", newIdx));
                break;
            case "solPnt" or "solPnc":
                newRows.Add(MakeField(sectionId, $"  {newIdx + 1}. Nom", "", $"{sectionId}.{newIdx}.nom", newIdx));
                newRows.Add(MakeField(sectionId, $"  {newIdx + 1}. Nb personnes", "0", $"{sectionId}.{newIdx}.nbPers", newIdx));
                newRows.Add(MakeField(sectionId, $"  {newIdx + 1}. Jours/mois", "0", $"{sectionId}.{newIdx}.joursMois", newIdx));
                break;
        }

        // Insert before AddButton
        _rows.InsertRange(addBtnIdx, newRows);

        // Position cursor on the first new field and start editing
        _selectedIndex = addBtnIdx;
        _editing = true;
        _editOriginal = newRows[0].Value;
        _editBuffer = newRows[0].Value;
        _editCursorPos = _editBuffer.Length;
    }

    private static FormRow MakeField(string section, string label, string value, string key, int listIndex) =>
        new() { Kind = FormRowKind.Field, Label = label, Value = value, Key = key, SectionId = section, ListIndex = listIndex };

    private static FormRow MakeSelector(string section, string label, string value, string key, string[] options, int listIndex) =>
        new() { Kind = FormRowKind.Selector, Label = label, Value = value, Key = key, SectionId = section, ListIndex = listIndex, SelectorOptions = options };

    // ── Selector helpers ──

    private static void CycleSelectorValue(FormRow row, int direction)
    {
        if (row.SelectorOptions is not { Length: > 0 }) return;
        int idx = Array.IndexOf(row.SelectorOptions, row.Value);
        if (idx < 0) idx = 0;
        else idx = (idx + direction + row.SelectorOptions.Length) % row.SelectorOptions.Length;
        row.Value = row.SelectorOptions[idx];
    }

    private bool CurrentRowIsSelector()
        => _selectedIndex >= 0 && _selectedIndex < _rows.Count
           && _rows[_selectedIndex].Kind == FormRowKind.Selector;

    // ── Navigation helpers ──

    private bool IsSelectable(FormRow row) => row.Kind is FormRowKind.Field or FormRowKind.AddButton or FormRowKind.Selector;

    private void MoveNext()
    {
        for (int i = _selectedIndex + 1; i < _rows.Count; i++)
        {
            if (IsSelectable(_rows[i]))
            {
                _selectedIndex = i;
                return;
            }
        }
    }

    private void MovePrev()
    {
        for (int i = _selectedIndex - 1; i >= 0; i--)
        {
            if (IsSelectable(_rows[i]))
            {
                _selectedIndex = i;
                return;
            }
        }
    }

    private void MoveToFirstSelectable()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (IsSelectable(_rows[i]))
            {
                _selectedIndex = i;
                return;
            }
        }
    }

    private void MoveToNearestSelectable()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _rows.Count && IsSelectable(_rows[_selectedIndex]))
            return;
        // Try forward
        for (int i = _selectedIndex; i < _rows.Count; i++)
        {
            if (IsSelectable(_rows[i])) { _selectedIndex = i; return; }
        }
        // Try backward
        for (int i = Math.Min(_selectedIndex, _rows.Count - 1); i >= 0; i--)
        {
            if (IsSelectable(_rows[i])) { _selectedIndex = i; return; }
        }
    }

    private void JumpNextSection()
    {
        if (_selectedIndex < 0) return;
        var currentSection = _rows[_selectedIndex].SectionId;
        for (int i = _selectedIndex + 1; i < _rows.Count; i++)
        {
            if (_rows[i].SectionId != currentSection && IsSelectable(_rows[i]))
            {
                _selectedIndex = i;
                return;
            }
        }
    }

    private void JumpPrevSection()
    {
        if (_selectedIndex < 0) return;
        var currentSection = _rows[_selectedIndex].SectionId;
        // Find start of previous section
        string? prevSection = null;
        for (int i = _selectedIndex - 1; i >= 0; i--)
        {
            if (_rows[i].SectionId != currentSection)
            {
                prevSection = _rows[i].SectionId;
                break;
            }
        }
        if (prevSection == null) return;
        // Move to first selectable of that section
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].SectionId == prevSection && IsSelectable(_rows[i]))
            {
                _selectedIndex = i;
                return;
            }
        }
    }

    public void EnsureVisibleInHeight(int height)
    {
        if (_selectedIndex < _scrollOffset)
            _scrollOffset = _selectedIndex;
        else if (_selectedIndex >= _scrollOffset + height)
            _scrollOffset = _selectedIndex - height + 1;

        // Show section header above if possible
        if (_scrollOffset > 0 && _selectedIndex > 0)
        {
            int headerIdx = -1;
            for (int i = _selectedIndex; i >= 0; i--)
            {
                if (_rows[i].Kind == FormRowKind.SectionHeader) { headerIdx = i; break; }
            }
            if (headerIdx >= 0 && headerIdx < _scrollOffset && _selectedIndex - headerIdx < height)
                _scrollOffset = headerIdx;
        }
    }

    // ── Parse helpers ──

    private static int PInt(string s) =>
        int.TryParse(s, NumberStyles.Integer, Inv, out var v) ? v : 0;

    private static double PDbl(string s) =>
        double.TryParse(s, NumberStyles.Float, Inv, out var v) ? v : 0;
}
