namespace CrewSizer.Tui;

public class OutputBuffer : IOutputWriter
{
    private readonly List<string> _lines = [];
    private string _currentLine = "";

    public int ScrollOffset { get; private set; }
    public int TotalLines => _lines.Count;

    public void WriteLine(string text = "")
    {
        _lines.Add(_currentLine + text);
        _currentLine = "";
        ScrollToBottom();
    }

    public void Write(string text)
    {
        _currentLine += text;
    }

    public void Clear()
    {
        _lines.Clear();
        _currentLine = "";
        ScrollOffset = 0;
    }

    public List<string> GetVisibleLines(int viewportHeight)
    {
        if (viewportHeight <= 0) return [];
        var start = Math.Max(0, Math.Min(ScrollOffset, _lines.Count - viewportHeight));
        var count = Math.Min(viewportHeight, _lines.Count - start);
        return count > 0 ? _lines.GetRange(start, count) : [];
    }

    public void ScrollUp(int lines = 1)
    {
        ScrollOffset = Math.Max(0, ScrollOffset - lines);
    }

    public void ScrollDown(int lines = 1, int viewportHeight = 0)
    {
        var maxOffset = Math.Max(0, _lines.Count - viewportHeight);
        ScrollOffset = Math.Min(maxOffset, ScrollOffset + lines);
    }

    public void PageUp(int viewportHeight) => ScrollUp(viewportHeight);

    public void PageDown(int viewportHeight) => ScrollDown(viewportHeight, viewportHeight);

    public void ScrollToBottom()
    {
        ScrollOffset = Math.Max(0, _lines.Count);
    }

    public bool CanScrollUp => ScrollOffset > 0;

    public bool CanScrollDown(int viewportHeight) =>
        ScrollOffset < _lines.Count - viewportHeight;
}
