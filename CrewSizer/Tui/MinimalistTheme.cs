namespace CrewSizer.Tui;

public class MinimalistTheme : ITheme
{
    public string Name => "minimalist";

    public string Reset => "\x1b[0m";
    public string TitleBar => "\x1b[30;47m";
    public string StatusBar => "\x1b[97;48;5;236m";
    public string SeparatorColor => "\x1b[38;5;240m";
    public string BorderColor => "";
    public string Prompt => "\x1b[1;36m";
    public string InputText => "\x1b[1;37m";
    public string Normal => "\x1b[37m";
    public string CommandEcho => "\x1b[38;5;245m";
    public string HintBar => "\x1b[38;5;245;48;5;236m";
    public string ScrollHint => "\x1b[38;5;240m";

    public string AlertOk => "\x1b[92m";
    public string AlertWarning => "\x1b[93m";
    public string AlertExceeded => "\x1b[91m";

    public bool HasBorders => false;
    public char Horizontal => '─';
    public char Vertical => ' ';
    public char TopLeft => ' ';
    public char TopRight => ' ';
    public char BottomLeft => ' ';
    public char BottomRight => ' ';
    public char TeeLeft => ' ';
    public char TeeRight => ' ';
}
