namespace CrewSizer.Tui;

public interface ITheme
{
    string Name { get; }

    // Couleurs ANSI
    string Reset { get; }
    string TitleBar { get; }
    string StatusBar { get; }
    string SeparatorColor { get; }
    string BorderColor { get; }
    string Prompt { get; }
    string InputText { get; }
    string Normal { get; }
    string CommandEcho { get; }
    string HintBar { get; }
    string ScrollHint { get; }

    // Alertes
    string AlertOk { get; }
    string AlertWarning { get; }
    string AlertExceeded { get; }

    // Bordures
    bool HasBorders { get; }
    char Horizontal { get; }
    char Vertical { get; }
    char TopLeft { get; }
    char TopRight { get; }
    char BottomLeft { get; }
    char BottomRight { get; }
    char TeeLeft { get; }
    char TeeRight { get; }
}
