using System.Text;

namespace CrewSizer.Tui;

public class ScreenRenderer
{
    private readonly ITheme _theme;
    private readonly StringBuilder _frameBuffer = new(4096);
    private int _width;
    private int _height;

    public ScreenRenderer(ITheme theme)
    {
        _theme = theme;
    }

    public void BeginFrame()
    {
        _frameBuffer.Clear();
        _frameBuffer.Append("\x1b[?2026h");
        _frameBuffer.Append("\x1b[?25l");
    }

    public void FlushFrame()
    {
        _frameBuffer.Append("\x1b[?2026l");
        Console.Write(_frameBuffer.ToString());
        _frameBuffer.Clear();
    }

    public void SetCursorVisible(bool visible)
    {
        _frameBuffer.Append(visible ? "\x1b[?25h" : "\x1b[?25l");
    }

    public int ViewportHeight => _height - 6;

    private int ContentWidth => _width - (_theme.HasBorders ? 2 : 0);

    public void UpdateSize()
    {
        _width = Console.WindowWidth > 0 ? Console.WindowWidth : 80;
        _height = Console.WindowHeight > 0 ? Console.WindowHeight : 24;
    }

    public void RenderFrame(string configName, string period, string statusExtra)
    {
        if (_theme.HasBorders)
            RenderFrameBordered(configName, period, statusExtra);
        else
            RenderFrameMinimalist(configName, period, statusExtra);
    }

    private void RenderFrameMinimalist(string configName, string period, string statusExtra)
    {
        var title = $"  CREWSIZER{new string(' ', 20)}F1=Aide  F2=Config  F3=Programme  F10=Quitter";
        BufferAt(0, 0, _theme.TitleBar + PadFull(title) + _theme.Reset);

        var status = $" {configName}   {period}";
        if (!string.IsNullOrEmpty(statusExtra))
            status += $"   {statusExtra}";
        BufferAt(0, 1, _theme.StatusBar + PadFull(status) + _theme.Reset);

        BufferAt(0, 2, _theme.SeparatorColor + new string(_theme.Horizontal, _width) + _theme.Reset);
        BufferAt(0, _height - 3, _theme.SeparatorColor + new string(_theme.Horizontal, _width) + _theme.Reset);

        var hints = " calc  show  set  add  del  save  load  new  help  quit";
        BufferAt(0, _height - 1, _theme.HintBar + PadFull(hints) + _theme.Reset);
    }

    private void RenderFrameBordered(string configName, string period, string statusExtra)
    {
        var inner = _width - 2;

        var titleText = " CREWSIZER ";
        var shortcutsText = " F1=Aide  F2=Config  F3=Programme  F10=Quitter ";
        var fillLen = inner - titleText.Length - shortcutsText.Length;
        if (fillLen < 0) fillLen = 0;
        var topBorder = $"{_theme.BorderColor}{_theme.TopLeft}{_theme.Horizontal}{_theme.Horizontal}" +
                        $"{_theme.TitleBar}{titleText}{_theme.BorderColor}" +
                        new string(_theme.Horizontal, fillLen) +
                        $"{_theme.TitleBar}{shortcutsText}{_theme.BorderColor}" +
                        $"{_theme.Horizontal}{_theme.TopRight}{_theme.Reset}";
        BufferAt(0, 0, topBorder);

        var status = $" {configName}   {period}";
        if (!string.IsNullOrEmpty(statusExtra))
            status += $"   {statusExtra}";
        BufferAt(0, 1, WrapRow(_theme.StatusBar, status));

        BufferAt(0, 2, MakeSeparator());
        BufferAt(0, _height - 3, MakeSeparator());

        var hints = " calc  show  set  add  del  save  load  new  help  quit ";
        var hintFill = inner - hints.Length;
        var leftFill = hintFill / 2;
        var rightFill = hintFill - leftFill;
        if (leftFill < 0) { leftFill = 0; rightFill = 0; }
        var bottomBorder = $"{_theme.BorderColor}{_theme.BottomLeft}" +
                           new string(_theme.Horizontal, leftFill) +
                           $"{_theme.HintBar}{hints}{_theme.BorderColor}" +
                           new string(_theme.Horizontal, rightFill) +
                           $"{_theme.BottomRight}{_theme.Reset}";
        BufferAt(0, _height - 1, bottomBorder);
    }

    public void RenderContent(OutputBuffer buffer)
    {
        var viewportHeight = ViewportHeight;
        var lines = buffer.GetVisibleLines(viewportHeight);
        var cw = ContentWidth;

        var showScrollbar = _theme.HasBorders && buffer.TotalLines > viewportHeight;
        var scrollChars = showScrollbar ? BuildScrollbar(viewportHeight, buffer.TotalLines, buffer.ScrollOffset) : null;

        var effectiveCw = showScrollbar ? cw - 1 : cw;

        for (var i = 0; i < viewportHeight; i++)
        {
            var row = 3 + i;
            var content = "";
            if (i < lines.Count)
            {
                var line = lines[i];
                var visLen = VisibleLength(line);
                content = visLen > effectiveCw ? VisibleTruncate(line, effectiveCw) : line;
            }

            if (showScrollbar)
                BufferAt(0, row, WrapRowWithScrollbar(_theme.Normal, content, scrollChars![i]));
            else if (_theme.HasBorders)
                BufferAt(0, row, WrapRow(_theme.Normal, content));
            else
                BufferAt(0, row, _theme.Normal + PadFull(content) + _theme.Reset);
        }

        if (!showScrollbar && buffer.TotalLines > viewportHeight)
        {
            var canUp = buffer.CanScrollUp;
            var canDown = buffer.CanScrollDown(viewportHeight);
            if (canUp || canDown)
            {
                var hint = canUp && canDown ? "PgUp/PgDn"
                    : canUp ? "PgUp"
                    : "PgDn";
                var indicator = $"{_theme.ScrollHint}{hint} {_theme.Horizontal}{_theme.Horizontal}{_theme.Reset}";
                var borderOffset = _theme.HasBorders ? 1 : 0;
                var col = _width - hint.Length - 3 - borderOffset;
                if (col > 0)
                    BufferAt(col, _height - 4, indicator);
            }
        }
    }

