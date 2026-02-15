namespace CrewSizer.Tui;

public class BorlandTheme : ITheme
{
    public string Name => "borland";

    public string Reset => "\x1b[0m";
    public string TitleBar => "\x1b[97;46m";
    public string StatusBar => "\x1b[93;44m";
    public string SeparatorColor => "\x1b[96;44m";
    public string BorderColor => "\x1b[96;44m";
    public string Prompt => "\x1b[1;93;44m";
    public string InputText => "\x1b[97;44m";
    public string Normal => "\x1b[37;44m";
    public string CommandEcho => "\x1b[36;44m";
    public string HintBar => "\x1b[97;44m";
    public string ScrollHint => "\x1b[36;44m";

    public string AlertOk => "\x1b[92;44m";
    public string AlertWarning => "\x1b[93;44m";
    public string AlertExceeded => "\x1b[91;44m";

    public bool HasBorders => true;
    public char Horizontal => '═';
    public char Vertical => '║';
    public char TopLeft => '╔';
    public char TopRight => '╗';
    public char BottomLeft => '╚';
    public char BottomRight => '╝';
    public char TeeLeft => '╠';
    public char TeeRight => '╣';
}
