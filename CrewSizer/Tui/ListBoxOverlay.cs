namespace CrewSizer.Tui;

public class ListBoxOverlay
{
    public string Title { get; }
    public List<string> Items { get; }
    public int SelectedIndex { get; private set; }
    public int ScrollOffset { get; private set; }
    public bool IsVisible { get; private set; }
    public HashSet<int> CheckedIndices { get; } = [];
    public List<string>? Results { get; private set; }

    public ListBoxOverlay(string title, List<string> items, IEnumerable<string>? preChecked = null)
    {
        Title = title;
        Items = items;
        IsVisible = true;

        if (preChecked != null)
        {
            foreach (var name in preChecked)
            {
                var idx = items.IndexOf(name);
                if (idx >= 0)
                    CheckedIndices.Add(idx);
            }
        }
    }

    public bool HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedIndex > 0)
                    SelectedIndex--;
                break;

            case ConsoleKey.DownArrow:
                if (SelectedIndex < Items.Count - 1)
                    SelectedIndex++;
                break;

            case ConsoleKey.Spacebar:
                if (!CheckedIndices.Remove(SelectedIndex))
                    CheckedIndices.Add(SelectedIndex);
                break;

            case ConsoleKey.Enter:
                if (CheckedIndices.Count > 0)
                    Results = CheckedIndices.OrderBy(i => i).Select(i => Items[i]).ToList();
                else
                    Results = null;
                IsVisible = false;
                return false;

            case ConsoleKey.Escape:
                Results = null;
                IsVisible = false;
                return false;
        }

        return true;
    }

    public void EnsureVisibleInHeight(int listHeight)
    {
        if (SelectedIndex < ScrollOffset)
            ScrollOffset = SelectedIndex;
        else if (SelectedIndex >= ScrollOffset + listHeight)
            ScrollOffset = SelectedIndex - listHeight + 1;
    }
}
