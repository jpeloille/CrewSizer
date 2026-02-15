namespace CrewSizer.Tui;

public interface IOutputWriter
{
    void WriteLine(string text = "");
    void Write(string text);
}