    private static char[] BuildScrollbar(int viewportHeight, int totalLines, int scrollOffset)
    {
        var chars = new char[viewportHeight];
        var thumbSize = Math.Max(1, viewportHeight * viewportHeight / totalLines);
        if (thumbSize > viewportHeight - 2) thumbSize = viewportHeight - 2;

        var maxScroll = totalLines - viewportHeight;
        var trackRange = viewportHeight - 2 - thumbSize;
        var thumbPos = maxScroll > 0 && trackRange > 0
            ? scrollOffset * trackRange / maxScroll
            : 0;

        for (var i = 0; i < viewportHeight; i++)
        {
            if (i == 0)
                chars[i] = '\u25b2';
            else if (i == viewportHeight - 1)
                chars[i] = '\u25bc';
            else if (i >= thumbPos + 1 && i < thumbPos + 1 + thumbSize)
                chars[i] = '\u2588';
            else
                chars[i] = '\u2591';
        }

        return chars;
    }

    public void RenderInputLine(string input, int cursorPos)
    {
        var row = _height - 2;

        if (_theme.HasBorders)
        {
            var cw = ContentWidth;
            var content = $"> {input}";
            var line = $"{_theme.BorderColor}{_theme.Vertical}{_theme.Prompt}> {_theme.InputText}{input}" +
                       new string(' ', Math.Max(0, cw - content.Length)) +
                       $"{_theme.BorderColor}{_theme.Vertical}{_theme.Reset}";
            BufferAt(0, row, line);
            BufferCursorPos(3 + cursorPos, row);
        }
        else
        {
            var line = $"{_theme.Prompt}> {_theme.InputText}{input}{_theme.Reset}";
            BufferAt(0, row, line + new string(' ', Math.Max(0, _width - input.Length - 2)));
            BufferCursorPos(2 + cursorPos, row);
        }
    }

    public void RenderBlankViewport()
    {
        for (var i = 0; i < ViewportHeight; i++)
        {
            var row = 3 + i;
            if (_theme.HasBorders)
                BufferAt(0, row, WrapRow(_theme.Normal, ""));
            else
                BufferAt(0, row, _theme.Normal + PadFull("") + _theme.Reset);
        }

        var inputRow = _height - 2;
        if (_theme.HasBorders)
            BufferAt(0, inputRow, WrapRow(_theme.Normal, ""));
        else
            BufferAt(0, inputRow, _theme.Normal + PadFull("") + _theme.Reset);
    }

