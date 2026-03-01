namespace CrewSizer.Domain.ValueObjects;

public class LimitesCumulatives
{
    public double H28Max { get; set; } = 100;
    public double H90Max { get; set; } = 280;
    public double H12Max { get; set; } = 900;
    public CumulEntrant CumulPNT { get; set; } = new();
    public CumulEntrant CumulPNC { get; set; } = new();
}
