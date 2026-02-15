namespace CrewSizer.Tui;

public class HelpOverlay
{
    public int SelectedIndex { get; private set; }
    public int DetailScrollOffset { get; private set; }
    public int ListScrollOffset { get; private set; }
    public bool IsVisible { get; private set; } = true;

    public HelpEntry CurrentEntry => HelpContent.Entries[SelectedIndex];

    public bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedIndex > 0)
                {
                    SelectedIndex--;
                    DetailScrollOffset = 0;
                }
                break;

            case ConsoleKey.DownArrow:
                if (SelectedIndex < HelpContent.Entries.Count - 1)
                {
                    SelectedIndex++;
                    DetailScrollOffset = 0;
                }
                break;

            case ConsoleKey.PageUp:
                if (DetailScrollOffset > 0)
                    DetailScrollOffset = Math.Max(0, DetailScrollOffset - 5);
                break;

            case ConsoleKey.PageDown:
                DetailScrollOffset += 5;
                break;

            case ConsoleKey.Home:
                SelectedIndex = 0;
                DetailScrollOffset = 0;
                break;

            case ConsoleKey.End:
                SelectedIndex = HelpContent.Entries.Count - 1;
                DetailScrollOffset = 0;
                break;

            case ConsoleKey.Escape:
                IsVisible = false;
                return false;
        }

        return true;
    }

    public void EnsureListVisibleInHeight(int listHeight)
    {
        if (SelectedIndex < ListScrollOffset)
            ListScrollOffset = SelectedIndex;
        else if (SelectedIndex >= ListScrollOffset + listHeight)
            ListScrollOffset = SelectedIndex - listHeight + 1;
    }

    public void ClampDetailScroll(int detailHeight)
    {
        var maxScroll = Math.Max(0, CurrentEntry.Lines.Length - detailHeight);
        if (DetailScrollOffset > maxScroll)
            DetailScrollOffset = maxScroll;
    }
}