    public void RenderOverlay(ListBoxOverlay overlay)
    {
        var maxItemLen = overlay.Items.Max(f => f.Length);
        var boxInnerWidth = Math.Max(30, maxItemLen + 8);
        var boxOuterWidth = boxInnerWidth + 2;
        if (boxOuterWidth > _width - 4) boxOuterWidth = _width - 4;
        boxInnerWidth = boxOuterWidth - 2;

        var maxListHeight = ViewportHeight - 4;
        var listHeight = Math.Min(overlay.Items.Count, Math.Max(3, maxListHeight));
        overlay.EnsureVisibleInHeight(listHeight);

        var boxTotalHeight = listHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        var showScroll = overlay.Items.Count > listHeight;
        var itemWidth = showScroll ? boxInnerWidth - 3 : boxInnerWidth - 2;
        var scrollChars = showScroll ? BuildScrollbar(listHeight, overlay.Items.Count, overlay.ScrollOffset) : null;

        const string selectedColor = "\x1b[97;44;7m";
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;

        var titleText = $" {overlay.Title} ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = titleFill / 2;
        var titleRight = titleFill - titleLeft;
        if (titleLeft < 0) { titleLeft = 0; titleRight = 0; }
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        for (var i = 0; i < listHeight; i++)
        {
            var itemIndex = overlay.ScrollOffset + i;
            var rowY = startRow + 1 + i;

            string itemText;
            string itemColor;
            if (itemIndex < overlay.Items.Count)
            {
                var isCursor = itemIndex == overlay.SelectedIndex;
                var isChecked = overlay.CheckedIndices.Contains(itemIndex);
                var checkbox = isChecked ? "[X]" : "[ ]";
                var raw = $" {checkbox} {overlay.Items[itemIndex]}";
                itemText = raw.Length > itemWidth ? raw[..itemWidth] : raw + new string(' ', itemWidth - raw.Length);
                itemColor = isCursor ? selectedColor : _theme.Normal;
            }
            else
            {
                itemText = new string(' ', itemWidth);
                itemColor = _theme.Normal;
            }

            if (showScroll)
            {
                BufferAt(startCol, rowY,
                    $"{bc}{v}{itemColor}{itemText}{bc}{scrollChars![i]}{v}{_theme.Reset}");
            }
            else
            {
                var padLen = boxInnerWidth - itemWidth;
                BufferAt(startCol, rowY,
                    $"{bc}{v}{itemColor}{itemText}{new string(' ', padLen)}{bc}{v}{_theme.Reset}");
            }
        }

        BufferAt(startCol, startRow + 1 + listHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        var footer = " \u2191\u2193 nav  Espace cocher  Enter ok  Esc ann ";
        if (footer.Length > boxInnerWidth)
            footer = footer[..boxInnerWidth];
        var footerPad = boxInnerWidth - footer.Length;
        BufferAt(startCol, startRow + 2 + listHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        BufferAt(startCol, startRow + 3 + listHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");
    }

    public void RenderHelpOverlay(HelpOverlay overlay)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";

        var boxOuterWidth = Math.Min(_width - 4, Math.Max(60, _width * 9 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        var contentHeight = Math.Max(6, ViewportHeight - 4);
        var boxTotalHeight = contentHeight + 4;

        var maxNameLen = HelpContent.Entries.Max(e => e.Name.Length);
        var leftPanelWidth = maxNameLen + 4;
        var rightPanelWidth = boxInnerWidth - leftPanelWidth - 1;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        overlay.EnsureListVisibleInHeight(contentHeight);
        overlay.ClampDetailScroll(contentHeight);

        var detailLines = overlay.CurrentEntry.Lines;
        var showDetailScroll = detailLines.Length > contentHeight;
        var detailScrollChars = showDetailScroll
            ? BuildScrollbar(contentHeight, detailLines.Length, overlay.DetailScrollOffset)
            : null;

        var titleText = " Aide ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = titleFill / 2;
        var titleRight = titleFill - titleLeft;
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;

            var listIndex = overlay.ListScrollOffset + i;
            string leftText;
            string leftColor;
            if (listIndex < HelpContent.Entries.Count)
            {
                var isCursor = listIndex == overlay.SelectedIndex;
                var name = $"  {HelpContent.Entries[listIndex].Name}";
                leftText = name.Length > leftPanelWidth ? name[..leftPanelWidth] : name + new string(' ', leftPanelWidth - name.Length);
                leftColor = isCursor ? selectedColor : _theme.Normal;
            }
            else
            {
                leftText = new string(' ', leftPanelWidth);
                leftColor = _theme.Normal;
            }

            var detailIndex = overlay.DetailScrollOffset + i;
            string rightText;
            var effectiveRightWidth = showDetailScroll ? rightPanelWidth - 1 : rightPanelWidth;
            if (detailIndex < detailLines.Length)
            {
                var line = $" {detailLines[detailIndex]}";
                rightText = line.Length > effectiveRightWidth ? line[..effectiveRightWidth] : line + new string(' ', effectiveRightWidth - line.Length);
            }
            else
            {
                rightText = new string(' ', effectiveRightWidth);
            }

            var scrollChar = showDetailScroll ? $"{bc}{detailScrollChars![i]}" : "";
            BufferAt(startCol, rowY,
                $"{bc}{v}{leftColor}{leftText}{_theme.Reset}{bc}{v}{_theme.Normal}{rightText}{scrollChar}{bc}{v}{_theme.Reset}");
        }

        var sepLeft = new string(h, leftPanelWidth);
        var sepRight = new string(h, boxInnerWidth - leftPanelWidth - 1);
        var teeDown = _theme.HasBorders ? '\u2567' : '\u2534';
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{sepLeft}{teeDown}{sepRight}{_theme.TeeRight}{_theme.Reset}");

        var footer = " \u2191\u2193 naviguer  PgUp/PgDn detail  Esc fermer ";
        if (footer.Length > boxInnerWidth)
            footer = footer[..boxInnerWidth];
        var footerPad = boxInnerWidth - footer.Length;
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");
    }

    public void RenderFormOverlay(FormOverlay form)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";
        const string editColor = "\x1b[97;42m";

        var boxOuterWidth = Math.Min(_width - 4, Math.Max(60, _width * 9 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        var contentHeight = Math.Max(6, ViewportHeight - 4);
        var boxTotalHeight = contentHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        form.EnsureVisibleInHeight(contentHeight);

        var showScroll = form.Rows.Count > contentHeight;
        var scrollChars = showScroll
            ? BuildScrollbar(contentHeight, form.Rows.Count, form.ScrollOffset)
            : null;
        var effectiveWidth = showScroll ? boxInnerWidth - 1 : boxInnerWidth;

        // Title
        var titleText = " Configuration ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = titleFill / 2;
        var titleRight = titleFill - titleLeft;
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        // Content rows
        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;
            var rowIdx = form.ScrollOffset + i;

            string text;
            string color;

            if (rowIdx < form.Rows.Count)
            {
                var row = form.Rows[rowIdx];
                var isCursor = rowIdx == form.SelectedIndex;

                switch (row.Kind)
                {
                    case FormRowKind.SectionHeader:
                        text = $"  {row.Label}";
                        color = _theme.TitleBar;
                        break;

                    case FormRowKind.Field:
                    {
                        var labelWidth = 28;
                        var label = row.Label.Length > labelWidth ? row.Label[..labelWidth] : row.Label;
                        var dots = new string('.', Math.Max(1, labelWidth - label.Length));

                        if (isCursor && form.IsEditing)
                        {
                            text = $"  {label} {dots} [{form.EditBuffer}]";
                            color = editColor;
                        }
                        else
                        {
                            text = $"  {label} {dots} {row.Value}";
                            color = isCursor ? selectedColor : _theme.Normal;
                        }
                        break;
                    }

                    case FormRowKind.Selector:
                    {
                        var labelWidth2 = 28;
                        var label2 = row.Label.Length > labelWidth2 ? row.Label[..labelWidth2] : row.Label;
                        var dots2 = new string('.', Math.Max(1, labelWidth2 - label2.Length));
                        text = $"  {label2} {dots2} < {row.Value} >";
                        color = isCursor ? selectedColor : _theme.Normal;
                        break;
                    }

                    case FormRowKind.AddButton:
                        text = $"  [{row.Label}]";
                        color = isCursor ? selectedColor : _theme.HintBar;
                        break;

                    default:
                        text = "";
                        color = _theme.Normal;
                        break;
                }

                if (text.Length > effectiveWidth)
                    text = text[..effectiveWidth];
                var pad = Math.Max(0, effectiveWidth - VisibleLength(text));

                if (showScroll)
                    BufferAt(startCol, rowY,
                        $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{scrollChars![i]}{v}{_theme.Reset}");
                else
                    BufferAt(startCol, rowY,
                        $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
            }
            else
            {
                var blank = new string(' ', effectiveWidth);
                if (showScroll)
                    BufferAt(startCol, rowY,
                        $"{bc}{v}{_theme.Normal}{blank}{bc}{scrollChars![i]}{v}{_theme.Reset}");
                else
                    BufferAt(startCol, rowY,
                        $"{bc}{v}{_theme.Normal}{blank}{bc}{v}{_theme.Reset}");
            }
        }

        // Separator
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = form.IsEditing
            ? " Enter ok  Esc annuler  Tab suivant "
            : form.SelectedIndex >= 0 && form.SelectedIndex < form.Rows.Count
              && form.Rows[form.SelectedIndex].Kind == FormRowKind.Selector
                ? " \u2190\u2192 changer  \u2191\u2193 nav  PgUp/PgDn section  Suppr  F2 valider  Esc annuler "
                : " \u2191\u2193 nav  PgUp/PgDn section  Enter editer  Suppr  F2 valider  Esc annuler ";
        if (footer.Length > boxInnerWidth)
            footer = footer[..boxInnerWidth];
        var footerPad = boxInnerWidth - footer.Length;
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom border
        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");

        // Cursor position when editing
        if (form.IsEditing)
        {
            var cursorRowIdx = form.SelectedIndex - form.ScrollOffset;
            if (cursorRowIdx >= 0 && cursorRowIdx < contentHeight)
            {
                var row = form.Rows[form.SelectedIndex];
                var labelWidth = 28;
                var label = row.Label.Length > labelWidth ? row.Label[..labelWidth] : row.Label;
                var dots = new string('.', Math.Max(1, labelWidth - label.Length));
                var prefix = $"  {label} {dots} [";
                var col = startCol + 1 + prefix.Length + form.EditCursorPos;
                BufferCursorPos(col, startRow + 1 + cursorRowIdx);
            }
        }
    }

    // ── Programme Overlay ──

    public void RenderProgrammeOverlay(ProgrammeOverlay overlay)
    {
        switch (overlay.View)
        {
            case ProgrammeView.SemainesTypes:
                RenderProgSemainesTypes(overlay);
                break;
            case ProgrammeView.Blocs:
                RenderProgBlocs(overlay);
                break;
            case ProgrammeView.BlocEdit:
                RenderProgBlocEdit(overlay);
                break;
            case ProgrammeView.Calendrier:
                RenderProgCalendrier(overlay);
                break;
            case ProgrammeView.VolsList:
                RenderProgVolsList(overlay);
                break;
            case ProgrammeView.VolEdit:
                RenderProgVolEdit(overlay);
                break;
            case ProgrammeView.Catalogue:
                RenderProgCatalogue(overlay);
                break;
        }
    }

    private void RenderProgSemainesTypes(ProgrammeOverlay overlay)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";
        const string editColor = "\x1b[97;42m";

        var stList = overlay.Config.SemainesTypes;
        var boxOuterWidth = Math.Min(_width - 4, Math.Max(50, _width * 7 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        var contentHeight = Math.Max(4, Math.Min(stList.Count + 2, ViewportHeight - 6));
        var boxTotalHeight = contentHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        // Title
        var titleText = " Semaines Types ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = titleFill / 2;
        var titleRight = titleFill - titleLeft;
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        // Content
        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;
            string text;
            string color;

            if (i < stList.Count)
            {
                var st = stList[i];
                var isCursor = i == overlay.StIndex;
                var marker = isCursor ? ">" : " ";
                var blocCount = st.Blocs.Count;

                if (isCursor && overlay.StEditing)
                {
                    text = $"  {marker} [{overlay.StEditBuffer}]   < {st.Saison} >   {blocCount} blocs";
                    color = editColor;
                }
                else
                {
                    var refStr = st.Reference.Length > 0 ? st.Reference : "(vide)";
                    text = $"  {marker} {refStr,-10} < {st.Saison,-6} >   {blocCount} blocs";
                    color = isCursor ? selectedColor : _theme.Normal;
                }
            }
            else
            {
                text = "";
                color = _theme.Normal;
            }

            if (text.Length > boxInnerWidth) text = text[..boxInnerWidth];
            var pad = Math.Max(0, boxInnerWidth - VisibleLength(text));
            BufferAt(startCol, rowY,
                $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
        }

        // Separator
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = overlay.StEditing
            ? " Enter ok  Esc annuler "
            : " Enter blocs  \u2190\u2192 saison  Ins  Suppr  C catalogue  Tab calendrier  Esc fermer ";
        if (footer.Length > boxInnerWidth) footer = footer[..boxInnerWidth];
        var footerPad = Math.Max(0, boxInnerWidth - footer.Length);
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom
        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");

        // Cursor for editing
        if (overlay.StEditing)
        {
            var cursorRow = overlay.StIndex;
            if (cursorRow >= 0 && cursorRow < contentHeight)
            {
                var prefix = "  > [";
                var col = startCol + 1 + prefix.Length + overlay.StEditCursorPos;
                BufferCursorPos(col, startRow + 1 + cursorRow);
            }
        }
    }

    private void RenderProgBlocs(ProgrammeOverlay overlay)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";

        var st = overlay.CurrentST;
        if (st == null) return;

        var boxOuterWidth = Math.Min(_width - 4, Math.Max(70, _width * 9 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        var contentHeight = Math.Max(4, ViewportHeight - 6);
        var boxTotalHeight = contentHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        overlay.EnsureBlocVisible(contentHeight - 2); // -2 for header row + separator

        var showScroll = st.Blocs.Count + 2 > contentHeight;
        var scrollChars = showScroll ? BuildScrollbar(contentHeight, st.Blocs.Count + 2, overlay.BlocScroll) : null;
        var effectiveWidth = showScroll ? boxInnerWidth - 1 : boxInnerWidth;

        // Title
        var titleText = $" Blocs - {st.Reference} ({st.Saison}) ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = Math.Max(0, titleFill / 2);
        var titleRight = Math.Max(0, titleFill - titleLeft);
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        // Content
        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;
            string text;
            string color;

            var dataIdx = overlay.BlocScroll + i;

            if (dataIdx == 0)
            {
                // Header row
                text = "  #  Seq  Jour       Per   DP           FDP          Vols  HDV";
                color = _theme.TitleBar;
            }
            else if (dataIdx == 1)
            {
                // Separator
                text = "  " + new string('\u2500', Math.Min(62, effectiveWidth - 2));
                color = _theme.Normal;
            }
            else
            {
                var blocIdx = dataIdx - 2;
                if (blocIdx >= 0 && blocIdx < st.Blocs.Count)
                {
                    var bloc = st.Blocs[blocIdx];
                    var isCursor = blocIdx == overlay.BlocIndex;
                    var marker = isCursor ? ">" : " ";
                    var dp = $"{bloc.DebutDP}-{bloc.FinDP}";
                    var fdp = $"{bloc.DebutFDP}-{bloc.FinFDP}";
                    text = $" {marker}{blocIdx + 1,2}  {bloc.Sequence,3}  {bloc.Jour,-10} {bloc.Periode,-5} {dp,-12} {fdp,-12} {bloc.Vols.Count,4} {bloc.HdvBloc,5:F2}";
                    color = isCursor ? selectedColor : _theme.Normal;
                }
                else
                {
                    text = "";
                    color = _theme.Normal;
                }
            }

            if (text.Length > effectiveWidth) text = text[..effectiveWidth];
            var pad = Math.Max(0, effectiveWidth - VisibleLength(text));

            if (showScroll)
                BufferAt(startCol, rowY,
                    $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{scrollChars![i]}{v}{_theme.Reset}");
            else
                BufferAt(startCol, rowY,
                    $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
        }

        // Separator
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = " Enter editer  Ins ajouter  Suppr supprimer  Esc retour ";
        if (footer.Length > boxInnerWidth) footer = footer[..boxInnerWidth];
        var footerPad = Math.Max(0, boxInnerWidth - footer.Length);
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom
        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");
    }

    private void RenderProgBlocEdit(ProgrammeOverlay overlay)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";
        const string editColor = "\x1b[97;42m";

        var bloc = overlay.CurrentBloc;
        var st = overlay.CurrentST;
        if (bloc == null || st == null) return;

        var labels = ProgrammeOverlay.GetFieldLabels();
        var boxOuterWidth = Math.Min(_width - 4, Math.Max(56, _width * 7 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        var contentHeight = labels.Length + 2; // fields + padding
        var boxTotalHeight = contentHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        // Title
        var titleText = $" Bloc {overlay.BlocIndex + 1} - {st.Reference} ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = Math.Max(0, titleFill / 2);
        var titleRight = Math.Max(0, titleFill - titleLeft);
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        // Content
        const int labelWidth = 18;
        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;
            string text;
            string color;

            if (i == 0 || i == contentHeight - 1)
            {
                text = "";
                color = _theme.Normal;
            }
            else
            {
                var fi = i - 1; // field index
                if (fi < labels.Length)
                {
                    var isCursor = fi == overlay.FieldIndex;
                    var marker = isCursor ? ">" : " ";
                    var label = labels[fi];
                    var dots = new string('.', Math.Max(1, labelWidth - label.Length));
                    var val = ProgrammeOverlay.GetBlocFieldValue(bloc, fi);

                    if (isCursor && overlay.FieldEditing)
                    {
                        text = $" {marker} {label} {dots} [{overlay.FieldEditBuffer}]";
                        color = editColor;
                    }
                    else if (ProgrammeOverlay.IsFieldSelector(fi))
                    {
                        text = $" {marker} {label} {dots} < {val} >";
                        color = isCursor ? selectedColor : _theme.Normal;
                    }
                    else if (ProgrammeOverlay.IsFieldLink(fi))
                    {
                        text = $" {marker} {label} {dots} {val}";
                        color = isCursor ? selectedColor : _theme.Normal;
                    }
                    else
                    {
                        text = $" {marker} {label} {dots} {val}";
                        color = isCursor ? selectedColor : _theme.Normal;
                    }
                }
                else
                {
                    text = "";
                    color = _theme.Normal;
                }
            }

            if (text.Length > boxInnerWidth) text = text[..boxInnerWidth];
            var pad = Math.Max(0, boxInnerWidth - VisibleLength(text));
            BufferAt(startCol, rowY,
                $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
        }

        // Separator
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = overlay.FieldEditing
            ? " Enter ok  Esc annuler "
            : " \u2191\u2193 champ  \u2190\u2192 selecteur  Enter editer  Esc retour ";
        if (footer.Length > boxInnerWidth) footer = footer[..boxInnerWidth];
        var footerPad = Math.Max(0, boxInnerWidth - footer.Length);
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom
        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");

        // Cursor
        if (overlay.FieldEditing)
        {
            var fi = overlay.FieldIndex;
            var rowOffset = fi + 1; // +1 for padding row
            var label = labels[fi];
            var dots = new string('.', Math.Max(1, labelWidth - label.Length));
            var prefix = $" > {label} {dots} [";
            var col = startCol + 1 + prefix.Length + overlay.FieldEditCursorPos;
            BufferCursorPos(col, startRow + 1 + rowOffset);
        }
    }

    private void RenderProgCalendrier(ProgrammeOverlay overlay)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";

        var cal = overlay.Config.Calendrier;
        var annee = cal.Count > 0 ? cal[0].Annee : overlay.Config.Periode.Annee;

        var boxOuterWidth = Math.Min(_width - 4, Math.Max(56, _width * 7 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        // Display as rows: up to 3 columns per row
        const int colWidth = 16; // "S01 < BS_01 > "
        var numCols = Math.Max(1, boxInnerWidth / colWidth);
        var rowsPerCol = (cal.Count + numCols - 1) / numCols;

        var contentHeight = Math.Max(4, Math.Min(rowsPerCol + 1, ViewportHeight - 6));
        var boxTotalHeight = contentHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        overlay.EnsureCalVisible(contentHeight);

        // Compute which visual row the selected item is on
        var selectedVisRow = overlay.CalIndex % rowsPerCol;

        // Title
        var titleText = $" Calendrier {annee} ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = Math.Max(0, titleFill / 2);
        var titleRight = Math.Max(0, titleFill - titleLeft);
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        // Content
        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;
            var visRow = overlay.CalScroll + i;

            var sb = new System.Text.StringBuilder();
            sb.Append("  ");

            var rowHasSelected = false;
            for (var c = 0; c < numCols; c++)
            {
                var calIdx = c * rowsPerCol + visRow;
                if (calIdx < cal.Count)
                {
                    var aff = cal[calIdx];
                    var isCursor = calIdx == overlay.CalIndex;
                    if (isCursor) rowHasSelected = true;
                    var sLabel = $"S{aff.Semaine:D2}";
                    var refStr = aff.SemaineTypeRef;
                    var entry = isCursor ? $"{sLabel} < {refStr,-6} >" : $"{sLabel}   {refStr,-6}  ";
                    sb.Append(entry.PadRight(colWidth));
                }
            }

            var text = sb.ToString();
            var color = rowHasSelected ? selectedColor : _theme.Normal;

            if (text.Length > boxInnerWidth) text = text[..boxInnerWidth];
            var pad = Math.Max(0, boxInnerWidth - VisibleLength(text));
            BufferAt(startCol, rowY,
                $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
        }

        // Separator
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = " \u2190\u2192 changer type  \u2191\u2193 nav  Tab types  Esc fermer ";
        if (footer.Length > boxInnerWidth) footer = footer[..boxInnerWidth];
        var footerPad = Math.Max(0, boxInnerWidth - footer.Length);
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom
        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");
    }

    // ── VolsList ──

    private void RenderProgVolsList(ProgrammeOverlay overlay)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";
        const string mhColor = "\x1b[93m"; // yellow for MH

        var bloc = overlay.CurrentBloc;
        var st = overlay.CurrentST;
        if (bloc == null || st == null) return;

        var boxOuterWidth = Math.Min(_width - 4, Math.Max(64, _width * 8 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        var contentHeight = Math.Max(4, Math.Min(bloc.Vols.Count + 2, ViewportHeight - 6));
        var boxTotalHeight = contentHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        overlay.EnsureVolVisible(contentHeight - 2);

        var showScroll = bloc.Vols.Count + 2 > contentHeight;
        var scrollChars = showScroll ? BuildScrollbar(contentHeight, bloc.Vols.Count + 2, overlay.VolScroll) : null;
        var effectiveWidth = showScroll ? boxInnerWidth - 1 : boxInnerWidth;

        // Title
        var titleText = $" Vols - Bloc {overlay.BlocIndex + 1} {st.Reference} ({bloc.Jour} {bloc.Periode}) ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = Math.Max(0, titleFill / 2);
        var titleRight = Math.Max(0, titleFill - titleLeft);
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        // Content
        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;
            string text;
            string color;

            var dataIdx = overlay.VolScroll + i;

            if (dataIdx == 0)
            {
                text = "  #   N\u00b0     DEP  ARR  H.DEP  H.ARR   HDV  MH";
                color = _theme.TitleBar;
            }
            else if (dataIdx == 1)
            {
                text = "  " + new string('\u2500', Math.Min(50, effectiveWidth - 2));
                color = _theme.Normal;
            }
            else
            {
                var volIdx = dataIdx - 2;
                if (volIdx >= 0 && volIdx < bloc.Vols.Count)
                {
                    var vol = bloc.Vols[volIdx];
                    var isCursor = volIdx == overlay.VolIndex;
                    var marker = isCursor ? ">" : " ";
                    var hdv = vol.HdvVol;
                    var hdvStr = $"{(int)hdv}h{(int)((hdv - (int)hdv) * 60):D2}";
                    var mhStr = vol.MH ? "MH" : "  ";
                    text = $" {marker}{volIdx + 1,2}   {vol.Numero,-6} {vol.Depart,-4} {vol.Arrivee,-4} {vol.HeureDepart,-6} {vol.HeureArrivee,-6} {hdvStr,-5} {mhStr}";
                    if (vol.MH && !isCursor)
                        color = mhColor;
                    else
                        color = isCursor ? selectedColor : _theme.Normal;
                }
                else
                {
                    text = "";
                    color = _theme.Normal;
                }
            }

            if (text.Length > effectiveWidth) text = text[..effectiveWidth];
            var pad = Math.Max(0, effectiveWidth - VisibleLength(text));

            if (showScroll)
                BufferAt(startCol, rowY,
                    $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{scrollChars![i]}{v}{_theme.Reset}");
            else
                BufferAt(startCol, rowY,
                    $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
        }

        // Separator
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = " Enter editer  Ins catalogue  M modif.horaire  Suppr supprimer  Esc retour ";
        if (footer.Length > boxInnerWidth) footer = footer[..boxInnerWidth];
        var footerPad = Math.Max(0, boxInnerWidth - footer.Length);
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom
        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");
    }

    // ── VolEdit ──

    private void RenderProgVolEdit(ProgrammeOverlay overlay)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";
        const string editColor = "\x1b[97;42m";

        var vol = overlay.CurrentVol;
        var bloc = overlay.CurrentBloc;
        var st = overlay.CurrentST;
        if (vol == null || bloc == null || st == null) return;

        var labels = ProgrammeOverlay.GetVolFieldLabels();
        var boxOuterWidth = Math.Min(_width - 4, Math.Max(50, _width * 6 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        var contentHeight = labels.Length + 2;
        var boxTotalHeight = contentHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        // Title
        var titleText = $" Vol {overlay.VolIndex + 1} - Bloc {overlay.BlocIndex + 1} {st.Reference} ";
        var titleFill = boxInnerWidth - titleText.Length;
        var titleLeft = Math.Max(0, titleFill / 2);
        var titleRight = Math.Max(0, titleFill - titleLeft);
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        // Content
        const int labelWidth = 18;
        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;
            string text;
            string color;

            if (i == 0 || i == contentHeight - 1)
            {
                text = "";
                color = _theme.Normal;
            }
            else
            {
                var fi = i - 1;
                if (fi < labels.Length)
                {
                    var isCursor = fi == overlay.VolFieldIndex;
                    var marker = isCursor ? ">" : " ";
                    var label = labels[fi];
                    var dots = new string('.', Math.Max(1, labelWidth - label.Length));
                    var val = ProgrammeOverlay.GetVolFieldValue(vol, fi);

                    if (isCursor && overlay.VolFieldEditing)
                    {
                        text = $" {marker} {label} {dots} [{overlay.VolFieldEditBuffer}]";
                        color = editColor;
                    }
                    else
                    {
                        text = $" {marker} {label} {dots} {val}";
                        color = isCursor ? selectedColor : _theme.Normal;
                    }
                }
                else
                {
                    text = "";
                    color = _theme.Normal;
                }
            }

            if (text.Length > boxInnerWidth) text = text[..boxInnerWidth];
            var pad = Math.Max(0, boxInnerWidth - VisibleLength(text));
            BufferAt(startCol, rowY,
                $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
        }

        // Separator
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = overlay.VolFieldEditing
            ? " Enter ok  Esc annuler "
            : " \u2191\u2193 champ  Enter editer  Esc retour ";
        if (footer.Length > boxInnerWidth) footer = footer[..boxInnerWidth];
        var footerPad = Math.Max(0, boxInnerWidth - footer.Length);
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom
        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");

        // Cursor
        if (overlay.VolFieldEditing)
        {
            var fi = overlay.VolFieldIndex;
            var rowOffset = fi + 1;
            var label = labels[fi];
            var dots = new string('.', Math.Max(1, labelWidth - label.Length));
            var prefix = $" > {label} {dots} [";
            var col = startCol + 1 + prefix.Length + overlay.VolFieldEditCursorPos;
            BufferCursorPos(col, startRow + 1 + rowOffset);
        }
    }

    // ── Catalogue ──

    private void RenderProgCatalogue(ProgrammeOverlay overlay)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";

        var catalogue = overlay.Config.CatalogueVols;

        var boxOuterWidth = Math.Min(_width - 4, Math.Max(64, _width * 8 / 10));
        var boxInnerWidth = boxOuterWidth - 2;

        var contentHeight = Math.Max(4, Math.Min(catalogue.Count + 2, ViewportHeight - 6));
        var boxTotalHeight = contentHeight + 4;

        var startCol = (_width - boxOuterWidth) / 2;
        var startRow = 3 + (ViewportHeight - boxTotalHeight) / 2;
        if (startRow < 3) startRow = 3;

        overlay.EnsureCatVisible(contentHeight - 2);

        var showScroll = catalogue.Count + 2 > contentHeight;
        var scrollChars = showScroll ? BuildScrollbar(contentHeight, catalogue.Count + 2, overlay.CatScroll) : null;
        var effectiveWidth = showScroll ? boxInnerWidth - 1 : boxInnerWidth;

        // Title
        var modeLabel = overlay.CatPickMode ? "SELECTION" : "CATALOGUE VOLS TYPES";
        var titleText = $" {modeLabel} ";
        var countText = $" {catalogue.Count} vols ";
        var titleFill = boxInnerWidth - titleText.Length - countText.Length;
        var titleLeft = Math.Max(0, titleFill / 2);
        var titleRight = Math.Max(0, titleFill - titleLeft);
        BufferAt(startCol, startRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TitleBar}{countText}{bc}{_theme.TopRight}{_theme.Reset}");

        // Content
        for (var i = 0; i < contentHeight; i++)
        {
            var rowY = startRow + 1 + i;
            string text;
            string color;

            var dataIdx = overlay.CatScroll + i;

            if (dataIdx == 0)
            {
                text = "  #   N\u00b0     DEP  ARR  H.DEP  H.ARR   HDV";
                color = _theme.TitleBar;
            }
            else if (dataIdx == 1)
            {
                text = "  " + new string('\u2500', Math.Min(44, effectiveWidth - 2));
                color = _theme.Normal;
            }
            else
            {
                var catIdx = dataIdx - 2;
                if (catIdx >= 0 && catIdx < catalogue.Count)
                {
                    var vol = catalogue[catIdx];
                    var isCursor = catIdx == overlay.CatIndex;
                    var marker = isCursor ? ">" : " ";
                    var hdv = vol.HdvVol;
                    var hdvStr = $"{(int)hdv}h{(int)((hdv - (int)hdv) * 60):D2}";
                    text = $" {marker}{catIdx + 1,2}   {vol.Numero,-6} {vol.Depart,-4} {vol.Arrivee,-4} {vol.HeureDepart,-6} {vol.HeureArrivee,-6} {hdvStr,-5}";
                    color = isCursor ? selectedColor : _theme.Normal;
                }
                else
                {
                    text = "";
                    color = _theme.Normal;
                }
            }

            if (text.Length > effectiveWidth) text = text[..effectiveWidth];
            var pad = Math.Max(0, effectiveWidth - VisibleLength(text));

            if (showScroll)
                BufferAt(startCol, rowY,
                    $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{scrollChars![i]}{v}{_theme.Reset}");
            else
                BufferAt(startCol, rowY,
                    $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
        }

        // Separator
        BufferAt(startCol, startRow + 1 + contentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, boxInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = overlay.CatPickMode
            ? " Enter selectionner  Esc retour "
            : " Enter editer  Ins ajouter  Suppr supprimer  Esc retour ";
        if (footer.Length > boxInnerWidth) footer = footer[..boxInnerWidth];
        var footerPad = Math.Max(0, boxInnerWidth - footer.Length);
        BufferAt(startCol, startRow + 2 + contentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom
        BufferAt(startCol, startRow + 3 + contentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, boxInnerWidth)}{_theme.BottomRight}{_theme.Reset}");

        // Popup overlay for editing
        if (overlay.CatEditPopup && overlay.CatEditVol != null)
            RenderCatEditPopup(overlay, startCol, startRow, boxInnerWidth, contentHeight);
    }

    private void RenderCatEditPopup(ProgrammeOverlay overlay, int parentCol, int parentRow, int parentInnerWidth, int parentContentHeight)
    {
        var bc = _theme.BorderColor;
        var h = _theme.Horizontal;
        var v = _theme.Vertical;
        const string selectedColor = "\x1b[97;44;7m";
        const string editColor = "\x1b[97;42m";

        var vol = overlay.CatEditVol!;
        var labels = ProgrammeOverlay.GetVolFieldLabels();

        var popupInnerWidth = 30;
        var popupOuterWidth = popupInnerWidth + 2;
        var popupContentHeight = labels.Length + 2;
        var popupTotalHeight = popupContentHeight + 4;

        var popupCol = parentCol + (parentInnerWidth + 2 - popupOuterWidth) / 2;
        var popupRow = parentRow + (parentContentHeight + 4 - popupTotalHeight) / 2;
        if (popupRow < parentRow + 1) popupRow = parentRow + 1;

        // Title
        var titleText = " VOL TYPE ";
        var titleFill = popupInnerWidth - titleText.Length;
        var titleLeft = Math.Max(0, titleFill / 2);
        var titleRight = Math.Max(0, titleFill - titleLeft);
        BufferAt(popupCol, popupRow,
            $"{bc}{_theme.TopLeft}{new string(h, titleLeft)}{_theme.TitleBar}{titleText}{bc}{new string(h, titleRight)}{_theme.TopRight}{_theme.Reset}");

        // Content
        const int labelWidth = 16;
        for (var i = 0; i < popupContentHeight; i++)
        {
            var rowY = popupRow + 1 + i;
            string text;
            string color;

            if (i == 0 || i == popupContentHeight - 1)
            {
                text = "";
                color = _theme.Normal;
            }
            else
            {
                var fi = i - 1;
                if (fi < labels.Length)
                {
                    var isCursor = fi == overlay.CatEditFieldIndex;
                    var marker = isCursor ? ">" : " ";
                    var label = labels[fi];
                    var dots = new string('.', Math.Max(1, labelWidth - label.Length));
                    var val = ProgrammeOverlay.GetVolFieldValue(vol, fi);

                    if (isCursor && overlay.CatEditFieldEditing)
                    {
                        text = $" {marker} {label} {dots} [{overlay.CatEditFieldBuffer}]";
                        color = editColor;
                    }
                    else
                    {
                        text = $" {marker} {label} {dots} {val}";
                        color = isCursor ? selectedColor : _theme.Normal;
                    }
                }
                else
                {
                    text = "";
                    color = _theme.Normal;
                }
            }

            if (text.Length > popupInnerWidth) text = text[..popupInnerWidth];
            var pad = Math.Max(0, popupInnerWidth - VisibleLength(text));
            BufferAt(popupCol, rowY,
                $"{bc}{v}{color}{text}{new string(' ', pad)}{bc}{v}{_theme.Reset}");
        }

        // Separator
        BufferAt(popupCol, popupRow + 1 + popupContentHeight,
            $"{bc}{_theme.TeeLeft}{new string(h, popupInnerWidth)}{_theme.TeeRight}{_theme.Reset}");

        // Footer
        var footer = overlay.CatEditFieldEditing
            ? " Enter ok  Esc ann. "
            : " \u2191\u2193 Enter Esc ";
        if (footer.Length > popupInnerWidth) footer = footer[..popupInnerWidth];
        var footerPad = Math.Max(0, popupInnerWidth - footer.Length);
        BufferAt(popupCol, popupRow + 2 + popupContentHeight,
            $"{bc}{v}{_theme.HintBar}{footer}{new string(' ', footerPad)}{bc}{v}{_theme.Reset}");

        // Bottom
        BufferAt(popupCol, popupRow + 3 + popupContentHeight,
            $"{bc}{_theme.BottomLeft}{new string(h, popupInnerWidth)}{_theme.BottomRight}{_theme.Reset}");

        // Cursor
        if (overlay.CatEditFieldEditing)
        {
            var fi = overlay.CatEditFieldIndex;
            var rowOffset = fi + 1;
            var label = labels[fi];
            var dots = new string('.', Math.Max(1, labelWidth - label.Length));
            var prefix = $" > {label} {dots} [";
            var col = popupCol + 1 + prefix.Length + overlay.CatEditFieldCursorPos;
            BufferCursorPos(col, popupRow + 1 + rowOffset);
        }
    }

    // Helpers

    private string WrapRow(string color, string content)
    {
        var cw = ContentWidth;
        var visLen = VisibleLength(content);
        var padLen = Math.Max(0, cw - visLen);
        return $"{_theme.BorderColor}{_theme.Vertical}{color}{content}{color}{new string(' ', padLen)}{_theme.BorderColor}{_theme.Vertical}{_theme.Reset}";
    }

    private string WrapRowWithScrollbar(string color, string content, char scrollChar)
    {
        var cw = ContentWidth - 1;
        var visLen = VisibleLength(content);
        var padLen = Math.Max(0, cw - visLen);
        return $"{_theme.BorderColor}{_theme.Vertical}{color}{content}{color}{new string(' ', padLen)}{_theme.BorderColor}{scrollChar}{_theme.Vertical}{_theme.Reset}";
    }

    private string MakeSeparator()
    {
        var inner = _width - 2;
        return $"{_theme.SeparatorColor}{_theme.TeeLeft}{new string(_theme.Horizontal, inner)}{_theme.TeeRight}{_theme.Reset}";
    }

    private string PadFull(string text)
    {
        if (text.Length >= _width) return text[.._width];
        return text + new string(' ', _width - text.Length);
    }

    private static int VisibleLength(string text)
    {
        var len = 0;
        var inEscape = false;
        foreach (var c in text)
        {
            if (c == '\x1b') { inEscape = true; continue; }
            if (inEscape) { if (c == 'm') inEscape = false; continue; }
            len++;
        }
        return len;
    }

    private static string VisibleTruncate(string text, int maxVisible)
    {
        var visible = 0;
        var inEscape = false;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\x1b') { inEscape = true; continue; }
            if (inEscape) { if (text[i] == 'm') inEscape = false; continue; }
            visible++;
            if (visible >= maxVisible)
                return text[..(i + 1)];
        }
        return text;
    }

    private void BufferAt(int col, int row, string text)
    {
        _frameBuffer.Append($"\x1b[{row + 1};{col + 1}H{text}");
    }

    private void BufferCursorPos(int col, int row)
    {
        _frameBuffer.Append($"\x1b[{row + 1};{col + 1}H");
    }
}
